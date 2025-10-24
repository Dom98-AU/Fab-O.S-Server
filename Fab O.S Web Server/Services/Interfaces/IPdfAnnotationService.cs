using PdfAnnotationEntity = FabOS.WebServer.Models.Entities.PdfAnnotation;

namespace FabOS.WebServer.Services.Interfaces;

public interface IPdfAnnotationService
{
    /// <summary>
    /// Save or update a PDF annotation (measurement, calibration, or general annotation)
    /// </summary>
    Task<PdfAnnotationEntity> SaveAnnotationAsync(PdfAnnotationEntity annotation);

    /// <summary>
    /// Save multiple annotations in a batch
    /// </summary>
    Task<List<PdfAnnotationEntity>> SaveAnnotationsBatchAsync(List<PdfAnnotationEntity> annotations);

    /// <summary>
    /// Get all annotations for a specific drawing
    /// </summary>
    Task<List<PdfAnnotationEntity>> GetAnnotationsByDrawingAsync(int packageDrawingId);

    /// <summary>
    /// Get all measurement annotations for a specific drawing
    /// </summary>
    Task<List<PdfAnnotationEntity>> GetMeasurementAnnotationsByDrawingAsync(int packageDrawingId);

    /// <summary>
    /// Get annotations for a specific page of a drawing
    /// </summary>
    Task<List<PdfAnnotationEntity>> GetAnnotationsByPageAsync(int packageDrawingId, int pageIndex);

    /// <summary>
    /// Get a specific annotation by its Nutrient annotation ID
    /// </summary>
    Task<PdfAnnotationEntity?> GetAnnotationByNutrientIdAsync(string annotationId, int packageDrawingId);

    /// <summary>
    /// Delete an annotation
    /// </summary>
    Task<bool> DeleteAnnotationAsync(int annotationId);

    /// <summary>
    /// Delete all annotations for a drawing
    /// </summary>
    Task<bool> DeleteAllAnnotationsForDrawingAsync(int packageDrawingId);
}
