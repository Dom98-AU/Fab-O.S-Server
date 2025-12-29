using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Saved label template configuration for printing equipment/kit/location labels
/// </summary>
[Table("LabelTemplates")]
public class LabelTemplate
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Company that owns this template (NULL for system templates)
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Unique template name within company
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Entity types this template supports: Equipment, Kit, Location, or All
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = "All";

    // ===== DIMENSIONS =====

    /// <summary>
    /// Label width in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal WidthMm { get; set; } = 50;

    /// <summary>
    /// Label height in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal HeightMm { get; set; } = 25;

    // ===== QR CODE SETTINGS =====

    /// <summary>
    /// Whether to include a QR code on the label
    /// </summary>
    public bool IncludeQRCode { get; set; } = true;

    /// <summary>
    /// QR code size: pixels per module (5=Small, 10=Medium, 15=Large, 20=ExtraLarge)
    /// </summary>
    public int QRCodePixelsPerModule { get; set; } = 10;

    // ===== FIELD VISIBILITY =====

    /// <summary>
    /// Include the entity code (e.g., EQ001, KIT-2025-0001, LOC-001)
    /// </summary>
    public bool IncludeCode { get; set; } = true;

    /// <summary>
    /// Include the entity name
    /// </summary>
    public bool IncludeName { get; set; } = true;

    /// <summary>
    /// Include category (for Equipment) or type (for Location)
    /// </summary>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    /// Include location information
    /// </summary>
    public bool IncludeLocation { get; set; } = true;

    /// <summary>
    /// Include serial number (Equipment only)
    /// </summary>
    public bool IncludeSerialNumber { get; set; } = false;

    /// <summary>
    /// Include next service/maintenance date (Equipment only)
    /// </summary>
    public bool IncludeServiceDate { get; set; } = false;

    /// <summary>
    /// Include contact information (Location only)
    /// </summary>
    public bool IncludeContactInfo { get; set; } = false;

    // ===== STYLING =====

    /// <summary>
    /// Primary font size for code/name (in points)
    /// </summary>
    public int PrimaryFontSize { get; set; } = 8;

    /// <summary>
    /// Secondary font size for other fields (in points)
    /// </summary>
    public int SecondaryFontSize { get; set; } = 6;

    /// <summary>
    /// Margin around label content in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal MarginMm { get; set; } = 2;

    // ===== TEMPLATE FLAGS =====

    /// <summary>
    /// True if this is a system-provided template (cannot be edited/deleted)
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// True if this is the default template for the company/entity type
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    // ===== AUDIT FIELDS =====

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    public DateTime? LastModified { get; set; }

    public int? LastModifiedByUserId { get; set; }

    // ===== NAVIGATION =====

    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }
}
