using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    public class RatingController : Controller
    {
        private readonly IRatingService ratingService;
        private readonly UserManager<AppUser> userManager;
        private readonly IBookingService bookingService;

        public RatingController(IRatingService ratingService, UserManager<AppUser> userManager, IBookingService bookingService)
        {
            this.ratingService = ratingService;
            this.userManager = userManager;
            this.bookingService = bookingService;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyRate(string? userId = null)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;
            var isAdmin = User.IsInRole("Admin");

            // If userId is provided and user is admin, use that. Otherwise use current user.
            var targetUserId = (isAdmin && !string.IsNullOrEmpty(userId)) ? userId : currentUserId;
            
            var targetUser = await userManager.FindByIdAsync(targetUserId);
            if (targetUser == null)
            {
                return NotFound("User not found.");
            }

            var ratingsResult = await ratingService.GetAllRatingById(targetUserId);
            
            // Get pending ratings (Completed bookings where user is a participant but hasn't rated yet)
            var bookingsResult = await bookingService.GetBookingsByUserIdAsync(targetUserId);
            var completedBookings = bookingsResult.Result?.Where(b => b.Status == LaborDAL.Enums.BookingStatus.Completed) ?? Enumerable.Empty<LaborBLL.ModelVM.BookingDashboardViewModel>();
            
            var pendingRatings = new List<LaborBLL.ModelVM.BookingDashboardViewModel>();
            foreach (var booking in completedBookings)
            {
                // Only suggest rating if the current user hasn't rated the OTHER party in this booking yet
                // Who is the ratee? 
                // If targetUserId is the poster, the ratee is the worker.
                // If targetUserId is the worker, the ratee is the poster.
                
                string rateeId = (targetUserId == booking.PosterId) ? booking.WorkerId : booking.PosterId;
                
                if (!string.IsNullOrEmpty(rateeId))
                {
                    var existingRating = await ratingService.GetRatingAsync(targetUserId, rateeId, booking.Id);
                    if (existingRating == null)
                    {
                        pendingRatings.Add(booking);
                    }
                }
            }

            ViewBag.TargetUser = targetUser;
            ViewBag.IsAdminView = isAdmin && targetUserId != currentUserId;
            ViewBag.PendingRatings = pendingRatings;
         
            return View(ratingsResult);
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
