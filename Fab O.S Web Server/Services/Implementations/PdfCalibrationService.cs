using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service for managing PDF calibration and annotation persistence
/// </summary>
public class PdfCalibrationService : IPdfCalibrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PdfCalibrationService> _logger;

    public PdfCalibrationService(
        ApplicationDbContext context,
        ILogger<PdfCalibrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PdfScaleCalibration> SaveCalibrationAsync(
        int packageDrawingId,
        decimal scale,
        string unit,
        decimal? knownDistance,
        decimal? measuredDistance,
        int pageIndex,
        string? calibrationLineStart,
        string? calibrationLineEnd,
        int? userId,
        int companyId)
    {
        try
        {
            _logger.LogInformation("[PdfCalibrationService] Saving calibration for drawing {DrawingId}: Scale 1:{Scale}, Unit {Unit}",
                packageDrawingId, scale, unit);

            // Check if calibration already exists for this drawing
            var existing = await _context.PdfScaleCalibrations
                .FirstOrDefaultAsync(c => c.PackageDrawingId == packageDrawingId && c.CompanyId == companyId);

            if (existing != null)
            {
                // Update existing calibration
                existing.Scale = scale;
                existing.Unit = unit;
                existing.KnownDistance = knownDistance;
                existing.MeasuredDistance = measuredDistance;
                existing.PageIndex = pageIndex;
                existing.CalibrationLineStart = calibrationLineStart;
                existing.CalibrationLineEnd = calibrationLineEnd;
                existing.ModifiedDate = DateTime.UtcNow;

                _logger.LogInformation("[PdfCalibrationService] Updating existing calibration {Id}", existing.Id);
            }
            else
            {
                // Create new calibration
                var calibration = new PdfScaleCalibration
                {
                    PackageDrawingId = packageDrawingId,
                    Scale = scale,
                    Unit = unit,
                    KnownDistance = knownDistance,
                    MeasuredDistance = measuredDistance,
                    PageIndex = pageIndex,
                    CalibrationLineStart = calibrationLineStart,
                    CalibrationLineEnd = calibrationLineEnd,
                    CreatedByUserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    CompanyId = companyId
                };

                _context.PdfScaleCalibrations.Add(calibration);
                existing = calibration;

                _logger.LogInformation("[PdfCalibrationService] Creating new calibration for drawing {DrawingId}", packageDrawingId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[PdfCalibrationService] ✓ Calibration saved successfully: ID {Id}", existing.Id);
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error saving calibration for drawing {DrawingId}", packageDrawingId);
            throw;
        }
    }

    public async Task<PdfScaleCalibration?> GetCalibrationAsync(int packageDrawingId, int companyId)
    {
        try
        {
            var calibration = await _context.PdfScaleCalibrations
                .Where(c => c.PackageDrawingId == packageDrawingId && c.CompanyId == companyId)
                .OrderByDescending(c => c.CreatedDate)
                .FirstOrDefaultAsync();

            if (calibration != null)
            {
                _logger.LogInformation("[PdfCalibrationService] Found calibration for drawing {DrawingId}: Scale 1:{Scale}",
                    packageDrawingId, calibration.Scale);
            }
            else
            {
                _logger.LogInformation("[PdfCalibrationService] No calibration found for drawing {DrawingId}", packageDrawingId);
            }

            return calibration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error retrieving calibration for drawing {DrawingId}", packageDrawingId);
            throw;
        }
    }

    public async Task<Models.Entities.PdfAnnotation> SaveAnnotationAsync(
        int packageDrawingId,
        string annotationId,
        string annotationType,
        int pageIndex,
        string instantJson,
        bool isMeasurement,
        bool isCalibration,
        int? traceTakeoffMeasurementId,
        int? userId,
        int companyId)
    {
        try
        {
            _logger.LogInformation("[PdfCalibrationService] Saving annotation {AnnotationId} for drawing {DrawingId}",
                annotationId, packageDrawingId);

            // Check if annotation already exists
            var existing = await _context.PdfAnnotations
                .FirstOrDefaultAsync(a => a.AnnotationId == annotationId && a.CompanyId == companyId);

            if (existing != null)
            {
                // Update existing annotation
                existing.AnnotationType = annotationType;
                existing.PageIndex = pageIndex;
                existing.InstantJson = instantJson;
                existing.IsMeasurement = isMeasurement;
                existing.IsCalibration = isCalibration;
                existing.TraceTakeoffMeasurementId = traceTakeoffMeasurementId;
                existing.ModifiedDate = DateTime.UtcNow;

                _logger.LogInformation("[PdfCalibrationService] Updating existing annotation {Id}", existing.Id);
            }
            else
            {
                // Create new annotation
                var annotation = new Models.Entities.PdfAnnotation
                {
                    PackageDrawingId = packageDrawingId,
                    AnnotationId = annotationId,
                    AnnotationType = annotationType,
                    PageIndex = pageIndex,
                    InstantJson = instantJson,
                    IsMeasurement = isMeasurement,
                    IsCalibration = isCalibration,
                    TraceTakeoffMeasurementId = traceTakeoffMeasurementId,
                    CreatedByUserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    CompanyId = companyId
                };

                _context.PdfAnnotations.Add(annotation);
                existing = annotation;

                _logger.LogInformation("[PdfCalibrationService] Creating new annotation {AnnotationId}", annotationId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("[PdfCalibrationService] ✓ Annotation saved successfully: ID {Id}", existing.Id);
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error saving annotation {AnnotationId}", annotationId);
            throw;
        }
    }

    public async Task<List<Models.Entities.PdfAnnotation>> GetAnnotationsAsync(int packageDrawingId, int companyId)
    {
        try
        {
            var annotations = await _context.PdfAnnotations
                .Where(a => a.PackageDrawingId == packageDrawingId && a.CompanyId == companyId)
                .OrderBy(a => a.PageIndex)
                .ThenBy(a => a.CreatedDate)
                .ToListAsync();

            _logger.LogInformation("[PdfCalibrationService] Retrieved {Count} annotations for drawing {DrawingId}",
                annotations.Count, packageDrawingId);

            return annotations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error retrieving annotations for drawing {DrawingId}", packageDrawingId);
            throw;
        }
    }

    public async Task<Models.Entities.PdfAnnotation?> UpdateAnnotationAsync(string annotationId, string instantJson, int companyId)
    {
        try
        {
            var annotation = await _context.PdfAnnotations
                .FirstOrDefaultAsync(a => a.AnnotationId == annotationId && a.CompanyId == companyId);

            if (annotation == null)
            {
                _logger.LogWarning("[PdfCalibrationService] Annotation {AnnotationId} not found for update", annotationId);
                return null;
            }

            annotation.InstantJson = instantJson;
            annotation.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[PdfCalibrationService] ✓ Annotation {AnnotationId} updated", annotationId);
            return annotation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error updating annotation {AnnotationId}", annotationId);
            throw;
        }
    }

    public async Task<bool> DeleteAnnotationAsync(string annotationId, int companyId)
    {
        try
        {
            var annotation = await _context.PdfAnnotations
                .FirstOrDefaultAsync(a => a.AnnotationId == annotationId && a.CompanyId == companyId);

            if (annotation == null)
            {
                _logger.LogWarning("[PdfCalibrationService] Annotation {AnnotationId} not found for deletion", annotationId);
                return false;
            }

            _context.PdfAnnotations.Remove(annotation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[PdfCalibrationService] ✓ Annotation {AnnotationId} deleted", annotationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error deleting annotation {AnnotationId}", annotationId);
            throw;
        }
    }

    public async Task<(PdfScaleCalibration? Calibration, List<Models.Entities.PdfAnnotation> Annotations)> GetPdfStateAsync(
        int packageDrawingId,
        int companyId)
    {
        try
        {
            _logger.LogInformation("[PdfCalibrationService] Loading complete PDF state for drawing {DrawingId}", packageDrawingId);

            var calibration = await GetCalibrationAsync(packageDrawingId, companyId);
            var annotations = await GetAnnotationsAsync(packageDrawingId, companyId);

            _logger.LogInformation("[PdfCalibrationService] ✓ Loaded PDF state: Calibration={HasCalibration}, Annotations={Count}",
                calibration != null, annotations.Count);

            return (calibration, annotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PdfCalibrationService] Error loading PDF state for drawing {DrawingId}", packageDrawingId);
            throw;
        }
    }
}
