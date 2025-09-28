using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.Configuration;

/// <summary>
/// Configuration settings for SharePoint integration
/// </summary>
public class SharePointSettings
{
    /// <summary>
    /// Azure AD Tenant ID
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Client/Application ID
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Client Secret
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// SharePoint site URL (e.g., https://contoso.sharepoint.com/sites/fabos)
    /// </summary>
    [Required]
    public string SiteUrl { get; set; } = string.Empty;

    /// <summary>
    /// Document library name (default: "Documents")
    /// </summary>
    public string DocumentLibrary { get; set; } = "Documents";

    /// <summary>
    /// Root folder path for takeoffs (default: "Takeoffs")
    /// </summary>
    public string TakeoffsRootFolder { get; set; } = "Takeoffs";

    /// <summary>
    /// Whether SharePoint integration is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Maximum file size in MB (default: 250)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 250;

    /// <summary>
    /// Whether to use mock data for testing
    /// </summary>
    public bool UseMockData { get; set; } = false;

    /// <summary>
    /// Validates the settings
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(TenantId) &&
               !string.IsNullOrWhiteSpace(ClientId) &&
               !string.IsNullOrWhiteSpace(ClientSecret) &&
               !string.IsNullOrWhiteSpace(SiteUrl) &&
               !string.IsNullOrWhiteSpace(DocumentLibrary);
    }

    /// <summary>
    /// Gets the site ID from the site URL
    /// </summary>
    public string GetSiteId()
    {
        if (string.IsNullOrWhiteSpace(SiteUrl))
            return string.Empty;

        var uri = new Uri(SiteUrl);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // For URLs like https://contoso.sharepoint.com/sites/fabos
        // Return "contoso.sharepoint.com,site-guid,web-guid" format
        // This will be resolved by Graph API
        return $"{uri.Host}:/sites/{string.Join("/", segments.Skip(1))}";
    }
}