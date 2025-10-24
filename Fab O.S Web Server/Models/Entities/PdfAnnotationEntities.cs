using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// PDF Scale Calibration Data - Stores calibration for each drawing
/// </summary>
[Table("PdfScaleCalibrations")]
public class PdfScaleCalibration
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Reference to the PackageDrawing this calibration belongs to
    /// </summary>
    [Required]
    public int PackageDrawingId { get; set; }

    /// <summary>
    /// Scale ratio (e.g., 50 for 1:50)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Scale { get; set; }

    /// <summary>
    /// Unit of measurement (mm, m, ft, in)
    /// </summary>
    [Required]
    [StringLength(10)]
    public string Unit { get; set; } = "mm";

    /// <summary>
    /// Known distance in real-world units (e.g., 1000mm)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? KnownDistance { get; set; }

    /// <summary>
    /// Measured distance in PDF units (pixels)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? MeasuredDistance { get; set; }

    /// <summary>
    /// Page number where calibration was performed (0-indexed)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Calibration line start coordinates (JSON format: {x, y})
    /// </summary>
    [StringLength(200)]
    public string? CalibrationLineStart { get; set; }

    /// <summary>
    /// Calibration line end coordinates (JSON format: {x, y})
    /// </summary>
    [StringLength(200)]
    public string? CalibrationLineEnd { get; set; }

    /// <summary>
    /// User who created this calibration
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual PackageDrawing PackageDrawing { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
    public virtual Company Company { get; set; } = null!;
}

/// <summary>
/// PDF Annotations - Stores Nutrient/PSPDFKit annotations in Instant JSON format
/// </summary>
[Table("PdfAnnotations")]
public class PdfAnnotation
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Reference to the PackageDrawing this annotation belongs to
    /// </summary>
    [Required]
    public int PackageDrawingId { get; set; }

    /// <summary>
    /// Nutrient/PSPDFKit annotation ID (UUID)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string AnnotationId { get; set; } = string.Empty;

    /// <summary>
    /// Annotation type (DistanceMeasurementAnnotation, AreaMeasurementAnnotation, etc.)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string AnnotationType { get; set; } = string.Empty;

    /// <summary>
    /// Page number where annotation exists (0-indexed)
    /// </summary>
    [Required]
    public int PageIndex { get; set; }

    /// <summary>
    /// PSPDFKit Instant JSON format for this annotation
    /// Stores complete annotation data for restoration
    /// </summary>
    [Required]
    public string InstantJson { get; set; } = string.Empty;

    /// <summary>
    /// Is this a measurement annotation?
    /// </summary>
    public bool IsMeasurement { get; set; } = false;

    /// <summary>
    /// Is this a calibration annotation?
    /// </summary>
    public bool IsCalibration { get; set; } = false;

    /// <summary>
    /// Reference to associated measurement in TraceTakeoffMeasurements (if applicable)
    /// </summary>
    public int? TraceTakeoffMeasurementId { get; set; }

    /// <summary>
    /// Measurement value with unit (e.g., "3.48 in", "8.51 sq in")
    /// Extracted from annotation.note field for easy querying
    /// </summary>
    [StringLength(100)]
    public string? MeasurementValue { get; set; }

    /// <summary>
    /// Measurement scale configuration (JSON format)
    /// Contains: { from, to, unitFrom, unitTo }
    /// Example: { "from": 1, "to": 1.00019, "unitFrom": "in", "unitTo": "in" }
    /// </summary>
    [StringLength(500)]
    public string? MeasurementScale { get; set; }

    /// <summary>
    /// Measurement precision (e.g., "0.01", "0.001")
    /// </summary>
    [StringLength(20)]
    public string? MeasurementPrecision { get; set; }

    /// <summary>
    /// Coordinates data (JSON format)
    /// Stores: startPoint, endPoint, bbox, points arrays
    /// Allows querying and spatial analysis of measurements
    /// </summary>
    public string? CoordinatesData { get; set; }

    /// <summary>
    /// User who created this annotation
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual PackageDrawing PackageDrawing { get; set; } = null!;
    public virtual TraceTakeoffMeasurement? TraceTakeoffMeasurement { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual Company Company { get; set; } = null!;
}
