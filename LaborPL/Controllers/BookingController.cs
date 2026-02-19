<<<<<<< HEAD
﻿
=======
﻿using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborBLL.Service.Implementation;
using LaborDAL.Entities;
using LaborDAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

>>>>>>> 6d9a1bdd13dff46e91f77821e657eb7a10a75bb9
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
        public async Task<IActionResult> Dashboard(string filter = "all", string role = "all")
        {
            var userId = userManager.GetUserId(User);

            var response = await bookingService.GetBookingsByUserIdAsync(userId);

            if (!response.Success)
                return View(new List<BookingDashboardViewModel>());

            var allBookings = response.Result.AsQueryable();

            if (role?.ToLower() == "worker")
                allBookings = allBookings.Where(b => b.WorkerId == userId);

            if (role?.ToLower() == "poster")
                allBookings = allBookings.Where(b => b.PosterId == userId);

            var list = allBookings.ToList();

            ViewBag.CurrentRole = role;
            ViewBag.CurrentFilter = filter;

            ViewBag.TotalBookings = list.Count();
            ViewBag.UpcomingCount = list.Count(b => b.Status == BookingStatus.Scheduled);
            ViewBag.InProgressCount = list.Count(b => b.Status == BookingStatus.InProgress);
            ViewBag.CompletedCount = list.Count(b => b.Status == BookingStatus.Completed);
            ViewBag.CancelCount = list.Count(b => b.Status == BookingStatus.Cancelled);

            var bookings = list.AsEnumerable();

            switch (filter.ToLower())
            {
                case "upcoming":
                    bookings = bookings.Where(b => b.Status == BookingStatus.Scheduled);
                    break;

                case "inprogress":
                    bookings = bookings.Where(b => b.Status == BookingStatus.InProgress);
                    break;

                case "completed":
                    bookings = bookings.Where(b => b.Status == BookingStatus.Completed);
                    break;

                case "cancel":
                    bookings = bookings.Where(b => b.Status == BookingStatus.Cancelled);
                    break;
            }

            return View(bookings.ToList());
        }


        public async Task<IActionResult> Details(int id)
        {
            var response = await bookingService.GetBookingByIdAsync(id);

            if (!response.Success || response.Result == null)
                return NotFound();

            return View(response.Result);
        }
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {       
            var result = await bookingService.CancelBookingAsync(id);
            if (!result.Success)
                return NotFound();
            TempData["Message"] = "Booking cancelled successfully.";

            return RedirectToAction("Details", new {id=id});
        }
        [HttpPost]
        public async Task <IActionResult> Start( int id)
        {
           var result = await bookingService.StartWorkBookingAsync(id);
            if (!result.Success)
                return NotFound();
            TempData["Message"] = "Booking started successfully.";
            return RedirectToAction("Details", new { id = id });

        }
        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await bookingService.CompleteBookingAsync(id);
            if (!result.Success)
                return NotFound();
            TempData["Message"] = "Booking completed successfully.";
            return RedirectToAction("Details", new { id = id });
        }
        public IActionResult ProfilePoster(string id)
        {
            var user = userManager.FindByIdAsync(id).Result;
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FirstName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                Country= user.Country,

            };
            return View(model);
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
