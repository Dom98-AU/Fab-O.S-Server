using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using PdfAnnotationEntity = FabOS.WebServer.Models.Entities.PdfAnnotation;

namespace FabOS.WebServer.Services.Implementations;

public class PdfAnnotationService : Services.Interfaces.IPdfAnnotationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PdfAnnotationService> _logger;

    public PdfAnnotationService(
        ApplicationDbContext context,
        ILogger<PdfAnnotationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PdfAnnotationEntity> SaveAnnotationAsync(PdfAnnotationEntity annotation)
    {
        try
        {
            // Check if annotation already exists (by NutrientId and PackageDrawingId)
            var existing = await _context.PdfAnnotations
                .FirstOrDefaultAsync(a =>
                    a.AnnotationId == annotation.AnnotationId &&
                    a.PackageDrawingId == annotation.PackageDrawingId);

            if (existing != null)
            {
                // Update existing annotation
                existing.AnnotationType = annotation.AnnotationType;
                existing.PageIndex = annotation.PageIndex;
                existing.InstantJson = annotation.InstantJson;
                existing.IsMeasurement = annotation.IsMeasurement;
                existing.IsCalibration = annotation.IsCalibration;
                existing.TraceTakeoffMeasurementId = annotation.TraceTakeoffMeasurementId;
                existing.MeasurementValue = annotation.MeasurementValue;
                existing.MeasurementScale = annotation.MeasurementScale;
                existing.MeasurementPrecision = annotation.MeasurementPrecision;
                existing.CoordinatesData = annotation.CoordinatesData;
                existing.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated annotation {AnnotationId} for drawing {DrawingId}",
                    annotation.AnnotationId, annotation.PackageDrawingId);

                return existing;
            }
            else
            {
                // Create new annotation
                annotation.CreatedDate = DateTime.UtcNow;
                _context.PdfAnnotations.Add(annotation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created annotation {AnnotationId} for drawing {DrawingId}",
                    annotation.AnnotationId, annotation.PackageDrawingId);

                return annotation;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving annotation {AnnotationId} for drawing {DrawingId}",
                annotation.AnnotationId, annotation.PackageDrawingId);
            throw;
        }
    }

    public async Task<List<PdfAnnotationEntity>> SaveAnnotationsBatchAsync(List<PdfAnnotationEntity> annotations)
    {
        try
        {
            var savedAnnotations = new List<PdfAnnotationEntity>();

            foreach (var annotation in annotations)
            {
                var saved = await SaveAnnotationAsync(annotation);
                savedAnnotations.Add(saved);
            }

            _logger.LogInformation("Saved batch of {Count} annotations", annotations.Count);
            return savedAnnotations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch of {Count} annotations", annotations.Count);
            throw;
        }
    }

    public async Task<List<PdfAnnotationEntity>> GetAnnotationsByDrawingAsync(int packageDrawingId)
    {
        try
        {
            return await _context.PdfAnnotations
                .Where(a => a.PackageDrawingId == packageDrawingId)
                .OrderBy(a => a.PageIndex)
                .ThenBy(a => a.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting annotations for drawing {DrawingId}", packageDrawingId);
            return new List<PdfAnnotationEntity>();
        }
    }

    public async Task<List<PdfAnnotationEntity>> GetMeasurementAnnotationsByDrawingAsync(int packageDrawingId)
    {
        try
        {
            return await _context.PdfAnnotations
                .Where(a => a.PackageDrawingId == packageDrawingId && a.IsMeasurement)
                .OrderBy(a => a.PageIndex)
                .ThenBy(a => a.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting measurement annotations for drawing {DrawingId}", packageDrawingId);
            return new List<PdfAnnotationEntity>();
        }
    }

    public async Task<List<PdfAnnotationEntity>> GetAnnotationsByPageAsync(int packageDrawingId, int pageIndex)
    {
        try
        {
            return await _context.PdfAnnotations
                .Where(a => a.PackageDrawingId == packageDrawingId && a.PageIndex == pageIndex)
                .OrderBy(a => a.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting annotations for drawing {DrawingId} page {PageIndex}",
                packageDrawingId, pageIndex);
            return new List<PdfAnnotationEntity>();
        }
    }

    public async Task<PdfAnnotationEntity?> GetAnnotationByNutrientIdAsync(string annotationId, int packageDrawingId)
    {
        try
        {
            return await _context.PdfAnnotations
                .FirstOrDefaultAsync(a =>
                    a.AnnotationId == annotationId &&
                    a.PackageDrawingId == packageDrawingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting annotation {AnnotationId} for drawing {DrawingId}",
                annotationId, packageDrawingId);
            return null;
        }
    }

    public async Task<bool> DeleteAnnotationAsync(int annotationId)
    {
        try
        {
            var annotation = await _context.PdfAnnotations.FindAsync(annotationId);
            if (annotation == null)
            {
                _logger.LogWarning("Annotation {AnnotationId} not found for deletion", annotationId);
                return false;
            }

            _context.PdfAnnotations.Remove(annotation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted annotation {AnnotationId}", annotationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting annotation {AnnotationId}", annotationId);
            return false;
        }
    }

    public async Task<bool> DeleteAllAnnotationsForDrawingAsync(int packageDrawingId)
    {
        try
        {
            var annotations = await _context.PdfAnnotations
                .Where(a => a.PackageDrawingId == packageDrawingId)
                .ToListAsync();

            if (!annotations.Any())
            {
                _logger.LogInformation("No annotations found for drawing {DrawingId}", packageDrawingId);
                return true;
            }

            _context.PdfAnnotations.RemoveRange(annotations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} annotations for drawing {DrawingId}",
                annotations.Count, packageDrawingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all annotations for drawing {DrawingId}", packageDrawingId);
            return false;
        }
    }
}
