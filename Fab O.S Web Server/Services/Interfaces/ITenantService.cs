namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing tenant context and company-based routing
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets the current company ID from the authenticated user's claims
    /// </summary>
    int GetCurrentCompanyId();

    /// <summary>
    /// Gets the current user ID from the authenticated user's claims
    /// </summary>
    int? GetCurrentUserId();

    /// <summary>
    /// Gets the current user name from the authenticated user's claims
    /// </summary>
    string GetCurrentUserName();

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated();

    /// <summary>
    /// Gets the current tenant slug from the authenticated user's company
    /// </summary>
    Task<string?> GetCurrentTenantSlugAsync();

    /// <summary>
    /// Gets the tenant slug for a specific company
    /// </summary>
    Task<string?> GetTenantSlugByCompanyIdAsync(int companyId);
}
