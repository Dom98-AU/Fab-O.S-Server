using FabOS.WebServer.Models.Calibration;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing scale calibration in PDF takeoff measurements
/// </summary>
public interface IScaleCalibrationService
{
    /// <summary>
    /// Get the active calibration for a drawing
    /// </summary>
    Task<CalibrationData?> GetActiveCalibrationAsync(int packageDrawingId);

    /// <summary>
    /// Get all calibrations for a drawing
    /// </summary>
    Task<List<CalibrationData>> GetCalibrationHistoryAsync(int packageDrawingId);

    /// <summary>
    /// Create a new calibration from user input
    /// </summary>
    Task<CalibrationResult> CreateCalibrationAsync(CreateCalibrationRequest request, int userId);

    /// <summary>
    /// Update an existing calibration
    /// </summary>
    Task<CalibrationResult> UpdateCalibrationAsync(int calibrationId, CreateCalibrationRequest request, int userId);

    /// <summary>
    /// Delete a calibration
    /// </summary>
    Task<bool> DeleteCalibrationAsync(int calibrationId, int userId);

    /// <summary>
    /// Set a calibration as active for a drawing
    /// </summary>
    Task<bool> SetActiveCalibrationAsync(int packageDrawingId, int calibrationId, int userId);

    /// <summary>
    /// Calculate scale factor from known distance and measured pixels
    /// </summary>
    Task<CalibrationResult> CalculateScaleFactorAsync(double knownDistance, double measuredPixels, string units = "mm");

    /// <summary>
    /// Validate a calibration for reasonableness
    /// </summary>
    Task<CalibrationResult> ValidateCalibrationAsync(CalibrationData calibration);

    /// <summary>
    /// Get common scale presets for architectural drawings
    /// </summary>
    Task<List<ScalePreset>> GetScalePresetsAsync();

    /// <summary>
    /// Apply a preset scale to a drawing
    /// </summary>
    Task<CalibrationResult> ApplyPresetScaleAsync(int packageDrawingId, decimal scaleRatio, int userId);

    /// <summary>
    /// Convert pixel measurements to real-world units using calibration
    /// </summary>
    Task<double> ConvertPixelsToRealWorldAsync(int packageDrawingId, double pixelDistance, string targetUnits = "mm");

    /// <summary>
    /// Convert real-world measurements to pixels using calibration
    /// </summary>
    Task<double> ConvertRealWorldToPixelsAsync(int packageDrawingId, double realWorldDistance, string sourceUnits = "mm");

    /// <summary>
    /// Get calibration session data for UI state management
    /// </summary>
    Task<CalibrationSession> GetCalibrationSessionAsync(int packageDrawingId);

    /// <summary>
    /// Recalibrate all measurements for a drawing when scale changes
    /// </summary>
    Task<bool> RecalibrateAllMeasurementsAsync(int packageDrawingId, int newCalibrationId);

    /// <summary>
    /// Get calibration accuracy score based on measurement consistency
    /// </summary>
    Task<double> GetCalibrationAccuracyAsync(int packageDrawingId);

    /// <summary>
    /// Create a quick calibration using a known scale ratio (e.g., 1:50)
    /// </summary>
    Task<CalibrationResult> CreateQuickCalibrationAsync(int packageDrawingId, decimal scaleRatio, int userId);

    /// <summary>
    /// Export calibration data for backup or transfer
    /// </summary>
    Task<string> ExportCalibrationDataAsync(int packageDrawingId);

    /// <summary>
    /// Import calibration data from backup
    /// </summary>
    Task<CalibrationResult> ImportCalibrationDataAsync(int packageDrawingId, string calibrationJson, int userId);
}