

using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Enums;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LaborBLL.Service
{
    /// <summary>
    /// Service for user-related business operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly IPenaltyService? _penaltyService;

        public UserService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IMapper mapper,
            ILogger<UserService> logger,
            IPenaltyService? penaltyService = null)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
            _logger = logger;
            _penaltyService = penaltyService;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        public async Task<Response<string>> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                // Check if email already exists in active users
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return new Response<string>(null, false, "Email is already registered.");
                }

                // NEW: Check for soft-deleted user with same email (bypass global query filter)
                var deletedUser = await _unitOfWork.GetDeletedUserByEmailAsync(model.Email);
                if (deletedUser != null)
                {
                    return await ReactivateUserAsync(deletedUser, model);
                }
                var isfirstuser = (await _userManager.Users.CountAsync()) == 0;
                // Map ViewModel to Entity
                var user = _mapper.Map<AppUser>(model);

                // Set user role based on selection
               
                // Create user
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if(model.UserRole == "Worker")
                    {
                        user.Role = ClientRole.Worker;
                    }
                    else if(model.UserRole == "Poster")
                    {
                        user.Role = ClientRole.Poster;
                    }
                    if(model.UserRole == "Both")
                    {
                       await _userManager.AddToRoleAsync(user, "Worker");
                       await _userManager.AddToRoleAsync(user, "Poster");
                       user.Role = ClientRole.Both;
                    }
                    else if (user.Role.HasFlag(ClientRole.Worker))
                    {
                        await _userManager.AddToRoleAsync(user, "Worker");
                    }
                   else if (user.Role.HasFlag(ClientRole.Poster))
                    {
                        await _userManager.AddToRoleAsync(user, "Poster");
                    }
                   
                    
                    //if this is the first user, assign them the Admin role as well
                    if (isfirstuser)
                    {
                        user.Role = ClientRole.Admin | user.Role;
                        await _userManager.UpdateAsync(user);
                        if (!await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            await _userManager.AddToRoleAsync(user, "Admin");
                        }
                        _logger.LogInformation("First user registered, assigned Admin role: {Email}", model.Email);
                    }
                    _logger.LogInformation("User registered successfully: {Email}", model.Email);
                    return new Response<string>(user.Id, true, null);
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed: {Errors}", errors);
                return new Response<string>(null, false, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration: {Email}", model.Email);
                return new Response<string>(null, false, "An error occurred during registration.");
            }
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        public async Task<Response<UserProfileDisplayViewModel>> LoginAsync(LoginViewModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return new Response<UserProfileDisplayViewModel>(null, false, "Invalid email or password.");
                }

                if (user.IsDeleted)
                {
                    return new Response<UserProfileDisplayViewModel>(null, false, "This account has been deactivated.");
                }

                // Check if email is verified
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login attempt for unverified email: {Email}", model.Email);
                    return new Response<UserProfileDisplayViewModel>(null, false, "Please verify your email before logging in. Check your inbox for the verification code.");
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in: {Email}", model.Email);
                    return new Response<UserProfileDisplayViewModel>(_mapper.Map<UserProfileDisplayViewModel>(user), true, null);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {Email}", model.Email);
                    return new Response<UserProfileDisplayViewModel>(null, false, "Account is locked out. Please try again later.");
                }

                return new Response<UserProfileDisplayViewModel>(null, false, "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login: {Email}", model.Email);
                return new Response<UserProfileDisplayViewModel>(null, false, "An error occurred during login.");
            }
        }

        /// <summary>
        /// Gets the profile of a user by their ID
        /// </summary>
        public async Task<UserProfileDisplayViewModel?> GetProfileAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                return _mapper.Map<UserProfileDisplayViewModel>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Gets the profile of a user with all details including ratings and statistics
        /// </summary>
        public async Task<UserProfileDisplayViewModel?> GetProfileWithDetailsAsync(string userId, string? viewerId = null)
        {
            try
            {
                // Use UnitOfWork bypass to get user (bypasses global query filter)
                var user = await _unitOfWork.GetUserByIdBypassFilterAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                var profile = _mapper.Map<UserProfileDisplayViewModel>(user);

                // Get ratings for this user
                var ratings = await _unitOfWork.RatingRepo.GetAllRatingByUserId(userId);
                if (ratings != null && ratings.Any())
                {
                    profile.TotalRatingsCount = ratings.Count;
                    profile.AverageRating = (decimal)ratings.Average(r => r.Score);
                    
                    // Map ratings to AllRatingViewModel
                    profile.RecentRatings = ratings.Select(r => new LaborBLL.ModelVM.AllRatingViewModel
                    {
                        id = r.Id.ToString(),
                        RaterId = r.RaterId,
                        RatedId = r.RateeId,
                        bookingId = r.bookingId,
                        Score = r.Score,
                        comment = r.Comment ?? string.Empty,
                        CreatedAt = r.CreatedAt,
                        // RaterName and RateeName would need to be fetched separately if needed
                        RaterName = string.Empty,
                        RateeName = string.Empty,
                        OverallAverageRating = (int)profile.AverageRating,
                        TotalRatingsReceived = ratings.Count
                    }).ToList();
                }

                // Get Worker statistics (completed jobs and earnings)
                if (profile.IsWorker)
                {
                    var workerBookings = await _unitOfWork.Bookings.GetBookingsByWorkerIdAsync(userId);
                    if (workerBookings != null)
                    {
                        // Count completed jobs (Completed status)
                        profile.CompletedJobsAsWorker = workerBookings
                            .Count(b => b.Status == LaborDAL.Enums.BookingStatus.Completed);
                        
                        // Calculate total earnings from completed jobs
                        profile.TotalEarnings = workerBookings
                            .Where(b => b.Status == LaborDAL.Enums.BookingStatus.Completed)
                            .Sum(b => b.AgreedRate);
                    }
                }

                // Get Poster statistics (tasks posted, hires, total spent)
                if (profile.IsPoster)
                {
                    // Get bookings made by this poster (hires)
                    var posterBookings = await _unitOfWork.Bookings.GetBookingsByPosterIdAsync(userId);
                    if (posterBookings != null)
                    {
                        profile.TotalHires = posterBookings.Count;
                        profile.TotalSpent = posterBookings
                            .Where(b => b.Status == LaborDAL.Enums.BookingStatus.Completed)
                            .Sum(b => b.AgreedRate);
                    }

                    // Get tasks posted by this poster
                    var tasks = await _unitOfWork.Tasks.FindAsync(t => t.PosterId == userId);
                    profile.TasksPosted = tasks?.Count() ?? 0;
                }

                // Get penalty statistics if service is available
                if (_penaltyService != null)
                {
                    var penaltyStats = await _penaltyService.GetPenaltyStatsAsync(userId);
                    profile.StrikeCount = penaltyStats.TotalStrikes;
                    profile.ActiveStrikes = penaltyStats.ActiveStrikes;
                    profile.NoShowCount = penaltyStats.NoShowCount;
                    profile.CancellationCount = penaltyStats.CancellationCount;
                    profile.RecentCancellations = penaltyStats.RecentCancellations;
                    profile.IsSuspended = penaltyStats.IsSuspended;
                    profile.SuspensionEndDate = penaltyStats.SuspensionEndDate;
                    profile.IsPostingRestricted = penaltyStats.IsPostingRestricted;
                    profile.IsAcceptanceRestricted = penaltyStats.IsAcceptanceRestricted;
                    profile.UnacknowledgedPenalties = penaltyStats.UnacknowledgedPenalties;

                    // Calculate account health status
                    if (penaltyStats.IsSuspended)
                        profile.AccountHealthStatus = "Suspended";
                    else if (penaltyStats.ActiveStrikes >= 2)
                        profile.AccountHealthStatus = "Critical";
                    else if (penaltyStats.ActiveStrikes >= 1 || penaltyStats.RecentCancellations > 2)
                        profile.AccountHealthStatus = "Warning";
                    else
                        profile.AccountHealthStatus = "Good";

                    // Get penalty history for admin
                    var activePenalties = await _penaltyService.GetActivePenaltiesAsync(userId);
                    profile.PenaltyHistory = activePenalties.Select(p => new PenaltyDisplayViewModel
                    {
                        Id = p.Id,
                        Type = p.Type.ToString(),
                        Severity = p.Severity.ToString(),
                        Reason = p.Reason,
                        AppliedAt = p.AppliedAt,
                        ExpiresAt = p.ExpiresAt,
                        IsActive = p.IsActive,
                        IsAcknowledged = p.IsAcknowledged,
                        RelatedTaskId = p.RelatedTaskId
                    }).ToList();
                }

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile with details: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Updates a user's profile
        /// </summary>
        public async Task<Response<bool>> UpdateProfileAsync(UserProfileUpdateModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return new Response<bool>(false, false, "User not found.");
                }

                // Update properties
                _mapper.Map(model, user);

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Profile updated: {UserId}", model.Id);
                    return new Response<bool>(true, true, null);
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new Response<bool>(false, false, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile: {UserId}", model.Id);
                return new Response<bool>(false, false, "An error occurred while updating profile.");
            }
        }

        /// <summary>
        /// Gets a user by their email
        /// </summary>
        public async Task<UserProfileDisplayViewModel?> GetByEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                return _mapper.Map<UserProfileDisplayViewModel>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        /// <summary>
        /// Checks if an email is already registered
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null && !user.IsDeleted;
        }

        public Task<bool> LogoutAsync()
        {
            try
            {
                _signInManager.SignOutAsync().Wait();
                _logger.LogInformation("User logged out.");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout.");
                return Task.FromResult(false);

            }
        }

        /// <summary>
        /// Reactivates a soft-deleted user with new registration information
        /// </summary>
        private async Task<Response<string>> ReactivateUserAsync(AppUser deletedUser, RegisterViewModel model)
        {
            try
            {
                _logger.LogInformation("Reactivating soft-deleted user: {Email}", model.Email);

                // Reactivate the user
                deletedUser.IsDeleted = false;
                deletedUser.DeletedAt = null;
                deletedUser.UpdatedAt = DateTime.UtcNow;

                // Update user information with new registration data
                deletedUser.FirstName = model.FirstName;
                deletedUser.LastName = model.LastName;
                deletedUser.PhoneNumber = model.PhoneNumber;
                deletedUser.EmailConfirmed = false; // Require email verification again

                // Update password
                var removePasswordResult = await _userManager.RemovePasswordAsync(deletedUser);
                if (!removePasswordResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove old password for reactivation: {Email}", model.Email);
                    return new Response<string>(null, false, "Failed to reactivate account. Please try again.");
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(deletedUser, model.Password);
                if (!addPasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to set new password during reactivation: {Errors}", errors);
                    return new Response<string>(null, false, $"Password error: {errors}");
                }

                // Update user roles if specified
                if (!string.IsNullOrEmpty(model.UserRole))
                {
                    // Clear existing roles first
                    var existingRoles = await _userManager.GetRolesAsync(deletedUser);
                    if (existingRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(deletedUser, existingRoles);
                    }

                    // Set new role
                    if (model.UserRole == "Worker")
                    {
                        deletedUser.Role = ClientRole.Worker;
                        await _userManager.AddToRoleAsync(deletedUser, "Worker");
                    }
                    else if (model.UserRole == "Poster")
                    {
                        deletedUser.Role = ClientRole.Poster;
                        await _userManager.AddToRoleAsync(deletedUser, "Poster");
                    }
                    else if (model.UserRole == "Both")
                    {
                        deletedUser.Role = ClientRole.Both;
                        await _userManager.AddToRoleAsync(deletedUser, "Worker");
                        await _userManager.AddToRoleAsync(deletedUser, "Poster");
                    }
                }

                // Save changes
                var updateResult = await _userManager.UpdateAsync(deletedUser);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update user during reactivation: {Errors}", errors);
                    return new Response<string>(null, false, $"Reactivation failed: {errors}");
                }

                _logger.LogInformation("User reactivated successfully: {Email}", model.Email);
                return new Response<string>(deletedUser.Id, true, "Welcome back! Your account has been restored successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user reactivation: {Email}", model.Email);
                return new Response<string>(null, false, "An error occurred while restoring your account.");
            }
        }

        /// <summary>
        /// Restores a soft-deleted user by admin action
        /// </summary>
        public async Task<Response<bool>> RestoreUserAsync(string userId)
        {
            try
            {
                // Get user bypassing soft delete filter
                var user = await _unitOfWork.GetDeletedUserByIdAsync(userId);
                if (user == null)
                {
                    return new Response<bool>(false, false, "Deleted user not found.");
                }

                if (!user.IsDeleted)
                {
                    return new Response<bool>(false, false, "User is already active.");
                }

                // Restore the user
                user.IsDeleted = false;
                user.DeletedAt = null;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User restored by admin: {UserId}", userId);
                    return new Response<bool>(true, true, "User restored successfully.");
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new Response<bool>(false, false, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user: {UserId}", userId);
                return new Response<bool>(false, false, "An error occurred while restoring the user.");
            }
        }

        /// <summary>
        /// Generates a password reset token and sends reset email
        /// </summary>
        public async Task<Response<bool>> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || user.IsDeleted)
                {
                    // Don't reveal that the user does not exist
                    _logger.LogInformation("Password reset requested for non-existent email: {Email}", email);
                    return new Response<bool>(true, true, "If an account exists with this email, you will receive password reset instructions.");
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Store token with expiration (for simplicity, we'll use the built-in Identity token)
                // The token is valid for a limited time by default (usually 1 day)
                
                _logger.LogInformation("Password reset token generated for user: {Email}", email);
                
                // Return success with the token (controller will handle email sending)
                return new Response<bool>(true, true, "Password reset instructions have been sent to your email.")
                {
                    Result = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for: {Email}", email);
                return new Response<bool>(false, false, "An error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Resets user password with token
        /// </summary>
        public async Task<Response<bool>> ResetPasswordAsync(string email, string token, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || user.IsDeleted)
                {
                    return new Response<bool>(false, false, "Invalid reset request.");
                }

                // Reset the password
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user: {Email}", email);
                    return new Response<bool>(true, true, "Your password has been reset successfully.");
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Password reset failed for {Email}: {Errors}", email, errors);
                return new Response<bool>(false, false, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for: {Email}", email);
                return new Response<bool>(false, false, "An error occurred while resetting your password.");
            }
        }
    }
}
