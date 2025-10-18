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
    /// Document library name (default: "Takeoff Files")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string DocumentLibrary { get; set; } = "Takeoff Files";

    /// <summary>
    /// Root folder for takeoffs within the document library (default: "Takeoffs")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string TakeoffsRootFolder { get; set; } = "Takeoffs";

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
}
