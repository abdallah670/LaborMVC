using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using LaborDAL.Repo.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IStripeService _stripeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVerificationService _verificationService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IUserService userService,
            IStripeService stripeService,
            IUnitOfWork unitOfWork,
            IVerificationService verificationService,
            ILogger<AccountController> logger)
        {
            _userService = userService;
            _stripeService = stripeService;
            _unitOfWork = unitOfWork;
            _verificationService = verificationService;
            _logger = logger;
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
                _logger.LogInformation("User registered successfully: {Email}", model.Email);

                // Auto-login after registration
                var loginModel = new LoginViewModel
                {
                    Email = model.Email,
                    Password = model.Password,
                    RememberMe = false
                };

                await _userService.LoginAsync(loginModel);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    await SignInUserAsync(new ProfileViewModel { Email = model.Email, FirstName = model.FirstName, LastName = model.LastName }, false);

                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
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
        public async Task<IActionResult> MyProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _userService.UpdateProfileAsync(model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(MyProfile));
            }

            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update profile.");
            return View(model);
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
        /// Confirm email address with token
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
        /// Resend email verification link
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

        #region Access Denied

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Add Cookies and Claims for Authentication
        private async Task SignInUserAsync(ProfileViewModel profile, bool isPersistent)
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
