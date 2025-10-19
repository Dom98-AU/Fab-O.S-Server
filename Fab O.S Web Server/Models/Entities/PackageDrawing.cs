using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FabOS.WebServer.Models.Calibration;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Represents a drawing/document associated with a package and stored in cloud storage
/// Supports multiple cloud storage providers (SharePoint, Google Drive, Dropbox, Azure Blob)
/// </summary>
[Table("PackageDrawings")]
public class PackageDrawing
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PackageId { get; set; }

    [Required]
    [StringLength(100)]
    public string DrawingNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DrawingTitle { get; set; }

    // LEGACY: SharePoint-specific fields (kept for backward compatibility)
    [Required]
    [StringLength(500)]
    public string SharePointItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string SharePointUrl { get; set; } = string.Empty;

    // NEW: Multi-provider cloud storage fields
    /// <summary>
    /// Cloud storage provider name: "SharePoint", "GoogleDrive", "Dropbox", "AzureBlob"
    /// If null, defaults to "SharePoint" for backward compatibility
    /// </summary>
    [StringLength(50)]
    public string? StorageProvider { get; set; }

    /// <summary>
    /// Provider-specific file identifier (e.g., SharePoint ItemId, Google Drive fileId, Dropbox path)
    /// For new records, this replaces SharePointItemId in a provider-agnostic way
    /// </summary>
    [StringLength(500)]
    public string? ProviderFileId { get; set; }

    /// <summary>
    /// JSON metadata specific to the storage provider (e.g., ETag, version info, permissions)
    /// Allows storing provider-specific information without schema changes
    /// </summary>
    public string? ProviderMetadata { get; set; }

    [StringLength(50)]
    public string FileType { get; set; } = "PDF";

    public long FileSize { get; set; }

    [Required]
    public DateTime UploadedDate { get; set; }

    [Required]
    public int UploadedBy { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Nutrient/PSPDFKit Instant JSON containing all annotations, measurements, and calibration scale
    /// This preserves the complete visual state of the PDF viewer
    /// </summary>
    public string? InstantJson { get; set; }

    public DateTime? InstantJsonLastUpdated { get; set; }

    /// <summary>
    /// Calibration configuration (measurement presets) stored separately for trial license compatibility
    /// Contains measurementValueConfiguration for distance, perimeter, and area measurement tools
    /// </summary>
    public string? CalibrationConfig { get; set; }

    public DateTime? CalibrationConfigLastUpdated { get; set; }

    // Navigation properties
    [ForeignKey("PackageId")]
    public virtual Package Package { get; set; } = null!;

    [ForeignKey("UploadedBy")]
    public virtual User UploadedByUser { get; set; } = null!;

    // Link to takeoff measurements
    public virtual ICollection<TraceTakeoffMeasurement> TakeoffMeasurements { get; set; } = new List<TraceTakeoffMeasurement>();

    // Link to calibration data
    public virtual ICollection<CalibrationData> Calibrations { get; set; } = new List<CalibrationData>();
}