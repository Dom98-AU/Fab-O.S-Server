using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Calibration;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FabOS.WebServer.Services.Implementations;

public class ScaleCalibrationService : IScaleCalibrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScaleCalibrationService> _logger;

    public ScaleCalibrationService(
        ApplicationDbContext context,
        ILogger<ScaleCalibrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CalibrationData?> GetActiveCalibrationAsync(int packageDrawingId)
    {
        try
        {
            return await _context.Calibrations
                .FirstOrDefaultAsync(c => c.PackageDrawingId == packageDrawingId && c.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active calibration for drawing {DrawingId}", packageDrawingId);
            return null;
        }
    }

    public async Task<List<CalibrationData>> GetCalibrationHistoryAsync(int packageDrawingId)
    {
        try
        {
            return await _context.Calibrations
                .Where(c => c.PackageDrawingId == packageDrawingId)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calibration history for drawing {DrawingId}", packageDrawingId);
            return new List<CalibrationData>();
        }
    }

    public async Task<CalibrationResult> CreateCalibrationAsync(CreateCalibrationRequest request, int userId)
    {
        try
        {
            // Calculate scale factor
            var scaleResult = await CalculateScaleFactorAsync(request.KnownDistance,
                GetPixelDistance(request.Point1, request.Point2), request.Units);

            if (!scaleResult.IsValid)
            {
                return scaleResult;
            }

            // Deactivate existing calibrations
            var existingCalibrations = await _context.Calibrations
                .Where(c => c.PackageDrawingId == request.PackageDrawingId && c.IsActive)
                .ToListAsync();

            foreach (var existing in existingCalibrations)
            {
                existing.IsActive = false;
            }

            // Create new calibration
            var calibration = new CalibrationData
            {
                PackageDrawingId = request.PackageDrawingId,
                PixelsPerUnit = scaleResult.PixelsPerUnit,
                ScaleRatio = scaleResult.ScaleRatio,
                KnownDistance = request.KnownDistance,
                MeasuredPixels = GetPixelDistance(request.Point1, request.Point2),
                Point1Json = SerializePoint(request.Point1),
                Point2Json = SerializePoint(request.Point2),
                Units = request.Units,
                CreatedBy = userId,
                Notes = request.Notes,
                IsActive = true
            };

            _context.Calibrations.Add(calibration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created calibration for drawing {DrawingId} with scale 1:{Scale}",
                request.PackageDrawingId, scaleResult.ScaleRatio);

            return scaleResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calibration for drawing {DrawingId}", request.PackageDrawingId);
            return new CalibrationResult
            {
                IsValid = false,
                ErrorMessage = "Failed to create calibration: " + ex.Message
            };
        }
    }

    public async Task<CalibrationResult> UpdateCalibrationAsync(int calibrationId, CreateCalibrationRequest request, int userId)
    {
        try
        {
            var calibration = await _context.Calibrations.FindAsync(calibrationId);
            if (calibration == null)
            {
                return new CalibrationResult { IsValid = false, ErrorMessage = "Calibration not found" };
            }

            // Calculate new scale factor
            var scaleResult = await CalculateScaleFactorAsync(request.KnownDistance,
                GetPixelDistance(request.Point1, request.Point2), request.Units);

            if (!scaleResult.IsValid)
            {
                return scaleResult;
            }

            // Update calibration
            calibration.PixelsPerUnit = scaleResult.PixelsPerUnit;
            calibration.ScaleRatio = scaleResult.ScaleRatio;
            calibration.KnownDistance = request.KnownDistance;
            calibration.MeasuredPixels = GetPixelDistance(request.Point1, request.Point2);
            calibration.Point1Json = SerializePoint(request.Point1);
            calibration.Point2Json = SerializePoint(request.Point2);
            calibration.Units = request.Units;
            calibration.Notes = request.Notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated calibration {CalibrationId} for drawing {DrawingId}",
                calibrationId, request.PackageDrawingId);

            return scaleResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calibration {CalibrationId}", calibrationId);
            return new CalibrationResult
            {
                IsValid = false,
                ErrorMessage = "Failed to update calibration: " + ex.Message
            };
        }
    }

    public async Task<bool> DeleteCalibrationAsync(int calibrationId, int userId)
    {
        try
        {
            var calibration = await _context.Calibrations.FindAsync(calibrationId);
            if (calibration == null)
            {
                return false;
            }

            _context.Calibrations.Remove(calibration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted calibration {CalibrationId} by user {UserId}", calibrationId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calibration {CalibrationId}", calibrationId);
            return false;
        }
    }

    public async Task<bool> SetActiveCalibrationAsync(int packageDrawingId, int calibrationId, int userId)
    {
        try
        {
            // Deactivate all calibrations for the drawing
            var allCalibrations = await _context.Calibrations
                .Where(c => c.PackageDrawingId == packageDrawingId)
                .ToListAsync();

            foreach (var cal in allCalibrations)
            {
                cal.IsActive = (cal.Id == calibrationId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Set calibration {CalibrationId} as active for drawing {DrawingId}",
                calibrationId, packageDrawingId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active calibration {CalibrationId} for drawing {DrawingId}",
                calibrationId, packageDrawingId);
            return false;
        }
    }

    public Task<CalibrationResult> CalculateScaleFactorAsync(double knownDistance, double measuredPixels, string units = "mm")
    {

        try
        {
            if (measuredPixels <= 0)
            {
                return Task.FromResult(new CalibrationResult
                {
                    IsValid = false,
                    ErrorMessage = "Measured pixel distance must be greater than 0"
                });
            }

            if (knownDistance <= 0)
            {
                return Task.FromResult(new CalibrationResult
                {
                    IsValid = false,
                    ErrorMessage = "Known distance must be greater than 0"
                });
            }

            // Convert to millimeters for consistency
            var knownDistanceMm = ConvertToMillimeters(knownDistance, units);

            // Calculate pixels per millimeter
            var pixelsPerUnit = measuredPixels / knownDistanceMm;

            // Calculate scale ratio (1 pixel represents how many mm in real world)
            // For 1:50 scale, 1 pixel = 50mm, so scale ratio = 50
            var scaleRatio = (decimal)(knownDistanceMm / measuredPixels);

            // Validate reasonable ranges
            if (pixelsPerUnit < 0.001 || pixelsPerUnit > 100)
            {
                return Task.FromResult(new CalibrationResult
                {
                    IsValid = false,
                    ErrorMessage = "Calculated scale appears unreasonable. Please check your measurements."
                });
            }

            // Validate scale ratio ranges (architectural scales typically 1:1 to 1:10000)
            if (scaleRatio < 1 || scaleRatio > 10000)
            {
                return Task.FromResult(new CalibrationResult
                {
                    IsValid = false,
                    ErrorMessage = "Calculated scale ratio is outside reasonable range (1:1 to 1:10000)."
                });
            }

            // Calculate accuracy score based on common architectural scales
            var accuracyScore = CalculateAccuracyScore(scaleRatio);

            return Task.FromResult(new CalibrationResult
            {
                IsValid = true,
                PixelsPerUnit = pixelsPerUnit,
                ScaleRatio = scaleRatio,
                AccuracyScore = accuracyScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating scale factor");
            return Task.FromResult(new CalibrationResult
            {
                IsValid = false,
                ErrorMessage = "Failed to calculate scale: " + ex.Message
            });
        }
    }

    public async Task<CalibrationResult> ValidateCalibrationAsync(CalibrationData calibration)
    {
        await Task.CompletedTask; // For async interface

        try
        {
            var result = new CalibrationResult { IsValid = true };

            // Check reasonable scale ranges
            if (calibration.ScaleRatio < 1 || calibration.ScaleRatio > 10000)
            {
                result.IsValid = false;
                result.ErrorMessage = "Scale ratio is outside reasonable range (1:1 to 1:10000)";
                return result;
            }

            // Check pixels per unit
            if (calibration.PixelsPerUnit < 0.01 || calibration.PixelsPerUnit > 1000)
            {
                result.IsValid = false;
                result.ErrorMessage = "Pixels per unit is outside reasonable range";
                return result;
            }

            // Calculate accuracy score
            result.AccuracyScore = CalculateAccuracyScore(calibration.ScaleRatio);
            result.PixelsPerUnit = calibration.PixelsPerUnit;
            result.ScaleRatio = calibration.ScaleRatio;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating calibration");
            return new CalibrationResult
            {
                IsValid = false,
                ErrorMessage = "Validation failed: " + ex.Message
            };
        }
    }

    public Task<List<ScalePreset>> GetScalePresetsAsync()
    {
        return Task.FromResult(new List<ScalePreset>
        {
            new() { ScaleRatio = 10, DisplayName = "1:10", Description = "Detail drawings", IsCommon = false },
            new() { ScaleRatio = 20, DisplayName = "1:20", Description = "Large scale plans", IsCommon = true },
            new() { ScaleRatio = 25, DisplayName = "1:25", Description = "Room layouts", IsCommon = false },
            new() { ScaleRatio = 50, DisplayName = "1:50", Description = "Floor plans", IsCommon = true },
            new() { ScaleRatio = 100, DisplayName = "1:100", Description = "Building plans", IsCommon = true },
            new() { ScaleRatio = 200, DisplayName = "1:200", Description = "Site plans", IsCommon = true },
            new() { ScaleRatio = 250, DisplayName = "1:250", Description = "Site layouts", IsCommon = false },
            new() { ScaleRatio = 500, DisplayName = "1:500", Description = "Large site plans", IsCommon = true },
            new() { ScaleRatio = 1000, DisplayName = "1:1000", Description = "Area maps", IsCommon = false },
            new() { ScaleRatio = 1250, DisplayName = "1:1250", Description = "Location maps", IsCommon = false }
        });
    }

    public async Task<CalibrationResult> ApplyPresetScaleAsync(int packageDrawingId, decimal scaleRatio, int userId)
    {
        try
        {
            // Deactivate existing calibrations
            var existingCalibrations = await _context.Calibrations
                .Where(c => c.PackageDrawingId == packageDrawingId && c.IsActive)
                .ToListAsync();

            foreach (var existing in existingCalibrations)
            {
                existing.IsActive = false;
            }

            // Create preset calibration (using arbitrary pixel measurements)
            var calibration = new CalibrationData
            {
                PackageDrawingId = packageDrawingId,
                PixelsPerUnit = 1.0 / (double)scaleRatio, // Simplified calculation
                ScaleRatio = scaleRatio,
                KnownDistance = (double)scaleRatio, // Assume 1 unit = scale ratio mm
                MeasuredPixels = 1.0, // Normalized
                Point1Json = SerializePoint(new CalibrationPoint { X = 0, Y = 0 }),
                Point2Json = SerializePoint(new CalibrationPoint { X = 1, Y = 0 }),
                Units = "mm",
                CreatedBy = userId,
                Notes = $"Preset scale 1:{scaleRatio} applied",
                IsActive = true
            };

            _context.Calibrations.Add(calibration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Applied preset scale 1:{Scale} to drawing {DrawingId}",
                scaleRatio, packageDrawingId);

            return new CalibrationResult
            {
                IsValid = true,
                PixelsPerUnit = calibration.PixelsPerUnit,
                ScaleRatio = scaleRatio,
                AccuracyScore = CalculateAccuracyScore(scaleRatio)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying preset scale to drawing {DrawingId}", packageDrawingId);
            return new CalibrationResult
            {
                IsValid = false,
                ErrorMessage = "Failed to apply preset scale: " + ex.Message
            };
        }
    }

    public async Task<double> ConvertPixelsToRealWorldAsync(int packageDrawingId, double pixelDistance, string targetUnits = "mm")
    {
        try
        {
            var calibration = await GetActiveCalibrationAsync(packageDrawingId);
            if (calibration == null)
            {
                _logger.LogWarning("No active calibration found for drawing {DrawingId}, using default scale", packageDrawingId);
                return pixelDistance * 50; // Default 1:50 scale
            }

            var realWorldMm = pixelDistance / calibration.PixelsPerUnit;
            return ConvertFromMillimeters(realWorldMm, targetUnits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting pixels to real world for drawing {DrawingId}", packageDrawingId);
            return pixelDistance * 50; // Fallback to default
        }
    }

    public async Task<double> ConvertRealWorldToPixelsAsync(int packageDrawingId, double realWorldDistance, string sourceUnits = "mm")
    {
        try
        {
            var calibration = await GetActiveCalibrationAsync(packageDrawingId);
            if (calibration == null)
            {
                _logger.LogWarning("No active calibration found for drawing {DrawingId}, using default scale", packageDrawingId);
                return realWorldDistance / 50; // Default 1:50 scale
            }

            var realWorldMm = ConvertToMillimeters(realWorldDistance, sourceUnits);
            return realWorldMm * calibration.PixelsPerUnit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting real world to pixels for drawing {DrawingId}", packageDrawingId);
            return realWorldDistance / 50; // Fallback to default
        }
    }

    public async Task<CalibrationSession> GetCalibrationSessionAsync(int packageDrawingId)
    {
        try
        {
            var activeCalibration = await GetActiveCalibrationAsync(packageDrawingId);
            var presets = await GetScalePresetsAsync();

            return new CalibrationSession
            {
                PackageDrawingId = packageDrawingId,
                ActiveCalibration = activeCalibration,
                IsCalibrated = activeCalibration != null,
                LastUpdated = activeCalibration?.CreatedDate ?? DateTime.MinValue,
                AvailablePresets = presets
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calibration session for drawing {DrawingId}", packageDrawingId);
            return new CalibrationSession { PackageDrawingId = packageDrawingId };
        }
    }

    public async Task<bool> RecalibrateAllMeasurementsAsync(int packageDrawingId, int newCalibrationId)
    {
        try
        {
            // This would recalculate all existing measurements with the new calibration
            // Implementation depends on how measurements are stored

            _logger.LogInformation("Recalibrating all measurements for drawing {DrawingId} with calibration {CalibrationId}",
                packageDrawingId, newCalibrationId);

            // TODO: Implement measurement recalculation logic
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalibrating measurements for drawing {DrawingId}", packageDrawingId);
            return false;
        }
    }

    public async Task<double> GetCalibrationAccuracyAsync(int packageDrawingId)
    {
        try
        {
            var calibration = await GetActiveCalibrationAsync(packageDrawingId);
            if (calibration == null)
            {
                return 0;
            }

            return CalculateAccuracyScore(calibration.ScaleRatio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calibration accuracy for drawing {DrawingId}", packageDrawingId);
            return 0;
        }
    }

    public async Task<CalibrationResult> CreateQuickCalibrationAsync(int packageDrawingId, decimal scaleRatio, int userId)
    {
        return await ApplyPresetScaleAsync(packageDrawingId, scaleRatio, userId);
    }

    public async Task<string> ExportCalibrationDataAsync(int packageDrawingId)
    {
        try
        {
            var calibrations = await GetCalibrationHistoryAsync(packageDrawingId);
            return JsonSerializer.Serialize(calibrations, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting calibration data for drawing {DrawingId}", packageDrawingId);
            return "{}";
        }
    }

    public async Task<CalibrationResult> ImportCalibrationDataAsync(int packageDrawingId, string calibrationJson, int userId)
    {
        try
        {
            var calibrations = JsonSerializer.Deserialize<List<CalibrationData>>(calibrationJson);
            if (calibrations == null || !calibrations.Any())
            {
                return new CalibrationResult { IsValid = false, ErrorMessage = "No valid calibration data found" };
            }

            // Import the most recent calibration
            var latest = calibrations.OrderByDescending(c => c.CreatedDate).First();
            latest.PackageDrawingId = packageDrawingId;
            latest.CreatedBy = userId;
            latest.CreatedDate = DateTime.UtcNow;
            latest.IsActive = true;

            // Deactivate existing calibrations
            var existing = await _context.Calibrations
                .Where(c => c.PackageDrawingId == packageDrawingId && c.IsActive)
                .ToListAsync();

            foreach (var cal in existing)
            {
                cal.IsActive = false;
            }

            _context.Set<CalibrationData>().Add(latest);
            await _context.SaveChangesAsync();

            return new CalibrationResult
            {
                IsValid = true,
                PixelsPerUnit = latest.PixelsPerUnit,
                ScaleRatio = latest.ScaleRatio,
                AccuracyScore = CalculateAccuracyScore(latest.ScaleRatio)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing calibration data for drawing {DrawingId}", packageDrawingId);
            return new CalibrationResult
            {
                IsValid = false,
                ErrorMessage = "Failed to import calibration: " + ex.Message
            };
        }
    }

    // Helper methods
    private static double GetPixelDistance(CalibrationPoint point1, CalibrationPoint point2)
    {
        var dx = point2.X - point1.X;
        var dy = point2.Y - point1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double ConvertToMillimeters(double value, string units)
    {
        return units.ToLower() switch
        {
            "mm" => value,
            "cm" => value * 10,
            "m" => value * 1000,
            "km" => value * 1000000,
            "in" => value * 25.4,
            "ft" => value * 304.8,
            _ => value // Default to mm
        };
    }

    private static double ConvertFromMillimeters(double valueMm, string targetUnits)
    {
        return targetUnits.ToLower() switch
        {
            "mm" => valueMm,
            "cm" => valueMm / 10,
            "m" => valueMm / 1000,
            "km" => valueMm / 1000000,
            "in" => valueMm / 25.4,
            "ft" => valueMm / 304.8,
            _ => valueMm // Default to mm
        };
    }

    private static double CalculateAccuracyScore(decimal scaleRatio)
    {
        // Common architectural scales get higher scores
        var commonScales = new[] { 10m, 20m, 25m, 50m, 100m, 200m, 250m, 500m, 1000m, 1250m };

        if (commonScales.Contains(scaleRatio))
        {
            return 95.0; // High confidence for standard scales
        }

        // Calculate how close to a common scale
        var closestScale = commonScales.OrderBy(s => Math.Abs(s - scaleRatio)).First();
        var difference = Math.Abs((double)(closestScale - scaleRatio));
        var percentDifference = difference / (double)closestScale * 100;

        return Math.Max(50.0, 95.0 - percentDifference * 2); // Scale from 50-95%
    }

    private static string SerializePoint(CalibrationPoint point)
    {
        try
        {
            return JsonSerializer.Serialize(point);
        }
        catch (Exception)
        {
            return JsonSerializer.Serialize(new CalibrationPoint { X = 0, Y = 0 });
        }
    }
}