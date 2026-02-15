using LaborBLL.ModelVM;
using LaborBLL.Response;
using LaborBLL.Service.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LaborPL.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IUserService userService, ILogger<AccountController> logger)
        {
            _userService = userService;
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
                // Add more claims as needed, e.g., roles

            };
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
