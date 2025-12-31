using FabOS.WebServer.Models.DTOs.Signup;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for provisioning new tenants (companies) and admin users
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Creates a new tenant (company + admin user) and returns the result with tenant slug
    /// </summary>
    Task<TenantCreationResult> CreateTenantAsync(SignupRequest request);

    /// <summary>
    /// Generates a company code from a company name (lowercase, hyphens, no special chars)
    /// </summary>
    string GenerateCompanyCode(string companyName);
}
