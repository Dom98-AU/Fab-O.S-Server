using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FabOS.WebServer.Models.Calibration;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Represents a drawing/document associated with a package and stored in SharePoint
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

    [Required]
    [StringLength(500)]
    public string SharePointItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string SharePointUrl { get; set; } = string.Empty;

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