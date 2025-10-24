using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantService> _logger;
        private readonly ApplicationDbContext _context;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<TenantService> logger,
            ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _context = context;
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

        public async Task<string?> GetCurrentTenantSlugAsync()
        {
            try
            {
                var companyId = GetCurrentCompanyId();
                return await GetTenantSlugByCompanyIdAsync(companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TenantService] Error getting current tenant slug");
                return null;
            }
        }

        public async Task<string?> GetTenantSlugByCompanyIdAsync(int companyId)
        {
            try
            {
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Id == companyId);

                if (company == null)
                {
                    _logger.LogWarning("[TenantService] Company {CompanyId} not found", companyId);
                    return null;
                }

                // Use Company.Code as the tenant slug (convert to lowercase, replace spaces with hyphens)
                var tenantSlug = company.Code.ToLowerInvariant().Replace(" ", "-");
                _logger.LogInformation("[TenantService] Company {CompanyId} has tenant slug '{TenantSlug}'", companyId, tenantSlug);
                return tenantSlug;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TenantService] Error getting tenant slug for company {CompanyId}", companyId);
                return null;
            }
        }
    }
}