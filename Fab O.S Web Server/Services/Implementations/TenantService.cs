using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FabOS.WebServer.Services.Implementations
{
    public interface ITenantService
    {
        int GetCurrentCompanyId();
        int? GetCurrentUserId();
        string GetCurrentUserName();
        bool IsAuthenticated();
    }

    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantService> _logger;

        public TenantService(IHttpContextAccessor httpContextAccessor, ILogger<TenantService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public int GetCurrentCompanyId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Attempting to get CompanyId for unauthenticated user");
                return 1; // Default company for testing - should throw in production
            }

            var companyIdClaim = httpContext.User.FindFirst("CompanyId")
                ?? httpContext.User.FindFirst(ClaimTypes.GroupSid)
                ?? httpContext.User.FindFirst("company_id");

            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            {
                return companyId;
            }

            // Try to get from session if available
            if (httpContext.Session != null)
            {
                var sessionCompanyId = httpContext.Session.GetInt32("CompanyId");
                if (sessionCompanyId.HasValue)
                {
                    return sessionCompanyId.Value;
                }
            }

            _logger.LogWarning($"CompanyId not found for user {httpContext.User.Identity.Name}");
            return 1; // Default company - in production this should throw an exception
        }

        public int? GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirst("UserId")
                ?? httpContext.User.FindFirst("user_id")
                ?? httpContext.User.FindFirst("sub");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }

        public string GetCurrentUserName()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return "System";
            }

            return httpContext.User.Identity.Name ?? "Unknown";
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}