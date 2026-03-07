using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    public class RatingController : Controller
    {
        private readonly IRatingService ratingService;
        private readonly UserManager<AppUser> userManager;

        public RatingController( IRatingService ratingService,UserManager<AppUser> userManager )
        {
            this.ratingService = ratingService;
            this.userManager = userManager;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyRate()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var ratings = await ratingService.GetAllRatingById(userId);
         
            return View (ratings);
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
