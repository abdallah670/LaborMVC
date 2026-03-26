using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Entities;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AutoMapper;

namespace LaborPL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IStripeService _stripeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVerificationService _verificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public AccountController(
            IUserService userService,
            IStripeService stripeService,
            IUnitOfWork unitOfWork,
            IVerificationService verificationService,
            IMapper mapper,
            ILogger<AccountController> logger,
            UserManager<AppUser> userManager,
            IEmailService emailService)
        {
            _userService = userService;
            _stripeService = stripeService;
            _unitOfWork = unitOfWork;
            _verificationService = verificationService;
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
            _emailService = emailService;
        }

        #region Register

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for registration");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                }
                return View(model);
            }

            _logger.LogInformation("Attempting to register user: {Email}", model.Email);
            var response = await _userService.RegisterAsync(model);

            if (response.Success)
            {
                var userId = response.Result!;
                _logger.LogInformation("User registered successfully: {Email}, UserId: {UserId}", model.Email, userId);

                // Send email verification code
                var sendCodeResult = await _verificationService.SendEmailVerificationCodeAsync(userId, model.Email);

                if (sendCodeResult.Success)
                {
                    _logger.LogInformation("Email verification code sent to: {Email}", model.Email);

                    // Store return URL in TempData for after verification
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        TempData["ReturnUrl"] = returnUrl;
                    }

                    // Redirect to email verification page
                    return RedirectToAction("VerifyEmailCode", new { userId = userId, email = model.Email });
                }
                else
                {
                    _logger.LogError("Failed to send verification code to {Email}: {Error}", model.Email, sendCodeResult.ErrorMessage);
                    // Still show success but warn about email
                    TempData["WarningMessage"] = "Account created but we couldn't send the verification email. Please try resending from your profile.";
                    return RedirectToAction("Login");
                }
            }

            _logger.LogError("Registration failed for {Email}: {Error}", model.Email, response.ErrorMessage);
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Registration failed.");
            return View(model);
        }

        #endregion

        #region Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // First check if user exists and get their details
            var user = await _unitOfWork.AppUsers.GetByEmailAsync(model.Email);
            if (user != null && !user.EmailConfirmed)
            {
                _logger.LogWarning("Login attempt for unverified email: {Email}", model.Email);

                // Store return URL for after verification
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    TempData["ReturnUrl"] = returnUrl;
                }

                // Check if we need to resend the code (if expired or never sent)
                if (user.EmailVerificationCodeExpiry == null || user.EmailVerificationCodeExpiry < DateTime.UtcNow)
                {
                    // Resend verification code
                    var resendResult = await _verificationService.ResendEmailVerificationCodeAsync(user.Id);
                    if (resendResult.Success)
                    {
                        TempData["InfoMessage"] = "A new verification code has been sent to your email.";
                    }
                }

                // Redirect to verification page
                return RedirectToAction("VerifyEmailCode", new { userId = user.Id, email = user.Email });
            }

            var response = await _userService.LoginAsync(model);

            if (response.Success)
            {
                _logger.LogInformation("User logged in: {Email}", model.Email);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    await SignInUserAsync(response.Result!, model.RememberMe);
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Invalid login attempt.");
            return View(model);
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            await _userService.LogoutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region MyProfile (Current User - Editable)

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // Get profile with all details including ratings and stats
            var profile = await _userService.GetProfileWithDetailsAsync(userId);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MyProfile(UserProfileUpdateModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (model.Id != userId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                // Re-fetch the display properties (stats, ratings) that aren't in the update model
                var profile = await _userService.GetProfileWithDetailsAsync(userId);
                if (profile != null)
                {
                    // Map the submitted values back into the display model so the user doesn't lose their input
                    _mapper.Map(model, profile);
                    return View(profile);
                }
                return View("Error");
            }

            var response = await _userService.UpdateProfileAsync(model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(MyProfile));
            }

            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update profile.");
            
            // Same pattern for service-level errors
            var errorProfile = await _userService.GetProfileWithDetailsAsync(userId);
            if (errorProfile != null)
            {
                _mapper.Map(model, errorProfile);
                return View(errorProfile);
            }
            
            return View("Error");
        }

        #endregion

        #region Upload Profile Picture

        /// <summary>
        /// Upload profile picture view
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var profile = await _userService.GetProfileWithDetailsAsync(userId);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        /// <summary>
        /// Handle profile picture upload
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["Error"] = "Please select an image file.";
                return RedirectToAction(nameof(UploadProfilePicture));
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(profilePicture.ContentType))
            {
                TempData["Error"] = "Invalid file type. Please upload JPEG, PNG, or GIF.";
                return RedirectToAction(nameof(UploadProfilePicture));
            }

            // Validate file size (5MB)
            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size exceeds 5MB. Please choose a smaller image.";
                return RedirectToAction(nameof(UploadProfilePicture));
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                // Update user profile picture URL
                var user = await _unitOfWork.AppUsers.GetByIdAsync(userId);
                if (user != null)
                {
                    // Delete old picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        var oldFileName = Path.GetFileName(user.ProfilePictureUrl);
                        var oldFilePath = Path.Combine(uploadsFolder, oldFileName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Update with new URL
                    user.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
                    await _unitOfWork.AppUsers.UpdateAsync(user);
                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation("Profile picture uploaded for user {UserId}", userId);
                    TempData["SuccessMessage"] = "Profile picture uploaded successfully!";
                }

                return RedirectToAction(nameof(MyProfile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture for user {UserId}", userId);
                TempData["Error"] = "Failed to upload image. Please try again.";
                return RedirectToAction(nameof(UploadProfilePicture));
            }
        }

        #endregion

        #region Email Verification

        /// <summary>
        /// Display email verification code entry page (after registration)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailCode(string userId, string email)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Invalid verification request.";
                return RedirectToAction("Login");
            }

            var user = await _unitOfWork.AppUsers.GetByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Check if already verified
            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] = "Your email is already verified. Please log in.";
                return RedirectToAction("Login");
            }

            // Calculate cooldown remaining
            var cooldownSeconds = 0;
            if (user.LastEmailVerificationAttempt.HasValue)
            {
                var timeSinceLastAttempt = DateTime.UtcNow - user.LastEmailVerificationAttempt.Value;
                if (timeSinceLastAttempt < TimeSpan.FromMinutes(1))
                {
                    cooldownSeconds = (int)(TimeSpan.FromMinutes(1) - timeSinceLastAttempt).TotalSeconds;
                }
            }

            var viewModel = new VerifyEmailCodeViewModel
            {
                UserId = userId,
                Email = email,
                ResendCooldownSeconds = cooldownSeconds
            };

            return View(viewModel);
        }

        /// <summary>
        /// Verify email with 6-digit code
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmailCode(VerifyEmailCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _verificationService.VerifyEmailCodeAsync(model.UserId, model.Code);

            if (result.Success)
            {
                _logger.LogInformation("Email verified successfully for user {UserId}", model.UserId);

                // Get user profile for sign in
                var profile = await _userService.GetProfileAsync(model.UserId);
                if (profile != null)
                {
                    await SignInUserAsync(profile, model.RememberMe);

                    TempData["SuccessMessage"] = "Email verified successfully! Welcome to Labor Marketplace.";

                    // Check for stored return URL
                    if (TempData["ReturnUrl"] is string returnUrl && !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Dashboard");
                }

                TempData["SuccessMessage"] = "Email verified successfully! Please log in.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid verification code.");
            
            // Recalculate cooldown for the view
            var user = await _unitOfWork.AppUsers.GetByIdAsync(model.UserId);
            if (user?.LastEmailVerificationAttempt != null)
            {
                var timeSinceLastAttempt = DateTime.UtcNow - user.LastEmailVerificationAttempt.Value;
                model.ResendCooldownSeconds = timeSinceLastAttempt < TimeSpan.FromMinutes(1) 
                    ? (int)(TimeSpan.FromMinutes(1) - timeSinceLastAttempt).TotalSeconds 
                    : 0;
            }

            return View(model);
        }

        /// <summary>
        /// Resend email verification code
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailVerificationCode(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User ID is required." });
            }

            var user = await _unitOfWork.AppUsers.GetByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                return Json(new { success = false, message = "Email is already verified." });
            }

            var result = await _verificationService.ResendEmailVerificationCodeAsync(userId);

            if (result.Success)
            {
                _logger.LogInformation("Email verification code resent to user {UserId}", userId);
                return Json(new { success = true, message = "Verification code sent! Please check your email." });
            }

            return Json(new { success = false, message = result.ErrorMessage ?? "Failed to resend code." });
        }

        /// <summary>
        /// Confirm email address with token (link-based verification)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid verification link.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _verificationService.ConfirmEmailAsync(userId, token);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Thank you! Your email has been confirmed successfully.";
                return RedirectToAction("Login");
            }
            else
            {
                TempData["Error"] = result.ErrorMessage ?? "Failed to confirm email. The link may have expired.";
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// Resend email verification link (for authenticated users)
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var result = await _verificationService.ResendEmailVerificationAsync(userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Verification email sent! Please check your inbox.";
            }
            else
            {
                TempData["Error"] = result.ErrorMessage ?? "Failed to send verification email.";
            }

            return RedirectToAction("MyProfile");
        }

        #endregion

        #region Phone Verification

        /// <summary>
        /// Phone verification page
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult VerifyPhone()
        {
            return View();
        }

        /// <summary>
        /// Send phone verification SMS code
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPhoneVerification([FromBody] SendPhoneVerificationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var result = await _verificationService.SendPhoneVerificationAsync(
                userId,
                request.PhoneNumber,
                request.CountryCode);

            return Json(new { success = result.Success, message = result.ErrorMessage });
        }

        /// <summary>
        /// Verify phone number with SMS code
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var result = await _verificationService.VerifyPhoneAsync(userId, request.Code);

            return Json(new { success = result.Success, message = result.ErrorMessage });
        }

        #endregion

        #region ID Verification (KYC)

        /// <summary>
        /// ID Verification page
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult IdVerification()
        {
            return View();
        }

        /// <summary>
        /// Submit ID verification documents
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitIdVerification([FromBody] IdVerificationRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var result = await _verificationService.SubmitIdVerificationAsync(userId, request);

            return Json(new { success = result.Success, message = result.ErrorMessage, id = result.Result });
        }

        /// <summary>
        /// Get ID verification status
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> IdVerificationStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var status = await _verificationService.GetIdVerificationStatusAsync(userId);
            return Json(new { success = true, status });
        }

        /// <summary>
        /// Check if user has pending ID verification
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> HasPendingIdVerification()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            var hasPending = await _verificationService.HasPendingIdVerificationAsync(userId);
            return Json(new { success = true, hasPending });
        }

        #endregion

        #region Profile (Other Users - Read Only)

        /// <summary>
        /// View another user's profile (read-only) - used for viewing applicant/worker profiles
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile(string id)
        {
            // If no ID provided, redirect to current user's profile
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction(nameof(MyProfile));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If user is viewing their own profile, redirect to the editable profile page
            if (id == currentUserId)
            {
                return RedirectToAction(nameof(MyProfile));
            }

            var profile = await _userService.GetProfileWithDetailsAsync(id);
            if (profile == null)
            {
                return NotFound("User not found.");
            }

            return View(profile);
        }

        #endregion

        #region Stripe Connect Onboarding

        /// <summary>
        /// Starts Stripe Connect onboarding for workers to receive payments
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> ConnectStripe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _unitOfWork.AppUsers.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // If user already has a connected account, check if it's enabled
            if (!string.IsNullOrEmpty(user.StripeAccountId))
            {
                var isEnabled = await _stripeService.IsAccountEnabledAsync(user.StripeAccountId);
                if (isEnabled)
                {
                    TempData["SuccessMessage"] = "Your Stripe account is already connected and enabled.";
                    return RedirectToAction(nameof(Profile));
                }

                // Account exists but not enabled, create new onboarding link
                var refreshUrl = Url.Action("ConnectStripe", "Account", null, Request.Scheme);
                var returnUrl = Url.Action("StripeConnectReturn", "Account", null, Request.Scheme);
                var accountLinkUrl = await _stripeService.CreateAccountLinkAsync(user.StripeAccountId, refreshUrl!, returnUrl!);
                return Redirect(accountLinkUrl);
            }

            // Create new Stripe Connect account
            try
            {
                var accountId = await _stripeService.CreateConnectAccountAsync(
                    user.Email!,
                    user.FirstName ?? "",
                    user.LastName ?? ""
                );

                // Save the account ID to user
                user.StripeAccountId = accountId;
                await _unitOfWork.AppUsers.UpdateAsync(user);
                await _unitOfWork.SaveAsync();

                // Create onboarding link
                var refreshUrl = Url.Action("ConnectStripe", "Account", null, Request.Scheme);
                var returnUrl = Url.Action("StripeConnectReturn", "Account", null, Request.Scheme);
                var accountLinkUrl = await _stripeService.CreateAccountLinkAsync(accountId, refreshUrl!, returnUrl!);

                return Redirect(accountLinkUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Connect account for user {UserId}", userId);
                TempData["ErrorMessage"] = "Failed to create Stripe Connect account. Please try again.";
                return RedirectToAction(nameof(Profile));
            }
        }

        /// <summary>
        /// Return URL after Stripe Connect onboarding
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> StripeConnectReturn()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _unitOfWork.AppUsers.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.StripeAccountId))
            {
                TempData["ErrorMessage"] = "Stripe account connection failed.";
                return RedirectToAction(nameof(Profile));
            }

            // Check if account is enabled
            var isEnabled = await _stripeService.IsAccountEnabledAsync(user.StripeAccountId);
            if (isEnabled)
            {
                TempData["SuccessMessage"] = "Your Stripe account has been successfully connected! You can now receive payments.";
            }
            else
            {
                TempData["WarningMessage"] = "Your Stripe account is pending verification. You will be able to receive payments once verification is complete.";
            }

            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// Check Stripe Connect status
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> StripeStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { connected = false, enabled = false });
            }

            var user = await _unitOfWork.AppUsers.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.StripeAccountId))
            {
                return Json(new { connected = false, enabled = false });
            }

            var isEnabled = await _stripeService.IsAccountEnabledAsync(user.StripeAccountId);
            return Json(new { connected = true, enabled = isEnabled, accountId = user.StripeAccountId });
        }

        #endregion

        #region Forgot Password

        /// <summary>
        /// Display forgot password page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Handle forgot password form submission
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generate reset token
            var result = await _userService.ForgotPasswordAsync(model.Email);

            if (result.Success)
            {
                // Get user to generate the reset link
                var user = await _unitOfWork.AppUsers.GetByEmailAsync(model.Email);
                if (user != null)
                {
                    // Generate the actual token
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = System.Web.HttpUtility.UrlEncode(token);
                    
                    // Create reset link
                    var resetLink = Url.Action("ResetPassword", "Account", 
                        new { email = model.Email, token = encodedToken }, 
                        Request.Scheme);

                    // Send password reset email
                    var userName = $"{user.FirstName} {user.LastName}";
                    var emailSent = await _emailService.SendPasswordResetAsync(model.Email, userName, resetLink);
                    
                    if (emailSent)
                    {
                        _logger.LogInformation("Password reset email sent successfully to {Email}", model.Email);
                    }
                    else
                    {
                        _logger.LogError("Failed to send password reset email to {Email}", model.Email);
                        // Store the reset link in TempData for display if email fails
                        TempData["ResetLink"] = resetLink;
                    }
                }

                // Always show success message (don't reveal if email exists)
                TempData["SuccessMessage"] = "If an account exists with this email, you will receive password reset instructions.";
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred. Please try again.");
            return View(model);
        }

        /// <summary>
        /// Display forgot password confirmation page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        #endregion

        #region Reset Password

        /// <summary>
        /// Display reset password page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        /// <summary>
        /// Handle reset password form submission
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Decode the token
            var decodedToken = System.Web.HttpUtility.UrlDecode(model.Token);

            // Reset the password
            var result = await _userService.ResetPasswordAsync(model.Email, decodedToken, model.NewPassword);

            if (result.Success)
            {
                _logger.LogInformation("Password reset successful for {Email}", model.Email);
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to reset password. The link may have expired.");
            return View(model);
        }

        #endregion

        #region Access Denied

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Add Cookies and Claims for Authentication
        private async Task SignInUserAsync(UserProfileDisplayViewModel profile, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, profile.Id),
                new Claim(ClaimTypes.Name, $"{profile.FirstName} {profile.LastName}"),
                new Claim(ClaimTypes.Email, profile.Email),
                // Add role claims based on ClientRole flags
                new Claim("Role", profile.Role.ToString())
            };

            // Add individual role claims for authorization
            if (profile.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            if (profile.IsWorker)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Worker"));
            }
            if (profile.IsPoster)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Poster"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, "Login");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };
            await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity), authProperties);
        }

        #endregion
    }
}
