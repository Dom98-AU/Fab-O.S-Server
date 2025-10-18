using FabOS.WebServer.Models.Entities;
using PdfAnnotationEntity = FabOS.WebServer.Models.Entities.PdfAnnotation;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing PDF calibration and annotation persistence
/// </summary>
public interface IPdfCalibrationService
{
    /// <summary>
    /// Save or update scale calibration for a package drawing
    /// </summary>
    /// <param name="packageDrawingId">Package drawing ID</param>
    /// <param name="scale">Scale ratio (e.g., 50 for 1:50)</param>
    /// <param name="unit">Unit of measurement (mm, m, ft, in)</param>
    /// <param name="knownDistance">Known real-world distance</param>
    /// <param name="measuredDistance">Measured distance in PDF units</param>
    /// <param name="pageIndex">Page number (0-indexed)</param>
    /// <param name="calibrationLineStart">Start coordinates JSON</param>
    /// <param name="calibrationLineEnd">End coordinates JSON</param>
    /// <param name="userId">User creating the calibration</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>Saved calibration entity</returns>
    Task<PdfScaleCalibration> SaveCalibrationAsync(
        int packageDrawingId,
        decimal scale,
        string unit,
        decimal? knownDistance,
        decimal? measuredDistance,
        int pageIndex,
        string? calibrationLineStart,
        string? calibrationLineEnd,
        int? userId,
        int companyId);

    /// <summary>
    /// Get the most recent calibration for a package drawing
    /// </summary>
    /// <param name="packageDrawingId">Package drawing ID</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>Calibration or null if not found</returns>
    Task<PdfScaleCalibration?> GetCalibrationAsync(int packageDrawingId, int companyId);

    /// <summary>
    /// Save a PDF annotation (from PSPDFKit Instant JSON)
    /// </summary>
    /// <param name="packageDrawingId">Package drawing ID</param>
    /// <param name="annotationId">Nutrient/PSPDFKit annotation ID</param>
    /// <param name="annotationType">Type of annotation</param>
    /// <param name="pageIndex">Page number (0-indexed)</param>
    /// <param name="instantJson">PSPDFKit Instant JSON</param>
    /// <param name="isMeasurement">Is this a measurement annotation?</param>
    /// <param name="isCalibration">Is this a calibration annotation?</param>
    /// <param name="traceTakeoffMeasurementId">Related measurement ID (optional)</param>
    /// <param name="userId">User creating the annotation</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>Saved annotation entity</returns>
    Task<PdfAnnotationEntity> SaveAnnotationAsync(
        int packageDrawingId,
        string annotationId,
        string annotationType,
        int pageIndex,
        string instantJson,
        bool isMeasurement,
        bool isCalibration,
        int? traceTakeoffMeasurementId,
        int? userId,
        int companyId);

    /// <summary>
    /// Get all annotations for a package drawing
    /// </summary>
    /// <param name="packageDrawingId">Package drawing ID</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>List of annotations</returns>
    Task<List<PdfAnnotationEntity>> GetAnnotationsAsync(int packageDrawingId, int companyId);

    /// <summary>
    /// Update an existing annotation (when modified in PDF viewer)
    /// </summary>
    /// <param name="annotationId">Nutrient annotation ID</param>
    /// <param name="instantJson">Updated PSPDFKit Instant JSON</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>Updated annotation or null if not found</returns>
    Task<PdfAnnotationEntity?> UpdateAnnotationAsync(string annotationId, string instantJson, int companyId);

    /// <summary>
    /// Delete an annotation
    /// </summary>
    /// <param name="annotationId">Nutrient annotation ID</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAnnotationAsync(string annotationId, int companyId);

    /// <summary>
    /// Get complete calibration and annotation data for restoring a PDF
    /// </summary>
    /// <param name="packageDrawingId">Package drawing ID</param>
    /// <param name="companyId">Company ID</param>
    /// <returns>Tuple of calibration and annotations</returns>
    Task<(PdfScaleCalibration? Calibration, List<PdfAnnotationEntity> Annotations)> GetPdfStateAsync(
        int packageDrawingId,
        int companyId);
}
