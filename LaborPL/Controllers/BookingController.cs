
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





        public IActionResult Index()
        {
            return View();
        }
    }
}
