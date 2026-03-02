
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace LaborPL.Controllers
{
    public class BaseController : Controller
    {
        private readonly UserManager<AppUser> userManager;

        public BaseController ( UserManager<AppUser> userManager)
        {
            this.userManager = userManager;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if(User.Identity?.IsAuthenticated==true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user=userManager.FindByIdAsync(userId).Result;
                ViewBag.AverageRating = user?.AverageRating ?? 0;
                ViewBag.FullName = $"{user?.FirstName} {user?.LastName}";
                ViewBag.ProfilePicture = user?.ProfilePictureUrl;


            }
            base.OnActionExecuting(context);


        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
