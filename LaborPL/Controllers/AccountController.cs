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
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, IStripeService stripeService, IUnitOfWork unitOfWork, ILogger<AccountController> logger)
        {
            _userService = userService;
            _stripeService = stripeService;
            _unitOfWork = unitOfWork;
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

                return RedirectToAction("Index", "Home");
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

        #region Profile

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response= await _userService.UpdateProfileAsync(model);

            if (response.Success)
            {
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update profile.");
            return View(model);
        }

        /// <summary>
        /// View another user's profile (read-only) - used for viewing applicant/worker profiles
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewProfile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User ID is required.");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // If user is viewing their own profile, redirect to the editable profile page
            if (id == currentUserId)
            {
                return RedirectToAction(nameof(Profile));
            }

            var profile = await _userService.GetProfileAsync(id);
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
