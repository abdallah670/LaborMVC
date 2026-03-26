using LaborBLL.Common;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using System.IO;
using System.Threading.Tasks;
using System.Linq;  // 👈 أضف ده

namespace LaborPL.Controllers
{
    [Route("api/stripe/webhook")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;
        private readonly IEscrowService _escrowService;

        public StripeWebhookController(
            IOptions<StripeSettings> stripeSettings,
            IUnitOfWork unitOfWork,
            IPaymentService paymentService,
            ILogger<StripeWebhookController> logger,
            IUserService userService,
            IEscrowService escrowService)
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
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            _logger.LogInformation($"Webhook received. Signature length: {signature?.Length ?? 0}");

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    _stripeSettings.WebhookSecret,
                    throwOnApiVersionMismatch: false
                );

                _logger.LogInformation($"✅ Event verified: {stripeEvent.Type}");

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        _logger.LogInformation($"PaymentIntent succeeded: {paymentIntent?.Id}");
                        await HandleSuccessfulPayment(paymentIntent);
                        break;

                    case "charge.succeeded":
                        var charge = stripeEvent.Data.Object as Charge;
                        _logger.LogInformation($"Charge succeeded: {charge?.Id}");

                        // ✅ الكود البسيط هنا
                        if (charge?.Metadata != null &&
                            charge.Metadata.TryGetValue("bookingId", out var bookingIdStr))
                        {
                            if (int.TryParse(bookingIdStr, out var bookingId))
                            {
                                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);

                                if (payment != null)
                                {
                                    payment.Status = PaymentStatus.Held;
                                    payment.TransactionId = charge.PaymentIntentId;

                                    await _unitOfWork.Payments.UpdateAsync(payment);
                                    await _unitOfWork.SaveAsync();

                                    _logger.LogInformation($"✅ Payment for booking {bookingId} updated");
                                }
                            }
                        }
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError($"❌ Stripe signature error: {ex.Message}");
                return BadRequest($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ General error: {ex.Message}");
                return StatusCode(500, "Internal error");
            }
        }

        // ✅ دالة تحديث حالة الدفع
        private async Task UpdatePaymentStatus(string paymentIntentId, string status)
        {
            // جيب كل المدفوعات وحولها لـ List
            var allPayments = (await _unitOfWork.Payments.GetAllAsync()).ToList();

            // دور على اللي TransactionId بتاعه مطابق
            var payment = allPayments.FirstOrDefault(p => p.TransactionId == paymentIntentId);

            if (payment != null)
            {
                payment.Status = status == "Completed" ? PaymentStatus.Held : PaymentStatus.Refunded;
                await _unitOfWork.Payments.UpdateAsync(payment);
                await _unitOfWork.SaveAsync();
                _logger.LogInformation($"Payment {payment.Id} updated to {status}");
            }
        }

        private async Task HandleSuccessfulPayment(PaymentIntent? paymentIntent)
        {
            if (paymentIntent?.Metadata == null) return;

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

        private async Task HandlePaymentFailed(PaymentIntent? paymentIntent)
        {
            if (paymentIntent?.Metadata == null) return;

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

        private async Task HandleRefund(Charge? charge)
        {
            if (charge?.Metadata == null) return;

            if (charge.Metadata.TryGetValue("bookingId", out var bookingIdStr) &&
                int.TryParse(bookingIdStr, out var bookingId))
            {
                var payment = await _unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);
                if (payment != null)
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.TransactionId = charge.Id;
                    await _unitOfWork.Payments.UpdateAsync(payment);
                    await _unitOfWork.SaveAsync();
                    _logger.LogInformation($"Payment refunded for booking {bookingId}");
                }
            }
            else
            {
                _logger.LogWarning("Refund received but no bookingId metadata present. " +
                                   $"ChargeId: {charge.Id}, Metadata keys: {string.Join(",", charge.Metadata?.Keys ?? Enumerable.Empty<string>())}");
            }
        }
    }
}