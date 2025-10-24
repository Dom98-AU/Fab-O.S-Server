namespace FabOS.WebServer.Models.Dto.Signup;

/// <summary>
/// Suggested action for resolving signup conflicts
/// </summary>
public class SuggestedAction
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
