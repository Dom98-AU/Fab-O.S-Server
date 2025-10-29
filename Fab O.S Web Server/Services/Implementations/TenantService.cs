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
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<TenantService> logger,
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _contextFactory = contextFactory;
            _environment = environment;
        }

        public int GetCurrentCompanyId()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // In production, require authentication - fail fast
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                if (_environment.IsProduction())
                {
                    _logger.LogError("[TenantService] Unauthenticated user attempted to access tenant resources");
                    throw new UnauthorizedAccessException("User must be authenticated to access tenant resources");
                }

                _logger.LogWarning("[TenantService] Unauthenticated user in development - using default CompanyId=1");
                return 1; // Development fallback only
            }

            // Try to get CompanyId from claims (primary method)
            var companyIdClaim = httpContext.User.FindFirst("CompanyId")
                ?? httpContext.User.FindFirst(ClaimTypes.GroupSid)
                ?? httpContext.User.FindFirst("company_id");

            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            {
                _logger.LogDebug("[TenantService] Found CompanyId={CompanyId} in user claims", companyId);
                return companyId;
            }

            // Fallback to session (secondary method)
            if (httpContext.Session != null)
            {
                var sessionCompanyId = httpContext.Session.GetInt32("CompanyId");
                if (sessionCompanyId.HasValue)
                {
                    _logger.LogDebug("[TenantService] Found CompanyId={CompanyId} in session", sessionCompanyId.Value);
                    return sessionCompanyId.Value;
                }
            }

            // No CompanyId found - fail in production, fallback in development
            var userName = httpContext.User.Identity?.Name ?? "Unknown";
            if (_environment.IsProduction())
            {
                _logger.LogError("[TenantService] User '{UserName}' is missing required CompanyId claim", userName);
                throw new InvalidOperationException(
                    $"User '{userName}' is missing required CompanyId claim. Please contact support.");
            }

            _logger.LogWarning("[TenantService] CompanyId not found for user '{UserName}' in development - using default CompanyId=1", userName);
            return 1; // Development fallback only
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
                var tenantSlug = await GetTenantSlugByCompanyIdAsync(companyId);

                // In production, tenant slug is required - don't return null
                if (string.IsNullOrEmpty(tenantSlug) && _environment.IsProduction())
                {
                    _logger.LogError("[TenantService] Failed to resolve tenant slug for CompanyId={CompanyId} in production", companyId);
                    throw new InvalidOperationException(
                        $"Unable to resolve tenant for company {companyId}. Database may be misconfigured.");
                }

                return tenantSlug;
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw auth exceptions without wrapping
                throw;
            }
            catch (InvalidOperationException)
            {
                // Re-throw validation exceptions without wrapping
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TenantService] Unexpected error getting current tenant slug");

                if (_environment.IsProduction())
                {
                    throw new InvalidOperationException("Unable to determine tenant context. Please try again or contact support.", ex);
                }

                _logger.LogWarning("[TenantService] Returning null tenant slug in development due to error");
                return null;
            }
        }

        public async Task<string?> GetTenantSlugByCompanyIdAsync(int companyId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var company = await context.Companies
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