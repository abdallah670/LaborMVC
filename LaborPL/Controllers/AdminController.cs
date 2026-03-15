using AutoMapper;
using LaborBLL.ModelVM;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LaborPL.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IVerificationService _verificationService;
        private readonly IAppUserRepository _userRepository;
        private readonly IRoleService _roleService;
        private readonly IDisputeService _disputeService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(
            IUserService userService,
            IVerificationService verificationService,
            IAppUserRepository userRepository,
            IRoleService roleService,
            IDisputeService disputeService,
            UserManager<AppUser> userManager,
            ILogger<AdminController> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _userService = userService;
            _verificationService = verificationService;
            _userRepository = userRepository;
            _roleService = roleService;
            _disputeService = disputeService;
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        // GET: /Admin/Index
        public async Task<IActionResult> Index()
        {
            // Get statistics for admin dashboard
            var users = await _userRepository.GetAllAsync();
            ViewBag.TotalUsers = users.Count();
            ViewBag.TotalTasks = 0; // TODO: Get from task service
            ViewBag.TotalBookings = 0; // TODO: Get from booking service
            ViewBag.PendingVerifications = await _unitOfWork.IDVerifications.GetPendingVerificationsAsync().ContinueWith(t => t.Result.Count());

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
                "workers" => users.Where(u => u.Role.HasFlag(LaborDAL.Enums.ClientRole.Worker)),
                "posters" => users.Where(u => u.Role.HasFlag(LaborDAL.Enums.ClientRole.Poster)),
                "verified" => users.Where(u => u.IDVerified),
                _ => users
            };

            // Statistics
            var allUsers = await _userRepository.GetAllAsync();
            ViewBag.TotalUsers = allUsers.Count();
            ViewBag.WorkersCount = allUsers.Count(u => u.Role.HasFlag(LaborDAL.Enums.ClientRole.Worker));
            ViewBag.PostersCount = allUsers.Count(u => u.Role.HasFlag(LaborDAL.Enums.ClientRole.Poster));
            ViewBag.VerifiedUsers = allUsers.Count(u => u.IDVerified);

            // Map AppUser entities to ProfileViewModel
            var userViewModels = _mapper.Map<IEnumerable<ProfileViewModel>>(users);

            return View(userViewModels);
        }

        #region ID Verification Management

        // GET: /Admin/IdVerifications
        public async Task<IActionResult> IdVerifications(string filter = "pending")
        {
            ViewBag.CurrentFilter = filter;

            IEnumerable<IDVerification> verifications;

            // Apply filters
            verifications = filter?.ToLower() switch
            {
                "pending" => await _unitOfWork.IDVerifications.GetPendingVerificationsAsync(),
                "approved" => await _unitOfWork.IDVerifications.GetByStatusAsync(VerificationStatus.Approved),
                "rejected" => await _unitOfWork.IDVerifications.GetByStatusAsync(VerificationStatus.Rejected),
                _ => await _unitOfWork.IDVerifications.GetPendingVerificationsAsync()
            };

            // Statistics
            var pending = await _unitOfWork.IDVerifications.GetPendingVerificationsAsync();
            var approved = await _unitOfWork.IDVerifications.GetByStatusAsync(VerificationStatus.Approved);
            var rejected = await _unitOfWork.IDVerifications.GetByStatusAsync(VerificationStatus.Rejected);

            ViewBag.PendingCount = pending.Count();
            ViewBag.ApprovedCount = approved.Count();
            ViewBag.RejectedCount = rejected.Count();

            return View(verifications);
        }

        // GET: /Admin/ReviewIdVerification/5
        public async Task<IActionResult> ReviewIdVerification(int id)
        {
            var verification = await _unitOfWork.IDVerifications.GetByIdAsync(id);
            if (verification == null)
            {
                return NotFound();
            }

            // Load user information
            var user = await _userManager.FindByIdAsync(verification.UserId);
            if (user != null)
            {
                ViewBag.UserName = $"{user.FirstName} {user.LastName}";
                ViewBag.UserEmail = user.Email;
            }

            return View(verification);
        }

        // POST: /Admin/ApproveIdVerification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveIdVerification(int id, string? notes)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(adminId))
                {
                    TempData["ErrorMessage"] = "Admin authentication required.";
                    return RedirectToAction(nameof(IdVerifications));
                }

                var result = await _verificationService.ApproveIdVerificationAsync(id, adminId, notes);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "ID verification approved successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to approve verification.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving ID verification {VerificationId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the verification.";
            }

            return RedirectToAction(nameof(IdVerifications));
        }

        // POST: /Admin/RejectIdVerification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectIdVerification(int id, string reason, string? notes)
        {
            try
            {
                var adminId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(adminId))
                {
                    TempData["ErrorMessage"] = "Admin authentication required.";
                    return RedirectToAction(nameof(IdVerifications));
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "Rejection reason is required.";
                    return RedirectToAction(nameof(ReviewIdVerification), new { id });
                }

                var result = await _verificationService.RejectIdVerificationAsync(id, adminId, reason, notes);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "ID verification rejected.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to reject verification.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting ID verification {VerificationId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the verification.";
            }

            return RedirectToAction(nameof(IdVerifications));
        }

        #endregion

        // GET: /Admin/Verifications (Legacy - kept for compatibility)
        public async Task<IActionResult> Verifications(string filter = "pending")
        {
            ViewBag.CurrentFilter = filter;

            var users = await _userRepository.GetAllAsync();

            // Apply filters based on verification status
            users = filter?.ToLower() switch
            {
                "pending" => users.Where(u => !u.IDVerified && !string.IsNullOrEmpty(u.IDDocumentUrl)),
                "verified" => users.Where(u => u.IDVerified),
                "rejected" => users.Where(u => !u.IDVerified ),
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

        // POST: /Admin/ApproveVerification (Legacy - kept for compatibility)
        [HttpPost]
        public async Task<IActionResult> ApproveVerification(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Verifications));
                }

                user.IDVerified = true;
                user.VerificationTier = VerificationTier.IDVerified;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {UserId} ID verification approved by admin", id);
                TempData["SuccessMessage"] = "User verification approved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving verification for user {UserId}", id);
                TempData["ErrorMessage"] = "Failed to approve verification.";
            }

            return RedirectToAction(nameof(Verifications));
        }

        // POST: /Admin/RejectVerification (Legacy - kept for compatibility)
        [HttpPost]
        public async Task<IActionResult> RejectVerification(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Verifications));
                }

                // Clear ID document URL to require resubmission
                user.IDDocumentUrl = null;
                user.IDDocumentSubmittedAt = null;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {UserId} ID verification rejected by admin", id);
                TempData["SuccessMessage"] = "User verification rejected. Document upload required again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting verification for user {UserId}", id);
                TempData["ErrorMessage"] = "Failed to reject verification.";
            }

            return RedirectToAction(nameof(Verifications));
        }

        // GET: /Admin/Disputes
        public async Task<IActionResult> Disputes(string filter = "open")
        {
            ViewBag.CurrentFilter = filter;

            // Get dispute statistics
            var stats = await _disputeService.GetDisputeStatsAsync();
            ViewBag.OpenCount = stats["Open"];
            ViewBag.UnderReviewCount = stats["UnderReview"];
            ViewBag.ResolvedCount = stats["Resolved"];
            ViewBag.TotalDisputes = stats["Total"];

            // Get disputes based on filter
            DisputeStatus? statusFilter = filter?.ToLower() switch
            {
                "open" => DisputeStatus.Open,
                "underreview" => DisputeStatus.UnderReview,
                "resolved" => DisputeStatus.Resolved,
                _ => null
            };

            var disputes = await _disputeService.GetAllDisputesAsync(statusFilter);
            return View(disputes);
        }

        // GET: /Admin/DisputeDetails/5
        public async Task<IActionResult> DisputeDetails(int id)
        {
            var dispute = await _disputeService.GetDisputeDetailsAsync(id);
            if (dispute == null)
            {
                return NotFound();
            }

            return View(dispute);
        }

        // POST: /Admin/UpdateDisputeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDisputeStatus(int disputeId, DisputeStatus status)
        {
            var result = await _disputeService.UpdateStatusAsync(disputeId, status);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Dispute status updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to update dispute status.";
            }

            return RedirectToAction(nameof(DisputeDetails), new { id = disputeId });
        }

        // POST: /Admin/ResolveDispute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(ResolveDisputeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var dispute = await _disputeService.GetDisputeDetailsAsync(model.DisputeId);
                if (dispute != null)
                {
                    model.TaskTitle = dispute.TaskTitle;
                    model.AgreedRate = dispute.AgreedRate;
                    model.PosterName = dispute.PosterName;
                    model.WorkerName = dispute.WorkerName;
                }
                return View("DisputeDetails", model);
            }

            var adminId = _userManager.GetUserId(User);
            var result = await _disputeService.ResolveDisputeAsync(model, adminId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Dispute resolved successfully.";
                return RedirectToAction(nameof(Disputes));
            }

            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to resolve dispute.";
            return RedirectToAction(nameof(DisputeDetails), new { id = model.DisputeId });
        }

        #region User Management Actions

        // GET: /Admin/UserDetails/{id}
        public async Task<IActionResult> UserDetails(string id)
        {

            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User ID is required.");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            // Use the new method to get full profile with ratings and stats
            var viewModel = await _userService.GetProfileWithDetailsAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        // GET: /Admin/EditRoles/{id}
        public async Task<IActionResult> EditRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User ID is required.");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            var viewModel = new EditRolesViewModel
            {
                UserId = user.Id,
                UserName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsWorker = user.Role.HasFlag(ClientRole.Worker),
                IsPoster = user.Role.HasFlag(ClientRole.Poster),
                IsAdmin = user.Role.HasFlag(ClientRole.Admin),
                CurrentRole = user.Role
            };

            return View(viewModel);
        }

        // POST: /Admin/EditRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditRolesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Calculate new role
            var newRole = model.GetNewRole();

            // Ensure at least one role is assigned
            if (newRole == ClientRole.None)
            {
                ModelState.AddModelError("", "At least one role must be assigned.");
                return View(model);
            }

            var result = await _roleService.SetRolesAsync(model.UserId, newRole);

            if (result)
            {
                _logger.LogInformation("User {UserId} roles updated to {NewRole} by admin", model.UserId, newRole);
                TempData["SuccessMessage"] = "User roles updated successfully.";
                return RedirectToAction(nameof(Users));
            }

            ModelState.AddModelError("", "Failed to update user roles.");
            return View(model);
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User ID is required.");
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent self-deletion
            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            // Prevent deleting other admins (optional safety measure)
            if (user.Role.HasFlag(ClientRole.Admin))
            {
                TempData["ErrorMessage"] = "Cannot delete administrator accounts. Remove admin role first.";
                return RedirectToAction(nameof(Users));
            }

            // Soft delete
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = currentUserId;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} was soft-deleted by admin {AdminId}", id, currentUserId);
            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction(nameof(Users));
        }

        #endregion
    }
}
