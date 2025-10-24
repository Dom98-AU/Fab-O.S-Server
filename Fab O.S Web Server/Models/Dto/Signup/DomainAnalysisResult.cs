namespace FabOS.WebServer.Models.Dto.Signup;

/// <summary>
/// Result of analyzing an email domain for existing workspaces
/// </summary>
public class DomainAnalysisResult
{
    public string Domain { get; set; } = string.Empty;
    public bool HasExistingTenants { get; set; }
    public List<ExistingTenantInfo> ExistingTenants { get; set; } = new();
    public int TenantCount { get; set; }
}
