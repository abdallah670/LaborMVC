using AutoMapper;
using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IVerificationService _verificationService;
        private readonly IAppUserRepository _userRepository;
        private readonly ILogger<AdminController> _logger;
        private readonly IMapper _mapper;

        public AdminController(
            IUserService userService,
            IVerificationService verificationService,
            IAppUserRepository userRepository,
            ILogger<AdminController> logger,
            IMapper mapper)
        {
            _userService = userService;
            _verificationService = verificationService;
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: /Admin/Index
        public async Task<IActionResult> Index()
        {
            // Get statistics for admin dashboard
            var users = await _userRepository.GetAllAsync();
            ViewBag.TotalUsers = users.Count();
            ViewBag.TotalTasks = 0; // TODO: Get from task service
            ViewBag.TotalBookings = 0; // TODO: Get from booking service
            ViewBag.PendingVerifications = users.Count(u => !u.IDVerified);

            return View();
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users(string filter = "all")
        {
            ViewBag.CurrentFilter = filter;
            
            var users = await _userRepository.GetAllAsync();
            
            // Apply filters
            users = filter?.ToLower() switch
            {
                "workers" => users.Where(u => u.Role == LaborDAL.Enums.ClientRole.Worker),
                "posters" => users.Where(u => u.Role == LaborDAL.Enums.ClientRole.Poster),
                "verified" => users.Where(u => u.IDVerified),
                _ => users
            };

            // Statistics
            var allUsers = await _userRepository.GetAllAsync();
            ViewBag.TotalUsers = allUsers.Count();
            ViewBag.WorkersCount = allUsers.Count(u => u.Role == LaborDAL.Enums.ClientRole.Worker);
            ViewBag.PostersCount = allUsers.Count(u => u.Role == LaborDAL.Enums.ClientRole.Poster);
            ViewBag.VerifiedUsers = allUsers.Count(u => u.IDVerified);

            // Map AppUser entities to ProfileViewModel
            var userViewModels = _mapper.Map<IEnumerable<ProfileViewModel>>(users);

            return View(userViewModels);
        }

        // GET: /Admin/Verifications
        public async Task<IActionResult> Verifications(string filter = "pending")
        {
            ViewBag.CurrentFilter = filter;
            
            var users = await _userRepository.GetAllAsync();
            
            // Apply filters based on verification status
            users = filter?.ToLower() switch
            {
                "pending" => users.Where(u => !u.IDVerified && !string.IsNullOrEmpty(u.IDDocumentUrl)),
                "verified" => users.Where(u => u.IDVerified),
                "rejected" => users.Where(u => !u.IDVerified && u.IDDocumentSubmittedAt.HasValue),
                _ => users.Where(u => !u.IDVerified && !string.IsNullOrEmpty(u.IDDocumentUrl))
            };

            // Statistics
            var allUsers = await _userRepository.GetAllAsync();
            ViewBag.PendingCount = allUsers.Count(u => !u.IDVerified && !string.IsNullOrEmpty(u.IDDocumentUrl));
            ViewBag.VerifiedCount = allUsers.Count(u => u.IDVerified);
            ViewBag.RejectedCount = 0; // TODO: Track rejected verifications

            // Map AppUser entities to ProfileViewModel
            var userViewModels = _mapper.Map<IEnumerable<ProfileViewModel>>(users);

            return View(userViewModels);
        }

        // POST: /Admin/ApproveVerification
        [HttpPost]
        public async Task<IActionResult> ApproveVerification(string id)
        {
            try
            {
                await _verificationService.ReviewIDDocumentAsync(id, true);
                TempData["SuccessMessage"] = "User verification approved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving verification for user {UserId}", id);
                TempData["ErrorMessage"] = "Failed to approve verification.";
            }

            return RedirectToAction(nameof(Verifications));
        }

        // POST: /Admin/RejectVerification
        [HttpPost]
        public async Task<IActionResult> RejectVerification(string id)
        {
            try
            {
                await _verificationService.ReviewIDDocumentAsync(id, false);
                TempData["SuccessMessage"] = "User verification rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting verification for user {UserId}", id);
                TempData["ErrorMessage"] = "Failed to reject verification.";
            }

            return RedirectToAction(nameof(Verifications));
        }

        // GET: /Admin/Disputes
        public IActionResult Disputes(string filter = "open")
        {
            ViewBag.CurrentFilter = filter;
            
            // TODO: Get disputes from dispute service
            // For now, return empty view
            ViewBag.Disputes = new List<object>();
            ViewBag.OpenCount = 0;
            ViewBag.UnderReviewCount = 0;
            ViewBag.ResolvedCount = 0;
            ViewBag.TotalDisputes = 0;

            return View();
        }

        // GET: /Admin/DisputeDetails/5
        public IActionResult DisputeDetails(int id)
        {
            // TODO: Get dispute details from service
            return View();
        }
    }
}