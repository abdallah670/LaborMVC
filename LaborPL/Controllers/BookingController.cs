using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService bookingService;
        private readonly UserManager<AppUser> userManager;

        public BookingController(IBookingService bookingService, UserManager<AppUser> userManager)
        {
            this.bookingService = bookingService;
            this.userManager = userManager;
        }
        [HttpGet]
        [Authorize]
        public IActionResult Create()
        {
            var userId = userManager.GetUserId(User)!;
            var model = new CreateBookingViewModel
            {
                WorkerId = userId
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            model.WorkerId = userManager.GetUserId(User);
            if (!ModelState.IsValid)
                return View(model);

            var result = await bookingService.CreateBookingAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage);
                return View(model);
            }

            return RedirectToAction("Dashboard");
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Dashboard(string status)
        {
            var userId = userManager.GetUserId(User);
            var response = await bookingService.GetBookingsByWorkerIdAsync(userId);

            var bookings = response.Result;

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "upcoming")
                {

                    bookings = bookings.Where(b => b.StartTime >= DateTime.Now).ToList();
               
                }
                
                else if (status == "past")
                    bookings = bookings.Where(b => b.EndTime < DateTime.Now).ToList();
            }

            return View(bookings);
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
