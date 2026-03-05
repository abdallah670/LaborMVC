using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    public class RatingController : Controller
    {
        public RatingController( )
        { }

        public IActionResult Index()
        {
            return View();
        }
    }
}
