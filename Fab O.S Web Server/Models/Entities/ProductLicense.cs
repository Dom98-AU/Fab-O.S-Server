using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Per-tenant product licensing for module availability and feature gating
/// Each tenant (company) has licenses for enabled modules with configurable features
/// </summary>
[Table("ProductLicenses")]
public class ProductLicense
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The company/tenant this license belongs to
    /// </summary>
    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Product/Module name (Trace, FabMate, QDocs, Estimate, Assets)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// License type (Standard, Professional, Enterprise)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LicenseType { get; set; } = string.Empty;

    /// <summary>
    /// Maximum concurrent users allowed for this module
    /// </summary>
    [Required]
    public int MaxConcurrentUsers { get; set; }

    /// <summary>
    /// License validity start date
    /// </summary>
    [Required]
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// License validity end date
    /// </summary>
    [Required]
    public DateTime ValidUntil { get; set; }

    /// <summary>
    /// Whether this license is currently active
    /// </summary>
    [Required]
    public bool IsActive { get; set; }

    /// <summary>
    /// JSON array of enabled features within this module
    /// Example: ["ITP","material-traceability","advanced-reporting"]
    /// </summary>
    [StringLength(2000)]
    public string? Features { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    public int? ModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }
}
