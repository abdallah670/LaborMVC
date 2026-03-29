using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IBookingService _bookingService;
        private readonly IApplicationService _applicationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ITaskService taskService,
            IBookingService bookingService,
            IApplicationService applicationService,
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            ILogger<DashboardController> logger)
        {
            _taskService = taskService;
            _bookingService = bookingService;
            _applicationService = applicationService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Main dashboard entry point - redirects to role-specific dashboard
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Redirect based on role
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Admin));
            }
            else if (User.IsInRole("Worker") && User.IsInRole("Poster"))
            {
                // User has both roles, show combined dashboard
                return RedirectToAction(nameof(Combined));
            }
            else if (User.IsInRole("Worker"))
            {
                return RedirectToAction(nameof(Worker));
            }
            else if (User.IsInRole("Poster"))
            {
                return RedirectToAction(nameof(Poster));
            }

            // Default fallback
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Worker Dashboard - Shows bookings, applications, and available tasks
        /// </summary>
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> Worker()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Get worker's bookings
                var bookingsResponse = await _bookingService.GetBookingsByWorkerIdAsync(userId);
                var bookings = bookingsResponse.Success ? bookingsResponse.Result ?? new List<BookingDashboardViewModel>() : new List<BookingDashboardViewModel>();

                // Get worker's applications
                var applicationsResponse = await _applicationService.GetApplicationsByWorkerAsync(userId);
                var applications = applicationsResponse.Success ? applicationsResponse.Result ?? new List<TaskApplicationViewModel>() : new List<TaskApplicationViewModel>();

                // Get available tasks for worker to browse
                var searchModel = new TaskSearchViewModel
                {
                    Page = 1,
                    PageSize = 5,
                    SortBy = "newest"
                };
                var tasksResponse = await _taskService.GetTaskListAsync(searchModel, userId);
                var availableTasks = tasksResponse.Success && tasksResponse.Result != null ? tasksResponse.Result.Results : new List<TaskListViewModel>();

                // Populate ViewModel
                var viewModel = new WorkerDashboardViewModel
                {
                    TotalBookings = bookings.Count(),
                    ActiveBookings = bookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Scheduled),
                    CompletedBookings = bookings.Count(b => b.Status == LaborDAL.Enums.BookingStatus.Completed),
                    PendingApplications = applications.Count(a => a.Status == LaborDAL.Enums.ApplicationStatus.Pending),
                    TotalApplications = applications.Count(),
                    RecentBookings = bookings.Take(5).ToList(),
                    RecentApplications = applications.Take(5).ToList(),
                    AvailableTasks = availableTasks
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading worker dashboard for user {UserId}", userId);
                TempData["Error"] = "Failed to load dashboard. Please try again.";
                return View(new WorkerDashboardViewModel());
            }
        }

        /// <summary>
        /// Poster Dashboard - Shows posted tasks, applications received, and active bookings
        /// </summary>
        [Authorize(Roles = "Poster")]
        public async Task<IActionResult> Poster()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Get poster's tasks
                var tasksResponse = await _taskService.GetMyTasksAsync(userId);
                var tasks = tasksResponse.Success ? tasksResponse.Result ?? new List<TaskListViewModel>() : new List<TaskListViewModel>();

                // Get poster's bookings
                var bookingsResponse = await _bookingService.GetBookingsByPosterIdAsync(userId);
                var bookings = bookingsResponse.Success ? bookingsResponse.Result ?? new List<BookingDashboardViewModel>() : new List<BookingDashboardViewModel>();

                // Get all applications for poster's tasks
                var allApplications = new List<TaskApplicationViewModel>();
                foreach (var task in tasks)
                {
                    var appsResponse = await _applicationService.GetApplicationsByTaskAsync(task.Id, userId);
                    if (appsResponse.Success && appsResponse.Result != null)
                    {
                        allApplications.AddRange(appsResponse.Result);
                    }
                }

                // Calculate statistics
                ViewBag.TotalTasks = tasks.Count();
                ViewBag.ActiveTasks = tasks.Count(t => t.Status == LaborDAL.Enums.TaskStatus.Open || t.Status == LaborDAL.Enums.TaskStatus.InProgress);
                ViewBag.CompletedTasks = tasks.Count(t => t.Status == LaborDAL.Enums.TaskStatus.Completed);
                ViewBag.PendingApplications = allApplications.Count(a => a.Status == ApplicationStatus.Pending);
                ViewBag.TotalApplications = allApplications.Count();
                ViewBag.ActiveBookings = bookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Scheduled);

                // Pass data to view
                ViewBag.Tasks = tasks.Take(5).ToList();
                ViewBag.Applications = allApplications.Where(a => a.Status == LaborDAL.Enums.ApplicationStatus.Pending).Take(5).ToList();
                ViewBag.Bookings = bookings.Take(5).ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading poster dashboard for user {UserId}. Exception: {Message}\nStackTrace: {StackTrace}", userId, ex.Message, ex.StackTrace);
                TempData["ErrorMessage"] = $"Failed to load dashboard: {ex.Message}";
                return View();
            }
        }

        /// <summary>
        /// Admin Dashboard - Shows platform statistics and management links
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            try
            {
                // Get all users
                var users = await _unitOfWork.AppUsers.GetAllAsync();
                var userList = users.ToList();

                // Get all tasks
                var allTasks = new List<TaskListViewModel>();
                var usersWithTasks = userList.Where(u => u.Role.HasFlag(LaborDAL.Enums.ClientRole.Poster));
                foreach (var poster in usersWithTasks)
                {
                    var tasksResponse = await _taskService.GetMyTasksAsync(poster.Id);
                    if (tasksResponse.Success && tasksResponse.Result != null)
                    {
                        allTasks.AddRange(tasksResponse.Result);
                    }
                }

                // Get all bookings
                var allBookingsResponse = await _bookingService.GetAllBookingAsync();
                var allBookings = allBookingsResponse.Success ? allBookingsResponse.Result ?? new List<BookingDetailViewModel>() : new List<BookingDetailViewModel>();

                // Calculate statistics
                ViewBag.TotalUsers = userList.Count;
                ViewBag.TotalWorkers = userList.Count(u => u.Role.HasFlag(ClientRole.Worker));
                ViewBag.TotalPosters = userList.Count(u => u.Role.HasFlag(ClientRole.Poster));
                ViewBag.TotalTasks = allTasks.Count;
                ViewBag.ActiveTasks = allTasks.Count(t => t.Status == LaborDAL.Enums.TaskStatus.Open || t.Status == LaborDAL.Enums.TaskStatus.InProgress);
                ViewBag.TotalBookings = allBookings.Count;
                ViewBag.ActiveBookings = allBookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Scheduled);
                ViewBag.CompletedBookings = allBookings.Count(b => b.Status == BookingStatus.Completed);
                ViewBag.PendingVerifications = userList.Count(u => !u.IDVerified && !string.IsNullOrEmpty(u.IDDocumentUrl));
                ViewBag.VerifiedUsers = userList.Count(u => u.IDVerified);
                ViewBag.TotalRevenue = allBookings.Sum(b => b.AgreedRate);

                // Recent activity (last 5 of each)
                ViewBag.RecentUsers = userList.OrderByDescending(u => u.CreatedAt).Take(5).ToList();
                ViewBag.RecentTasks = allTasks.OrderByDescending(t => t.CreatedAt).Take(5).ToList();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Failed to load dashboard. Please try again.";
                return View();
            }
        }

        /// <summary>
        /// Combined Dashboard - Shows both Worker and Poster content for users with both roles
        /// </summary>
        [Authorize(Roles = "Worker,Poster,Admin")]
        public async Task<IActionResult> Combined()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Get Worker data
                var workerBookingsResponse = await _bookingService.GetBookingsByWorkerIdAsync(userId);
                var workerBookings = workerBookingsResponse.Success ? workerBookingsResponse.Result ?? new List<BookingDashboardViewModel>() : new List<BookingDashboardViewModel>();

                var workerApplicationsResponse = await _applicationService.GetApplicationsByWorkerAsync(userId);
                var workerApplications = workerApplicationsResponse.Success ? workerApplicationsResponse.Result ?? new List<TaskApplicationViewModel>() : new List<TaskApplicationViewModel>();

                // Get Poster data
                var posterTasksResponse = await _taskService.GetMyTasksAsync(userId);
                var posterTasks = posterTasksResponse.Success ? posterTasksResponse.Result ?? new List<TaskListViewModel>() : new List<TaskListViewModel>();

                var posterBookingsResponse = await _bookingService.GetBookingsByPosterIdAsync(userId);
                var posterBookings = posterBookingsResponse.Success ? posterBookingsResponse.Result ?? new List<BookingDashboardViewModel>() : new List<BookingDashboardViewModel>();

                // Get all applications for poster's tasks
                var allPosterApplications = new List<TaskApplicationViewModel>();
                foreach (var task in posterTasks)
                {
                    var appsResponse = await _applicationService.GetApplicationsByTaskAsync(task.Id, userId);
                    if (appsResponse.Success && appsResponse.Result != null)
                    {
                        allPosterApplications.AddRange(appsResponse.Result);
                    }
                }

                // Calculate Worker stats
                ViewBag.WorkerTotalBookings = workerBookings.Count();
                ViewBag.WorkerActiveBookings = workerBookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Scheduled);
                ViewBag.WorkerPendingApplications = workerApplications.Count(a => a.Status == ApplicationStatus.Pending);

                // Calculate Poster stats
                ViewBag.PosterTotalTasks = posterTasks.Count();
                ViewBag.PosterActiveTasks = posterTasks.Count(t => t.Status == LaborDAL.Enums.TaskStatus.Open || t.Status == LaborDAL.Enums.TaskStatus.InProgress);
                ViewBag.PosterPendingApplications = allPosterApplications.Count(a => a.Status == ApplicationStatus.Pending);

                // Combined stats
                ViewBag.TotalActiveBookings = workerBookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Scheduled)
                    + posterBookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.Scheduled);
                ViewBag.TotalCompleted = workerBookings.Count(b => b.Status == BookingStatus.Completed)
                    + posterBookings.Count(b => b.Status == BookingStatus.Completed);

                // Pass data to view
                ViewBag.WorkerBookings = workerBookings;
                ViewBag.WorkerApplications = workerApplications;
                ViewBag.PosterTasks = posterTasks;
                ViewBag.PosterApplications = allPosterApplications.Where(a => a.Status == ApplicationStatus.Pending).ToList();
                ViewBag.PosterBookings = posterBookings;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading combined dashboard for user {UserId}. Exception: {Message}\nStackTrace: {StackTrace}", userId, ex.Message, ex.StackTrace);
                TempData["ErrorMessage"] = $"Failed to load dashboard: {ex.Message}";
                return View();
            }
        }
    }
}
