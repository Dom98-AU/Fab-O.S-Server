namespace FabOS.WebServer.Models.DTOs.Signup;

/// <summary>
/// Information about an existing tenant/workspace
/// </summary>
public class ExistingTenantInfo
{
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
