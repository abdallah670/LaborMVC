

using LaborBLL.Service.Implementation;
using LaborDAL.Repo.Abstract;
using LaborDAL.Repo.Implementation;

namespace LaborPL.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService bookingService;
        private readonly IConfiguration configuration;
        private readonly IDisputeService disputeService;
        private readonly UserManager<AppUser> userManager;
        private readonly IRatingService ratingService;
        private readonly IEscrowService escrowService;
        private readonly IPaymentService paymentService;
        private readonly IUserService userService;

        public BookingController(IBookingService bookingService, IConfiguration configuration, IDisputeService disputeService, UserManager<AppUser> userManager ,IRatingService ratingService,IEscrowService escrowService,IPaymentService paymentService, IUserService userService)
        {
            this.bookingService = bookingService;
            this.configuration = configuration;
            this.disputeService = disputeService;
            this.userManager = userManager;
            this.ratingService = ratingService;
            this.escrowService = escrowService;
            this.paymentService = paymentService;
            this.userService = userService;
        }
        #region Creat Booking
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

            return RedirectToAction("Checkout", "Payment", new { bookingId = result.Result });
        }
        #endregion


        #region Dashboard
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Dashboard(string filter = "all", string role = "all", string? search = null)
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

            // Apply keyword search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                allBookings = allBookings.Where(b => 
                    (b.TaskTitle != null && b.TaskTitle.ToLower().Contains(lowerSearch)) || 
                    (b.WorkerName != null && b.WorkerName.ToLower().Contains(lowerSearch)) ||
                    (b.PosterName != null && b.PosterName.ToLower().Contains(lowerSearch)));
            }

            var list = allBookings.ToList();

            ViewBag.CurrentRole = role;
            ViewBag.CurrentFilter = filter;
            ViewBag.CurrentSearch = search;

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
        #endregion


        #region Details
        public async Task<IActionResult> Details(int id)
        {
            var response = await bookingService.GetBookingByIdAsync(id);

            if (!response.Success || response.Result == null)
                return NotFound();

            decimal penalty = response.Result.AgreedRate * 0.10m;
            ViewBag.Penalty = penalty;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isPoster = currentUserId == response.Result.PosterId;
            var rateeId = isPoster ? response.Result.WorkerId : response.Result.PosterId;

            var existingRating = await ratingService.GetRatingAsync(currentUserId, rateeId, id);
            ViewBag.ExistingScore = existingRating?.Score ?? 0;
            ViewBag.RatedId = rateeId;

            // ✅ أضف السطرين دول بس
            var otherUser = await userManager.FindByIdAsync(rateeId);
            ViewBag.OtherUserProfilePicture = otherUser?.ProfilePictureUrl;

            return View(response.Result);
        }
        #endregion
        #region Cancell


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = userManager.GetUserId(User);

            var booking = await bookingService.GetBookingByIdAsync(id);
            if (!booking.Success || booking.Result == null)
                return NotFound();

            if (booking.Result.PosterId != userId && booking.Result.WorkerId != userId)
                return Forbid();

            // حساب الوقت المتبقي
            TimeSpan timeUntilStart;
            if (booking.Result.StartTime.HasValue)
            {
                timeUntilStart = booking.Result.StartTime.Value - DateTime.Now;
            }
            else
            {
                timeUntilStart = TimeSpan.Zero;
            }

            bool isWorker = booking.Result.WorkerId == userId;
            decimal penaltyPercentage = 0;
            int ratingScore = 5;
            string ratingComment = "";
            bool applyRating = false;

            // حساب نسبة الغرامة وتقييم العامل
            if (isWorker) // العامل ألغى
            {
                if (timeUntilStart.TotalHours > 24)
                {
                    penaltyPercentage = 0;
                }
                else if (timeUntilStart.TotalHours > 12)
                {
                    penaltyPercentage = 0.05m;
                    ratingScore = 2;
                    ratingComment = "System auto-rating: Worker cancelled late";
                    applyRating = true;
                }
                else if (timeUntilStart.TotalHours > 2)
                {
                    penaltyPercentage = 0.10m;
                    ratingScore = 1;
                    ratingComment = "System auto-rating: Worker cancelled very late";
                    applyRating = true;
                }
                else
                {
                    penaltyPercentage = 0.20m;
                    ratingScore = 1;
                    ratingComment = "System auto-rating: Worker cancelled at last minute";
                    applyRating = true;
                }
            }
            else // العميل ألغى
            {
                if (timeUntilStart.TotalHours > 48)
                {
                    penaltyPercentage = 0;
                }
                else if (timeUntilStart.TotalHours > 24)
                {
                    penaltyPercentage = 0.10m;
                    ratingScore = 5;
                    ratingComment = "System auto-rating: Worker compensated for client cancellation";
                    applyRating = true;
                }
                else if (timeUntilStart.TotalHours > 12)
                {
                    penaltyPercentage = 0.25m;
                    ratingScore = 5;
                    ratingComment = "System auto-rating: Worker compensated for client cancellation";
                    applyRating = true;
                }
                else if (timeUntilStart.TotalHours > 2)
                {
                    penaltyPercentage = 0.50m;
                    ratingScore = 5;
                    ratingComment = "System auto-rating: Worker compensated for client cancellation";
                    applyRating = true;
                }
                else
                {
                    penaltyPercentage = 0.75m;
                    ratingScore = 5;
                    ratingComment = "System auto-rating: Worker compensated for client last-minute cancellation";
                    applyRating = true;
                }
            }

            decimal totalAmount = booking.Result.AgreedRate;
            var penaltyAmount = totalAmount * penaltyPercentage;
            var refundAmount = totalAmount - penaltyAmount;

            // تطبيق التقييم التلقائي باستخدام الـ Admin
            if (applyRating)
            {
                try
                {
                    // ID حق System Admin من قاعدة البيانات
                    string adminId = "166dcbba-3f40-4e1b-a760-278b03e4e938";

                    var ratingModel = new RatingViewModel
                    {
                        RatedId = booking.Result.WorkerId,  // العامل اللي هيتقيم
                        bookingId = id,
                        Score = ratingScore,
                        comment = ratingComment
                    };

                    await ratingService.SubmitOrUpdateRatingAsync(ratingModel, adminId);
                }
                catch (Exception ex)
                {
                    // لو فشل التقييم، سجل الخطأ ومتوقفش العملية
                    Console.WriteLine($"Auto-rating failed: {ex.Message}");
                }
            }

            // إلغاء الحجز
            var result = await bookingService.CancelBookingAsync(id, userId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Details", new { id });
            }

            // معالجة المبلغ
            await escrowService.ProcessCancellationAsync(id, userId);

            // الرسالة المناسبة للمستخدم
            if (isWorker)
            {
                if (penaltyAmount > 0)
                    TempData["Message"] = $"⚠️ Booking cancelled. Penalty: {penaltyAmount:C}. Rating reduced automatically.";
                else
                    TempData["Message"] = $"✅ Booking cancelled. No penalty. Rating not affected.";
            }
            else
            {
                if (penaltyAmount > 0)
                    TempData["Message"] = $"⚠️ Booking cancelled. Refund: {refundAmount:C}, Penalty: {penaltyAmount:C}. Worker rating increased automatically.";
                else
                    TempData["Message"] = $"✅ Booking cancelled. Full refund: {refundAmount:C}. Rating not affected.";
            }

            return RedirectToAction("Details", new { id });
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> UpdatePrice(UpdateBookingViewModel model)
        {
            await bookingService.UpdateBookingAsync(model);
            return RedirectToAction("Details",new {id=model.Id});

        }
        #region Start Work

        [HttpPost]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> Start(int id)
        {
            var result = await bookingService.StartWorkBookingAsync(id);


            if (!result.Success)
                return NotFound();
            TempData["Message"] = "Booking started successfully.";
            return RedirectToAction("Details", new { id = id });

        }
        #endregion

        #region Complete Work by worker
        [HttpPost]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await bookingService.CompleteBookingByWorkerAsync(id);
            if (result.Success == false)
                return NotFound();
            TempData["Message"] = "Marked as completed. Waiting for poster confirmation.";
            return RedirectToAction("Details", new { id = id });
        }
        #endregion

        #region confirm Complete work by poster
        [HttpPost]
        [Authorize(Roles = "Poster")]
        public async Task<IActionResult> ConfirmCompletion(int id)
        {
            var result = await bookingService.CompleteBookingByPosterAsync(id);
            if (result.Success == false)
                return NotFound();
            TempData["Message"] = "Booking confirmed as completed. Thank you for using our service!";
            await escrowService.ReleasePaymentAsync(id);

            return RedirectToAction("Details", new { id = id });
        }

        #endregion

        #region poster and worker can see other profile
        public async Task<IActionResult> ProfilePoster(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();

            var model = await userService.GetProfileWithDetailsAsync(id);
            if (model == null) return NotFound();

            return View(model);
        } 
        #endregion


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


        #region Rating
        public async Task<IActionResult> Rate(RatingViewModel model)
        {
            var userId = userManager.GetUserId(User);
            if (userId == null)
                return RedirectToAction("Login", "Account");
            var result = await ratingService.SubmitOrUpdateRatingAsync(model, userId);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = "Failed to submit rating. Please try again.";
                return RedirectToAction(nameof(Details), new { id = model.bookingId });
            }
            TempData["SuccessMessage"] = "Your rating has been submitted successfully.";
            return RedirectToAction(nameof(Details), new { id = model.bookingId });
        }
        #endregion
        #region Stripe Connect

        [HttpGet]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> ConnectStripe()
        {
            var userId = userManager.GetUserId(User);
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            if (!string.IsNullOrEmpty(user.StripeAccountId))
            {
                TempData["Message"] = "✅ Your Stripe account is already connected.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                var stripeService = new StripeService(configuration);

                var accountId = await stripeService.CreateConnectAccountAsync(
                    user.Email,
                    user.FirstName ?? user.UserName,
                    user.LastName ?? ""
                );

                user.StripeAccountId = accountId;
                await userManager.UpdateAsync(user);

                var accountLink = await stripeService.CreateAccountLinkAsync(
                    accountId,
                    Url.Action("StripeOnboardingFailed", "Booking", null, Request.Scheme),
                    Url.Action("StripeOnboardingCompleted", "Booking", null, Request.Scheme)
                );

                return Redirect(accountLink);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> StripeOnboardingCompleted()
        {
            TempData["Message"] = "✅ Stripe account connected successfully! You can now receive payments.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Authorize(Roles = "Worker")]
        public IActionResult StripeOnboardingFailed()
        {
            TempData["ErrorMessage"] = "❌ Failed to connect Stripe account. Please try again.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> CheckStripeStatus()
        {
            var userId = userManager.GetUserId(User);
            var user = await userManager.FindByIdAsync(userId);

            if (string.IsNullOrEmpty(user?.StripeAccountId))
            {
                return Json(new { hasAccount = false, message = "No Stripe account connected" });
            }

            var stripeService = new StripeService(configuration);
            var isEnabled = await stripeService.IsAccountEnabledAsync(user.StripeAccountId);

            return Json(new
            {
                hasAccount = true,
                isEnabled = isEnabled,
                message = isEnabled ? "Account is active" : "Account needs verification"
            });
        }

        #endregion
        public IActionResult Index()
        {
            return View();
        }
    }
}
