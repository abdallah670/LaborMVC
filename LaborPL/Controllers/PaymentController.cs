
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

        private readonly IPaymentService _paymentService;
        private readonly IEscrowService _escrowService;
        private readonly IBookingService _bookingService;
        private readonly IPaymentReceiptService _receiptService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUser> _userManager;

        public PaymentController(IPaymentService paymentService, IEscrowService escrowService,
            IBookingService bookingService, IPaymentReceiptService receiptService,
            IConfiguration configuration, UserManager<AppUser> userManager)
        {
            _paymentService = paymentService;
            _escrowService = escrowService;
            _bookingService = bookingService;
            _receiptService = receiptService;
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "You do not have permission to view this page.";
                return RedirectToAction("Index", "Home");
            }
            var response = await _paymentService.GetAllAsync();
            if (!response.Success)
            {
                TempData["Error"] = response.ErrorMessage;
                return View(new List<PaymentVM>());
            }
            return View(response.Result);
        }
        public async Task<IActionResult> MyPaymentHistory()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var response = await _paymentService.GetByUserIdAsync(userId);
            if (!response.Success)
            {
                TempData["Error"] = response.ErrorMessage;
                return View(new List<PaymentVM>());
            }
            return View(response.Result);

        }
        public async Task<IActionResult> Details(int id)
        {
            var response = await _paymentService.GetByIdAsync(id);
            if (!response.Success)
            {
                TempData["Error"] = response.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }
            return View(response.Result);
        }
        [Authorize]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get booking details
            var bookingResponse = await _bookingService.GetBookingByIdAsync(bookingId);
            if (!bookingResponse.Success || bookingResponse.Result == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index", "Home");
            }

            var booking = bookingResponse.Result;

            // Check if user is the poster
            if (booking.PosterId != userId)
            {
                TempData["Error"] = "You are not authorized to pay for this booking.";
                return RedirectToAction("Index", "Home");
            }

            // Check if payment already exists
            var existingPayment = await _paymentService.GetPaymentByBookingIdAsync(bookingId);
            PaymentVM payment;
            
            if (existingPayment.Success && existingPayment.Result != null)
            {
                // Use existing payment
                payment = existingPayment.Result;
            }
            else
            {
                // Create new payment
                var paymentVM = new PaymentVM
                {
                    BookingId = bookingId,
                    UserId = userId,
                    Amount = booking.AgreedRate,
                    PaymentType = "Booking",
                    Description = $"Payment for booking #{bookingId}",
                    Currency = "USD",
                    PaymentMethod = "CreditCard"
                };

                var createResponse = await _paymentService.CreateAsync(paymentVM);
                if (!createResponse.Success)
                {
                    TempData["Error"] = createResponse.ErrorMessage;
                    return RedirectToAction("Details", "Booking", new { id = bookingId });
                }
                payment = createResponse.Result;
            }

            // Show Stripe checkout form
            var viewModel = new CheckoutViewModel
            {
                BookingId = bookingId,
                Amount = booking.AgreedRate,
                ClientSecret = payment.ClientSecret, // Use the actual ClientSecret from Stripe
                PubishableKey = _configuration["Stripe:PublishableKey"]
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ReleasePayment(int bookingId)
        {
            var result = await _escrowService.ReleasePaymentAsync(bookingId);
            if (result.Success)
            {
                TempData["Success"] = "Payment released successfully.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelPayment(int bookingId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _escrowService.ProcessCancellationAsync(bookingId, userId);
            if (result.Success)
            {
                TempData["Success"] = "Booking cancelled and refund processed.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
            }
            return RedirectToAction("MyPaymentHistory");
        }
    }
}
