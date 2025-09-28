using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace FabOS.WebServer.Middleware;

/// <summary>
/// Hybrid authentication middleware that supports both Cookie and JWT authentication
/// </summary>
public class HybridAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HybridAuthenticationMiddleware> _logger;

    public HybridAuthenticationMiddleware(RequestDelegate next, ILogger<HybridAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Determine authentication scheme based on request characteristics
        var authScheme = DetermineAuthenticationScheme(context);
        
        if (!string.IsNullOrEmpty(authScheme))
        {
            context.Items["AuthScheme"] = authScheme;
            _logger.LogDebug("Using authentication scheme: {Scheme} for request: {Path}", 
                authScheme, context.Request.Path);
        }

        await _next(context);
    }

    private string DetermineAuthenticationScheme(HttpContext context)
    {
        // Check for JWT Bearer token in Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // Check for mobile app user agent
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent) && IsMobileUserAgent(userAgent))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // Check for API endpoints
        if (context.Request.Path.StartsWithSegments("/api") || 
            context.Request.Path.StartsWithSegments("/mobile"))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        // Default to cookie authentication for web browsers
        return CookieAuthenticationDefaults.AuthenticationScheme;
    }

    private bool IsMobileUserAgent(string userAgent)
    {
        var mobileIndicators = new[] 
        { 
            "FabOS-Mobile", "FabOS-Android", "FabOS-iOS", 
            "okhttp", "Xamarin", "Flutter", "ReactNative" 
        };
        
        return mobileIndicators.Any(indicator => 
            userAgent.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Custom authentication scheme selector for hybrid authentication
/// </summary>
public class HybridAuthenticationSchemeSelector : IAuthenticationSchemeProvider
{
    private readonly IAuthenticationSchemeProvider _inner;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HybridAuthenticationSchemeSelector(
        IAuthenticationSchemeProvider inner,
        IHttpContextAccessor httpContextAccessor)
    {
        _inner = inner;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.ContainsKey("AuthScheme") == true)
        {
            var schemeName = context.Items["AuthScheme"] as string;
            return GetSchemeAsync(schemeName);
        }
        
        return _inner.GetDefaultAuthenticateSchemeAsync();
    }

    public Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items.ContainsKey("AuthScheme") == true)
        {
            var schemeName = context.Items["AuthScheme"] as string;
            return GetSchemeAsync(schemeName);
        }
        
        return _inner.GetDefaultChallengeSchemeAsync();
    }

    public Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync()
        => _inner.GetDefaultForbidSchemeAsync();

    public Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync()
        => _inner.GetDefaultSignInSchemeAsync();

    public Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync()
        => _inner.GetDefaultSignOutSchemeAsync();

    public Task<AuthenticationScheme?> GetSchemeAsync(string name)
        => _inner.GetSchemeAsync(name);

    public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
        => _inner.GetRequestHandlerSchemesAsync();

    public void AddScheme(AuthenticationScheme scheme)
        => _inner.AddScheme(scheme);

    public void RemoveScheme(string name)
        => _inner.RemoveScheme(name);

    public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
        => _inner.GetAllSchemesAsync();
}
