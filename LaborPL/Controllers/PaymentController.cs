
using LaborBLL.Service.Implementation;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using LaborDAL.Repo.Implementation;
using Microsoft.AspNetCore.Identity;
using Stripe;

namespace LaborPL.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService paymentService;
        private readonly IEscrowService escrowService;
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork unitOfWork;
        private readonly UserManager<AppUser> userManager;

        public PaymentController( IPaymentService paymentService,IEscrowService escrowService ,IConfiguration configuration, IUnitOfWork unitOfWork ,UserManager<AppUser> userManager )
        {
            this.paymentService = paymentService;
            this.escrowService = escrowService;
            this.configuration = configuration;
            this.unitOfWork = unitOfWork;
            this.userManager = userManager;
        }
        [Authorize]
        // ✅ الصح
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var booking = await unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
                return NotFound();

            var result = await paymentService.CreatePaymentIntentAsync(bookingId, booking.AgreedRate);
            if (!result.Success)
                return RedirectToAction("Error");

            var model = new CheckoutViewModel
            {
                BookingId = bookingId,
                Amount = booking.AgreedRate,
                ClientSecret = result.Result,
                PubishableKey = configuration["Stripe:PublishableKey"]
            };
            return View(model);
        }
      
        public async Task<IActionResult> Status(int bookingId)
        {
            var payment = await unitOfWork.Payments.GetPaymentByBookingIdAsync(bookingId);

            if (payment == null)
                return RedirectToAction("Error");

            var model = new PaymentStatusViewModel
            {
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                Status = payment.Status.ToString(),
                CreatAt = payment.CreatedAt,
                RealaseAt = payment.ReleasedAt
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);

                if (stripeEvent.Type == "payment_intent.succeeded")
                    {
                    // الدفع نجح
                }
                    if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    // الدفع فشل
                }

                return Ok();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Refund(int bookingId)
        {
            var result = await paymentService.RefundPaymentAsync(bookingId);
            if (!result.Success)
                return RedirectToAction("Error");

            return RedirectToAction("Status", new { bookingId });
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
