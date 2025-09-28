using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FabOS.WebServer.Authentication;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for mobile and API authentication using JWT tokens
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICookieAuthenticationService _cookieAuthService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IJwtTokenService jwtTokenService,
        ICookieAuthenticationService cookieAuthService,
        ILogger<AuthController> logger)
    {
        _jwtTokenService = jwtTokenService;
        _cookieAuthService = cookieAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Mobile/API login endpoint
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid request data", errors = ModelState });
            }

            // Validate credentials
            var isValid = await _cookieAuthService.ValidateUserAsync(request.Email, request.Password);
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Get user with company
            var user = await _cookieAuthService.GetUserByEmailAsync(request.Email);
            if (user?.Company == null)
            {
                return Unauthorized(new { message = "User or company not found" });
            }

            // Generate JWT tokens
            var tokenResponse = await _jwtTokenService.GenerateTokenAsync(user, user.Company);

            _logger.LogInformation("JWT login successful for user {Email}", request.Email);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during JWT login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh JWT access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var tokenResponse = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken);
            return Ok(tokenResponse);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid refresh token used");
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { message = "An error occurred while refreshing token" });
        }
    }

    /// <summary>
    /// Logout and revoke tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
                {
                    // Revoke specific refresh token
                    await _jwtTokenService.RevokeTokenAsync(request.RefreshToken);
                }
                else
                {
                    // Revoke all user tokens
                    await _jwtTokenService.RevokeAllUserTokensAsync(userId);
                }
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        try
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
                AuthMethod = User.FindFirst("auth_method")?.Value ?? "JWT"
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user info");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    /// <summary>
    /// Validate JWT token (for API clients)
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { message = "Token is required" });
            }

            var principal = _jwtTokenService.ValidateToken(request.Token);
            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new { message = "Token is valid", valid = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { message = "An error occurred while validating token" });
        }
    }
}

// Request/Response models
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? DeviceInfo { get; set; }
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class ValidateTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
