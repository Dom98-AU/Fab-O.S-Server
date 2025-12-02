using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Stores SharePoint configuration per tenant/company
/// Each company can configure their own SharePoint site
/// </summary>
[Table("CompanySharePointSettings")]
public class CompanySharePointSettings
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The company/tenant this SharePoint configuration belongs to
    /// </summary>
    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Azure AD Tenant ID for this company's SharePoint
    /// </summary>
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application (Client) ID
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Client Secret (should be encrypted)
    /// </summary>
    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// SharePoint site URL (e.g., https://contoso.sharepoint.com/sites/fabrication)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string SiteUrl { get; set; } = string.Empty;

    /// <summary>
    /// Document library name for Trace module (user must create in SharePoint first, case-sensitive)
    /// </summary>
    [StringLength(200)]
    public string? TraceDocumentLibrary { get; set; }

    /// <summary>
    /// Document library name for Estimate module (user must create in SharePoint first, case-sensitive)
    /// </summary>
    [StringLength(200)]
    public string? EstimateDocumentLibrary { get; set; }

    /// <summary>
    /// Document library name for FabMate module (user must create in SharePoint first, case-sensitive)
    /// </summary>
    [StringLength(200)]
    public string? FabMateDocumentLibrary { get; set; }

    /// <summary>
    /// Document library name for QDocs module (user must create in SharePoint first, case-sensitive)
    /// </summary>
    [StringLength(200)]
    public string? QDocsDocumentLibrary { get; set; }

    /// <summary>
    /// Document library name for Assets module (user must create in SharePoint first, case-sensitive)
    /// </summary>
    [StringLength(200)]
    public string? AssetsDocumentLibrary { get; set; }

    /// <summary>
    /// Root folder for Trace module within the document library (optional)
    /// </summary>
    [StringLength(200)]
    public string? TraceRootFolder { get; set; }

    /// <summary>
    /// Root folder for Estimate module within the document library (optional)
    /// </summary>
    [StringLength(200)]
    public string? EstimateRootFolder { get; set; }

    /// <summary>
    /// Root folder for FabMate module within the document library (optional)
    /// </summary>
    [StringLength(200)]
    public string? FabMateRootFolder { get; set; }

    /// <summary>
    /// Root folder for QDocs module within the document library (optional)
    /// </summary>
    [StringLength(200)]
    public string? QDocsRootFolder { get; set; }

    /// <summary>
    /// Root folder for Assets module within the document library (optional)
    /// </summary>
    [StringLength(200)]
    public string? AssetsRootFolder { get; set; }

    /// <summary>
    /// Whether SharePoint integration is enabled for this company
    /// </summary>
    [Required]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to use mock data instead of real SharePoint (for testing)
    /// </summary>
    [Required]
    public bool UseMockData { get; set; } = false;

    /// <summary>
    /// Maximum file size in MB allowed for uploads
    /// </summary>
    [Required]
    public int MaxFileSizeMB { get; set; } = 250;

    /// <summary>
    /// Indicates if the ClientSecret is encrypted
    /// </summary>
    [Required]
    public bool IsClientSecretEncrypted { get; set; } = false;

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    public int? LastModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedByUserId")]
    public virtual User? LastModifiedByUser { get; set; }

    /// <summary>
    /// Gets the document library name for a specific module
    /// Throws exception if library is not configured for the module
    /// </summary>
    public string GetDocumentLibraryForModule(string moduleName)
    {
        var library = moduleName?.ToLower() switch
        {
            "trace" => TraceDocumentLibrary,
            "estimate" => EstimateDocumentLibrary,
            "fabmate" => FabMateDocumentLibrary,
            "qdocs" => QDocsDocumentLibrary,
            "assets" => AssetsDocumentLibrary,
            _ => throw new ArgumentException($"Unknown module: {moduleName}", nameof(moduleName))
        };

        if (string.IsNullOrWhiteSpace(library))
        {
            throw new InvalidOperationException(
                $"SharePoint document library not configured for {moduleName} module. " +
                $"Please create the library in SharePoint first, then configure it in Settings.");
        }

        return library;
    }

    /// <summary>
    /// Gets the root folder for a specific module (empty string if not configured)
    /// </summary>
    public string GetRootFolderForModule(string moduleName)
    {
        return moduleName?.ToLower() switch
        {
            "trace" => TraceRootFolder ?? "",
            "estimate" => EstimateRootFolder ?? "",
            "fabmate" => FabMateRootFolder ?? "",
            "qdocs" => QDocsRootFolder ?? "",
            "assets" => AssetsRootFolder ?? "",
            _ => ""
        };
    }
}
