namespace FabOS.WebServer.Models.DTOs.Signup;

/// <summary>
/// Result of signup validation with conflict detection
/// </summary>
public class SignupValidationResult
{
    public bool IsValid { get; set; }
    public ConflictType ConflictType { get; set; } = ConflictType.None;
    public string Message { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public ExistingTenantInfo? ExistingTenant { get; set; }
    public DomainAnalysisResult? DomainAnalysis { get; set; }
    public List<string> CodeSuggestions { get; set; } = new();
    public SuggestedAction[] SuggestedActions { get; set; } = Array.Empty<SuggestedAction>();
}
