using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FabOS.WebServer.Authentication;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// Web controller for cookie-based authentication (traditional login)
/// </summary>
[Route("Account")]
public class AccountController : Controller
{
    private readonly ICookieAuthenticationService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ICookieAuthenticationService authService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Display login page
    /// </summary>
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            // If user is already authenticated, redirect to a safe page or return URL
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/database");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new WebLoginModel());
    }

    /// <summary>
    /// Process login form submission
    /// </summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(WebLoginModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Validate credentials
            var isValid = await _authService.ValidateUserAsync(model.Email, model.Password);
            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            // Get user with company
            var user = await _authService.GetUserByEmailAsync(model.Email);
            if (user?.Company == null)
            {
                ModelState.AddModelError(string.Empty, "User account not found or inactive.");
                return View(model);
            }

            // Sign in with cookie
            var signInResult = await _authService.SignInAsync(user, user.Company, model.RememberMe);
            if (!signInResult)
            {
                ModelState.AddModelError(string.Empty, "An error occurred during sign in.");
                return View(model);
            }

            _logger.LogInformation("User {Email} logged in successfully via web", model.Email);

            return RedirectToLocal(returnUrl ?? "/database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during web login for {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
            return View(model);
        }
    }

    /// <summary>
    /// Process logout (GET request)
    /// </summary>
    [HttpGet("Logout")]
    public async Task<IActionResult> LogoutGet()
    {
        await _authService.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Process logout (POST request)
    /// </summary>
    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Access denied page
    /// </summary>
    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// User profile page
    /// </summary>
    [HttpGet("Profile")]
    [Authorize]
    public IActionResult Profile()
    {
        var userInfo = new
        {
            Id = int.Parse(User.FindFirst("user_id")?.Value ?? "0"),
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Name = User.FindFirst(ClaimTypes.Name)?.Value,
            Role = User.FindFirst(ClaimTypes.Role)?.Value,
            Company = new
            {
                Id = int.Parse(User.FindFirst("company_id")?.Value ?? "0"),
                Code = User.FindFirst("company_code")?.Value,
                Name = User.FindFirst("company_name")?.Value
            },
            Modules = User.FindAll("module").Select(c => c.Value).ToArray(),
            AuthMethod = User.FindFirst("auth_method")?.Value ?? "Cookie"
        };

        return View(userInfo);
    }

    /// <summary>
    /// Change password page
    /// </summary>
    [HttpGet("ChangePassword")]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    /// <summary>
    /// Process password change
    /// </summary>
    [HttpPost("ChangePassword")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            // Validate current password
            var isValid = await _authService.ValidateUserAsync(userEmail, model.CurrentPassword);
            if (!isValid)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
                return View(model);
            }

            // TODO: Implement password change logic
            // This would involve updating the user's password hash in the database

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {Email}", User.FindFirst(ClaimTypes.Email)?.Value);
            ModelState.AddModelError(string.Empty, "An error occurred while changing password.");
            return View(model);
        }
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        // Redirect to the database page since we don't have a HomeController
        return Redirect("/database");
    }
}

/// <summary>
/// Web login form model
/// </summary>
public class WebLoginModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

/// <summary>
/// Change password form model
/// </summary>
public class ChangePasswordModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
