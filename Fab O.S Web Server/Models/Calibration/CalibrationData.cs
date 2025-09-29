using System.ComponentModel.DataAnnotations;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.Calibration;

/// <summary>
/// Represents scale calibration data for a PDF drawing
/// </summary>
public class CalibrationData
{
    public int Id { get; set; }

    /// <summary>
    /// The PackageDrawing this calibration applies to
    /// </summary>
    public int PackageDrawingId { get; set; }

    /// <summary>
    /// Scale factor (pixels per unit)
    /// </summary>
    public double PixelsPerUnit { get; set; }

    /// <summary>
    /// Calibration scale ratio (e.g., 50 for 1:50)
    /// </summary>
    public decimal ScaleRatio { get; set; }

    /// <summary>
    /// Known real-world distance used for calibration (in mm)
    /// </summary>
    public double KnownDistance { get; set; }

    /// <summary>
    /// Measured pixel distance for the known distance
    /// </summary>
    public double MeasuredPixels { get; set; }

    /// <summary>
    /// First calibration point (JSON serialized)
    /// </summary>
    public string Point1Json { get; set; } = string.Empty;

    /// <summary>
    /// Second calibration point (JSON serialized)
    /// </summary>
    public string Point2Json { get; set; } = string.Empty;

    /// <summary>
    /// Units for the known distance (mm, cm, m, etc.)
    /// </summary>
    [StringLength(10)]
    public string Units { get; set; } = "mm";

    /// <summary>
    /// When this calibration was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this calibration
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// Whether this calibration is currently active for the drawing
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional notes about this calibration
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property to the drawing
    /// </summary>
    public virtual PackageDrawing? PackageDrawing { get; set; }
}

/// <summary>
/// Common scale presets for architectural drawings
/// </summary>
public class ScalePreset
{
    public decimal ScaleRatio { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCommon { get; set; }
}

/// <summary>
/// Calibration point for reference measurements
/// </summary>
public class CalibrationPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Result of a calibration calculation
/// </summary>
public class CalibrationResult
{
    public bool IsValid { get; set; }
    public double PixelsPerUnit { get; set; }
    public decimal ScaleRatio { get; set; }
    public string? ErrorMessage { get; set; }
    public double AccuracyScore { get; set; } // 0-100% confidence
}

/// <summary>
/// Request for creating a new calibration
/// </summary>
public class CreateCalibrationRequest
{
    public int PackageDrawingId { get; set; }
    public double KnownDistance { get; set; }
    public string Units { get; set; } = "mm";
    public CalibrationPoint Point1 { get; set; } = new();
    public CalibrationPoint Point2 { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// Calibration settings for a user session
/// </summary>
public class CalibrationSession
{
    public int PackageDrawingId { get; set; }
    public CalibrationData? ActiveCalibration { get; set; }
    public bool IsCalibrated { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<ScalePreset> AvailablePresets { get; set; } = new();
}