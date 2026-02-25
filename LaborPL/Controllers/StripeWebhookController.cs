

using LaborBLL.Common;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.IO;
using System.Threading.Tasks;

namespace LaborPL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;
     
        private readonly IUserService _userService;
      
        private readonly IUnitOfWork _unitOfWork;
    //    private readonly IEmailService _emailService;
        private readonly IPaymentService _paymentService;
        private readonly IEscrowService _escrowService;
        private readonly ILogger<StripeWebhookController> _logger;

     
        public StripeWebhookController(
            IOptions<StripeSettings> stripeSettings,
          
            IUnitOfWork unitOfWork,
          
            IPaymentService paymentService,
            ILogger<StripeWebhookController> logger,
                IUserService userService,
                IEscrowService escrowService
         )
        {
            _stripeSettings = stripeSettings.Value;
           
            _logger = logger;
            _unitOfWork = unitOfWork;
           
            _paymentService = paymentService;
            _userService = userService;
            _escrowService = escrowService;

        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            _logger.LogInformation("Stripe Webhook Received: Starting processing...");
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeSettings.WebhookSecret
                );

                _logger.LogInformation($"Webhook received: {stripeEvent.Type}");

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandleSuccessfulPayment(stripeEvent.Data.Object as PaymentIntent);
                        break;
                    case "payment_intent.payment_failed":
                        await HandlePaymentFailed(stripeEvent.Data.Object as PaymentIntent);
                        break;
                    case "charge.refunded":
                        await HandleRefund(stripeEvent.Data.Object as Charge);
                        break;
                }

                return Ok();

               
            }
            catch (StripeException e)
            {
                _logger.LogError($"Stripe Signature Verification Failed: {e.Message}");
                return BadRequest($"Webhook error: {e.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected Error in Stripe Webhook: {ex.Message} \n {ex.StackTrace}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        private async Task HandleSuccessfulPayment(PaymentIntent paymentIntent)
        {
            if (paymentIntent.Metadata.TryGetValue("bookingId", out var bookingIdStr)
               && int.TryParse(bookingIdStr, out var bookingId))
            {
                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                if (payment != null && payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Held;
                    payment.TransactionId = paymentIntent.Id;
                    await _unitOfWork.Payments.UpdateAsync(payment);
                    await _unitOfWork.SaveAsync();
                    _logger.LogInformation($"Payment held for booking {bookingId}");
                }
            }

        }
        private async Task HandlePaymentFailed(PaymentIntent paymentIntent)
        {
            if (paymentIntent.Metadata.TryGetValue("bookingId", out var bookingIdStr)
                && int.TryParse(bookingIdStr, out var bookingId))
            {
                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Failed;
                    await _unitOfWork.Payments.UpdateAsync(payment);
                    await _unitOfWork.SaveAsync();
                    _logger.LogWarning($"Payment failed for booking {bookingId}");
                }
            }
        }

        private async Task HandleRefund(Charge charge)
        {
            _logger.LogInformation($"Refund processed: {charge?.Id}");

            try
            {
                if (charge == null)
                {
                    _logger.LogWarning("HandleRefund called with null charge.");
                    return;
                }

                // Try to get bookingId from charge metadata
                if (charge.Metadata != null &&
                    charge.Metadata.TryGetValue("bookingId", out var bookingIdStr) &&
                    int.TryParse(bookingIdStr, out var bookingId))
                {
                    var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                    if (payment != null)
                    {
                        // Update internal payment status to refunded and persist
                        payment.Status = PaymentStatus.Refunded;
                        payment.TransactionId = charge.Id;
                        await _unitOfWork.Payments.UpdateAsync(payment);
                        await _unitOfWork.SaveAsync();

                        _logger.LogInformation($"Payment refunded for booking {bookingId}, charge {charge.Id}");
                        return;
                    }
                    else
                    {
                        _logger.LogWarning($"No payment found for booking {bookingId} while processing refund (charge {charge.Id}).");
                        return;
                    }
                }

                // Fallback: no bookingId in metadata — log details for investigation
                _logger.LogWarning("Refund received but no bookingId metadata present. " +
                                   $"ChargeId: {charge.Id}, PaymentIntent: {charge.PaymentIntent}, Metadata keys: {string.Join(",", charge.Metadata?.Keys ?? Enumerable.Empty<string>())}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while handling refund for charge {charge?.Id}");
            }
        }
      
    }
}
