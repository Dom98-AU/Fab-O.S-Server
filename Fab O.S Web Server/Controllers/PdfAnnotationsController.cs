using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.Entities;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for managing PDF annotations (measurements, calibrations, and general annotations)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PdfAnnotationsController : ControllerBase
{
    private readonly Services.Interfaces.IPdfAnnotationService _annotationService;
    private readonly ILogger<PdfAnnotationsController> _logger;

    public PdfAnnotationsController(
        Services.Interfaces.IPdfAnnotationService annotationService,
        ILogger<PdfAnnotationsController> logger)
    {
        _annotationService = annotationService;
        _logger = logger;
    }

    /// <summary>
    /// Save a single annotation
    /// POST /api/PdfAnnotations
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveAnnotation([FromBody] Models.Entities.PdfAnnotation annotation)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid annotation data", errors = ModelState });
            }

            // Set audit fields
            var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
            var companyId = int.Parse(User.FindFirst("company_id")?.Value ?? "0");

            if (annotation.Id == 0) // New annotation
            {
                annotation.CreatedByUserId = userId;
                annotation.CompanyId = companyId;
            }

            var savedAnnotation = await _annotationService.SaveAnnotationAsync(annotation);

            _logger.LogInformation("Saved annotation {AnnotationId} for drawing {DrawingId}",
                savedAnnotation.AnnotationId, savedAnnotation.PackageDrawingId);

            return Ok(savedAnnotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving annotation");
            return StatusCode(500, new { message = "An error occurred while saving annotation" });
        }
    }

    /// <summary>
    /// Save multiple annotations in a batch
    /// POST /api/PdfAnnotations/batch
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> SaveAnnotationsBatch([FromBody] List<Models.Entities.PdfAnnotation> annotations)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid annotations data", errors = ModelState });
            }

            // Set audit fields for all annotations
            var userId = int.Parse(User.FindFirst("user_id")?.Value ?? "0");
            var companyId = int.Parse(User.FindFirst("company_id")?.Value ?? "0");

            foreach (var annotation in annotations.Where(a => a.Id == 0))
            {
                annotation.CreatedByUserId = userId;
                annotation.CompanyId = companyId;
            }

            var savedAnnotations = await _annotationService.SaveAnnotationsBatchAsync(annotations);

            _logger.LogInformation("Saved batch of {Count} annotations", savedAnnotations.Count);

            return Ok(savedAnnotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving annotations batch");
            return StatusCode(500, new { message = "An error occurred while saving annotations" });
        }
    }

    /// <summary>
    /// Get all annotations for a drawing
    /// GET /api/PdfAnnotations/drawing/{packageDrawingId}
    /// </summary>
    [HttpGet("drawing/{packageDrawingId}")]
    public async Task<IActionResult> GetAnnotationsByDrawing(int packageDrawingId)
    {
        try
        {
            var annotations = await _annotationService.GetAnnotationsByDrawingAsync(packageDrawingId);

            _logger.LogInformation("Retrieved {Count} annotations for drawing {DrawingId}",
                annotations.Count, packageDrawingId);

            return Ok(annotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting annotations for drawing {DrawingId}", packageDrawingId);
            return StatusCode(500, new { message = "An error occurred while retrieving annotations" });
        }
    }

    /// <summary>
    /// Get all measurement annotations for a drawing
    /// GET /api/PdfAnnotations/drawing/{packageDrawingId}/measurements
    /// </summary>
    [HttpGet("drawing/{packageDrawingId}/measurements")]
    public async Task<IActionResult> GetMeasurementAnnotationsByDrawing(int packageDrawingId)
    {
        try
        {
            var annotations = await _annotationService.GetMeasurementAnnotationsByDrawingAsync(packageDrawingId);

            _logger.LogInformation("Retrieved {Count} measurement annotations for drawing {DrawingId}",
                annotations.Count, packageDrawingId);

            return Ok(annotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting measurement annotations for drawing {DrawingId}", packageDrawingId);
            return StatusCode(500, new { message = "An error occurred while retrieving measurement annotations" });
        }
    }

    /// <summary>
    /// Get annotations for a specific page
    /// GET /api/PdfAnnotations/drawing/{packageDrawingId}/page/{pageIndex}
    /// </summary>
    [HttpGet("drawing/{packageDrawingId}/page/{pageIndex}")]
    public async Task<IActionResult> GetAnnotationsByPage(int packageDrawingId, int pageIndex)
    {
        try
        {
            var annotations = await _annotationService.GetAnnotationsByPageAsync(packageDrawingId, pageIndex);

            _logger.LogInformation("Retrieved {Count} annotations for drawing {DrawingId} page {PageIndex}",
                annotations.Count, packageDrawingId, pageIndex);

            return Ok(annotations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting annotations for drawing {DrawingId} page {PageIndex}",
                packageDrawingId, pageIndex);
            return StatusCode(500, new { message = "An error occurred while retrieving annotations" });
        }
    }

    /// <summary>
    /// Get a specific annotation by Nutrient ID
    /// GET /api/PdfAnnotations/{annotationId}?drawingId={packageDrawingId}
    /// </summary>
    [HttpGet("{annotationId}")]
    public async Task<IActionResult> GetAnnotationByNutrientId(string annotationId, [FromQuery] int drawingId)
    {
        try
        {
            var annotation = await _annotationService.GetAnnotationByNutrientIdAsync(annotationId, drawingId);

            if (annotation == null)
            {
                return NotFound(new { message = "Annotation not found" });
            }

            return Ok(annotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting annotation {AnnotationId}", annotationId);
            return StatusCode(500, new { message = "An error occurred while retrieving annotation" });
        }
    }

    /// <summary>
    /// Delete an annotation
    /// DELETE /api/PdfAnnotations/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnnotation(int id)
    {
        try
        {
            var result = await _annotationService.DeleteAnnotationAsync(id);

            if (!result)
            {
                return NotFound(new { message = "Annotation not found" });
            }

            _logger.LogInformation("Deleted annotation {AnnotationId}", id);

            return Ok(new { message = "Annotation deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting annotation {AnnotationId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting annotation" });
        }
    }

    /// <summary>
    /// Delete all annotations for a drawing
    /// DELETE /api/PdfAnnotations/drawing/{packageDrawingId}
    /// </summary>
    [HttpDelete("drawing/{packageDrawingId}")]
    public async Task<IActionResult> DeleteAllAnnotationsForDrawing(int packageDrawingId)
    {
        try
        {
            var result = await _annotationService.DeleteAllAnnotationsForDrawingAsync(packageDrawingId);

            _logger.LogInformation("Deleted all annotations for drawing {DrawingId}", packageDrawingId);

            return Ok(new { message = "All annotations deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting annotations for drawing {DrawingId}", packageDrawingId);
            return StatusCode(500, new { message = "An error occurred while deleting annotations" });
        }
    }
}
