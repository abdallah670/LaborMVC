using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborBLL.Service.Implementation;
using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService bookingService;
        private readonly IDisputeService disputeService;
        private readonly UserManager<AppUser> userManager;

        public BookingController(IBookingService bookingService, IDisputeService disputeService, UserManager<AppUser> userManager)
        {
            this.bookingService = bookingService;
            this.disputeService = disputeService;
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

        #region Dispute Actions

        // GET: /Booking/RaiseDispute/{bookingId}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RaiseDispute(int bookingId)
        {
            var userId = userManager.GetUserId(User);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Check if user can raise dispute
            var canRaise = await disputeService.CanRaiseDisputeAsync(bookingId, userId);
            if (!canRaise)
            {
                TempData["ErrorMessage"] = "You cannot raise a dispute for this booking. Disputes can only be raised within 48 hours of completion.";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            var model = new CreateDisputeViewModel
            {
                BookingId = bookingId
            };

            ViewBag.BookingId = bookingId;
            return View(model);
        }

        // POST: /Booking/RaiseDispute
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RaiseDispute(CreateDisputeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = userManager.GetUserId(User);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var result = await disputeService.RaiseDisputeAsync(model, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Your dispute has been raised successfully. An administrator will review it shortly.";
                return RedirectToAction(nameof(Dashboard));
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Failed to raise dispute.");
            return View(model);
        }

        #endregion




        public IActionResult Index()
        {
            return View();
        }
    }
}
