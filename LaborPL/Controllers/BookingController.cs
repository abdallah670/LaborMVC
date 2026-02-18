using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborBLL.Service.Implementation;
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
        public async Task<IActionResult> Dashboard(string filter = "all")
        {
            var userId = userManager.GetUserId(User);
            var response = await bookingService.GetBookingsByWorkerIdAsync(userId);

            var allBookings = response.Result;

            ViewBag.CurrentFilter = filter;

            // احسب العدادات من كل الحجوزات
            ViewBag.TotalBookings = allBookings.Count;
            ViewBag.UpcomingCount = allBookings.Count(b => b.Status == BookingStatus.Scheduled);
            ViewBag.InProgressCount = allBookings.Count(b => b.Status == BookingStatus.InProgress);
            ViewBag.CompletedCount = allBookings.Count(b => b.Status == BookingStatus.Completed);

            // بعد كده فلتر للعرض فقط
            var bookings = allBookings;

            switch (filter.ToLower())
            {
                case "upcoming":
                    bookings = allBookings.Where(b => b.Status == BookingStatus.Scheduled).ToList();
                    break;

                case "inprogress":
                    bookings = allBookings.Where(b => b.Status == BookingStatus.InProgress).ToList();
                    break;

                case "completed":
                    bookings = allBookings.Where(b => b.Status == BookingStatus.Completed).ToList();
                    break;
            }

            return View(bookings);
        }
        public async Task< IActionResult> Details(int id)
        {

            var booking = await bookingService.GetBookingByIdAsync(id);
            if (booking == null)
                return NotFound();

            return View(booking.Result);
        }




        public IActionResult Index()
        {
            return View();
        }
    }
}
