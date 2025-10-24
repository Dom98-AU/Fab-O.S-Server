namespace FabOS.WebServer.Models.Dto.Signup;

/// <summary>
/// Result of tenant/company creation
/// </summary>
public class TenantCreationResult
{
    public bool Success { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
