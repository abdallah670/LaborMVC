

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

        public UserService(
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        public async Task<Response<string>> RegisterAsync(RegisterViewModel model)
        {
            try
            {

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return new Response<string>(null, false, "Email is already registered.");
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
        public async Task<Response<ProfileViewModel>> LoginAsync(LoginViewModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return new Response<ProfileViewModel>(null, false, "Invalid email or password.");
                }

                if (user.IsDeleted)
                {
                    return new Response<ProfileViewModel>(null, false, "This account has been deactivated.");
                }

                // Check if email is verified
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login attempt for unverified email: {Email}", model.Email);
                    return new Response<ProfileViewModel>(null, false, "Please verify your email before logging in. Check your inbox for the verification code.");
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in: {Email}", model.Email);
                    return new Response<ProfileViewModel>(_mapper.Map<ProfileViewModel>(user), true, null);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {Email}", model.Email);
                    return new Response<ProfileViewModel>(null, false, "Account is locked out. Please try again later.");
                }

                return new Response<ProfileViewModel>(null, false, "Invalid email or password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login: {Email}", model.Email);
                return new Response<ProfileViewModel>(null, false, "An error occurred during login.");
            }
        }

        /// <summary>
        /// Gets the profile of a user by their ID
        /// </summary>
        public async Task<ProfileViewModel?> GetProfileAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                return _mapper.Map<ProfileViewModel>(user);
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
        public async Task<ProfileViewModel?> GetProfileWithDetailsAsync(string userId, string? viewerId = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                var profile = _mapper.Map<ProfileViewModel>(user);

                // Get ratings for this user
                var ratings = await _unitOfWork.RatingRepo.GetAllRatingByUserId(userId);
                if (ratings != null && ratings.Any())
                {
                    profile.TotalRatingsReceived = ratings.Count;
                    profile.AverageRating = (decimal)ratings.Average(r => r.Score);
                    
                    // Map ratings to AllRatingViewModel
                    profile.Ratings = ratings.Select(r => new LaborBLL.ModelVM.AllRatingViewModel
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
        public async Task<Response<bool>> UpdateProfileAsync(ProfileViewModel model)
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
        public async Task<ProfileViewModel?> GetByEmailAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                return _mapper.Map<ProfileViewModel>(user);
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
    }
}
