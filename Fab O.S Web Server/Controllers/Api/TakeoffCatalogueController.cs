using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/takeoff/catalogue")]
    public class TakeoffCatalogueController : ControllerBase
    {
        private readonly ITakeoffCatalogueService _catalogueService;
        private readonly IPdfCalibrationService _calibrationService;
        private readonly ILogger<TakeoffCatalogueController> _logger;

        public TakeoffCatalogueController(
            ITakeoffCatalogueService catalogueService,
            IPdfCalibrationService calibrationService,
            ILogger<TakeoffCatalogueController> logger)
        {
            _catalogueService = catalogueService;
            _calibrationService = calibrationService;
            _logger = logger;
        }

        private int GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return companyIdClaim != null ? int.Parse(companyIdClaim) : 0;
        }

        /// <summary>
        /// Get all catalogue categories
        /// GET /api/takeoff/catalogue/categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            try
            {
                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var categories = await _catalogueService.GetCategoriesAsync(companyId);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue categories");
                return StatusCode(500, new { error = "Failed to retrieve categories" });
            }
        }

        /// <summary>
        /// Get catalogue items by category
        /// GET /api/takeoff/catalogue/items?category=Plates
        /// </summary>
        [HttpGet("items")]
        public async Task<ActionResult<List<CatalogueItem>>> GetItemsByCategory([FromQuery] string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    return BadRequest(new { error = "Category parameter is required" });
                }

                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var items = await _catalogueService.GetItemsByCategoryAsync(category, companyId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue items for category {Category}", category);
                return StatusCode(500, new { error = "Failed to retrieve catalogue items" });
            }
        }

        /// <summary>
        /// Get a specific catalogue item by ID
        /// GET /api/takeoff/catalogue/items/123
        /// </summary>
        [HttpGet("items/{id}")]
        public async Task<ActionResult<CatalogueItem>> GetItemById(int id)
        {
            try
            {
                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var item = await _catalogueService.GetItemByIdAsync(id, companyId);

                if (item == null)
                {
                    return NotFound(new { error = $"Catalogue item {id} not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue item {Id}", id);
                return StatusCode(500, new { error = "Failed to retrieve catalogue item" });
            }
        }

        /// <summary>
        /// Search catalogue items
        /// GET /api/takeoff/catalogue/search?q=350MS
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<CatalogueItem>>> SearchItems([FromQuery] string q, [FromQuery] int maxResults = 50)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { error = "Search query 'q' parameter is required" });
                }

                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var items = await _catalogueService.SearchItemsAsync(q, companyId, maxResults);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching catalogue items with query '{Query}'", q);
                return StatusCode(500, new { error = "Failed to search catalogue items" });
            }
        }

        /// <summary>
        /// Calculate measurement result
        /// POST /api/takeoff/catalogue/calculate
        /// </summary>
        [HttpPost("calculate")]
        public async Task<ActionResult<MeasurementCalculationResult>> CalculateMeasurement(
            [FromBody] CalculateMeasurementRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                // Use CompanyId = 1 (consistent with SaveAnnotation and DeleteAnnotation)
                // TODO: In production, get companyId from user claims or drawing's package
                int companyId = 1;

                // Log diagnostic information
                _logger.LogInformation("Calculate measurement request: CatalogueItemId={CatalogueItemId}, CompanyId={CompanyId}, MeasurementType={MeasurementType}, Value={Value}, Unit={Unit}, AnnotationId={AnnotationId}",
                    request.CatalogueItemId, companyId, request.MeasurementType, request.Value, request.Unit, request.AnnotationId);

                var result = await _catalogueService.CalculateMeasurementAsync(
                    request.CatalogueItemId,
                    request.MeasurementType,
                    request.Value,
                    request.Unit,
                    companyId);

                // Pass the annotation ID through to the result
                result.AnnotationId = request.AnnotationId;

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid calculation request for CatalogueItemId={CatalogueItemId}, CompanyId={CompanyId}",
                    request?.CatalogueItemId, 1);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating measurement for CatalogueItemId={CatalogueItemId}", request?.CatalogueItemId);
                return StatusCode(500, new { error = "Failed to calculate measurement" });
            }
        }

        /// <summary>
        /// Create a new takeoff measurement
        /// POST /api/takeoff/measurements
        /// </summary>
        [HttpPost("measurements")]
        public async Task<ActionResult<TraceTakeoffMeasurement>> CreateMeasurement(
            [FromBody] CreateMeasurementRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                // Use CompanyId = 1 (consistent with SaveAnnotation and DeleteAnnotation)
                // TODO: In production, get companyId from user claims or drawing's package
                int companyId = 1;
                var measurement = await _catalogueService.CreateMeasurementAsync(
                    request.TraceTakeoffId,
                    request.PackageDrawingId,
                    request.CatalogueItemId,
                    request.MeasurementType,
                    request.Value,
                    request.Unit,
                    request.Coordinates,
                    companyId);

                return CreatedAtAction(
                    nameof(GetMeasurementsByDrawing),
                    new { drawingId = request.PackageDrawingId },
                    measurement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating measurement");
                return StatusCode(500, new { error = "Failed to create measurement" });
            }
        }

        /// <summary>
        /// Get all measurements for a drawing
        /// GET /api/takeoff/measurements/drawing/123
        /// </summary>
        [HttpGet("measurements/drawing/{drawingId}")]
        public async Task<ActionResult<List<TraceTakeoffMeasurement>>> GetMeasurementsByDrawing(int drawingId)
        {
            try
            {
                // Use CompanyId = 1 (consistent with SaveAnnotation and DeleteAnnotation)
                // TODO: In production, get companyId from user claims or drawing's package
                int companyId = 1;
                var measurements = await _catalogueService.GetMeasurementsByDrawingAsync(drawingId, companyId);
                return Ok(measurements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving measurements for drawing {DrawingId}", drawingId);
                return StatusCode(500, new { error = "Failed to retrieve measurements" });
            }
        }

        /// <summary>
        /// Update a measurement
        /// PUT /api/takeoff/measurements/456
        /// </summary>
        [HttpPut("measurements/{id}")]
        public async Task<ActionResult<TraceTakeoffMeasurement>> UpdateMeasurement(
            int id,
            [FromBody] UpdateMeasurementRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Request body is required" });
                }

                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var measurement = await _catalogueService.UpdateMeasurementAsync(
                    id,
                    request.Value,
                    request.Coordinates,
                    companyId);

                if (measurement == null)
                {
                    return NotFound(new { error = $"Measurement {id} not found" });
                }

                return Ok(measurement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating measurement {Id}", id);
                return StatusCode(500, new { error = "Failed to update measurement" });
            }
        }

        /// <summary>
        /// Delete a measurement
        /// DELETE /api/takeoff/measurements/456
        /// </summary>
        [HttpDelete("measurements/{id}")]
        public async Task<ActionResult> DeleteMeasurement(int id)
        {
            try
            {
                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var deletedAnnotationIds = await _catalogueService.DeleteMeasurementAsync(id, companyId);

                if (deletedAnnotationIds == null || deletedAnnotationIds.Count == 0)
                {
                    // Could mean measurement not found OR measurement had no linked annotations
                    // Check if measurement existed by trying to get it first would be more accurate,
                    // but for now we'll assume deletion was successful even without annotations
                    return Ok(new { deletedAnnotationIds = new List<string>() });
                }

                // Return the list of annotation IDs that were deleted (for PDF viewer cleanup)
                return Ok(new { deletedAnnotationIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting measurement {Id}", id);
                return StatusCode(500, new { error = "Failed to delete measurement" });
            }
        }

        /// <summary>
        /// Get summary statistics for a drawing
        /// GET /api/takeoff/measurements/drawing/123/summary
        /// </summary>
        [HttpGet("measurements/drawing/{drawingId}/summary")]
        public async Task<ActionResult<DrawingMeasurementSummary>> GetDrawingSummary(int drawingId)
        {
            try
            {
                // Use CompanyId = 1 for consistency across all endpoints
                int companyId = 1;
                var summary = await _catalogueService.GetDrawingSummaryAsync(drawingId, companyId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drawing summary for drawing {DrawingId}", drawingId);
                return StatusCode(500, new { error = "Failed to retrieve drawing summary" });
            }
        }

        /// <summary>
        /// Save a PDF annotation to the database
        /// POST /api/takeoff/annotations
        /// </summary>
        [HttpPost("annotations")]
        public async Task<ActionResult> SaveAnnotation([FromBody] SaveAnnotationRequest request)
        {
            try
            {
                _logger.LogInformation("[API] SaveAnnotation called for annotation {AnnotationId}, drawing {DrawingId}",
                    request.AnnotationId, request.PackageDrawingId);

                // Use CompanyId = 1 (confirmed to exist in database as "Steel Estimation Platform")
                // TODO: In production, get companyId from user claims or drawing's package
                int companyId = 1;

                // Save the annotation using the service
                // Foreign key constraints will validate that PackageDrawingId exists
                var savedAnnotation = await _calibrationService.SaveAnnotationAsync(
                    packageDrawingId: request.PackageDrawingId,
                    annotationId: request.AnnotationId,
                    annotationType: request.AnnotationType,
                    pageIndex: request.PageIndex,
                    instantJson: request.InstantJson ?? "{}",
                    isMeasurement: request.IsMeasurement,
                    isCalibration: false,
                    traceTakeoffMeasurementId: null,
                    userId: null,
                    companyId: companyId
                );

                _logger.LogInformation("[API] ✓ Successfully saved annotation {AnnotationId}", request.AnnotationId);
                return Ok(new { success = true, message = "Annotation saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] Error saving annotation {AnnotationId}", request.AnnotationId);
                return StatusCode(500, new { error = "Failed to save annotation" });
            }
        }

        /// <summary>
        /// Delete a PDF annotation and its linked measurement (cascade delete)
        /// DELETE /api/takeoff/annotations/{annotationId}
        /// </summary>
        [HttpDelete("annotations/{annotationId}")]
        public async Task<ActionResult> DeleteAnnotation(string annotationId)
        {
            try
            {
                _logger.LogInformation("[API] DeleteAnnotation called for annotation {AnnotationId}", annotationId);

                // Use CompanyId = 1 (same as SaveAnnotation to ensure consistency)
                // TODO: In production, get companyId from user claims or drawing's package
                int companyId = 1;
                var result = await _calibrationService.DeleteAnnotationAsync(annotationId, companyId);

                if (!result)
                {
                    _logger.LogWarning("[API] Annotation {AnnotationId} not found", annotationId);
                    return NotFound(new { error = $"Annotation {annotationId} not found" });
                }

                _logger.LogInformation("[API] ✓ Successfully deleted annotation {AnnotationId} and linked measurement", annotationId);
                return Ok(new { success = true, message = "Annotation and linked measurement deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] Error deleting annotation {AnnotationId}", annotationId);
                return StatusCode(500, new { error = "Failed to delete annotation" });
            }
        }
    }

    // Request DTOs
    public class CalculateMeasurementRequest
    {
        public int CatalogueItemId { get; set; }
        public string MeasurementType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? AnnotationId { get; set; }
    }

    public class CreateMeasurementRequest
    {
        public int TraceTakeoffId { get; set; }
        public int PackageDrawingId { get; set; }
        public int CatalogueItemId { get; set; }
        public string MeasurementType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Coordinates { get; set; }
    }

    public class UpdateMeasurementRequest
    {
        public decimal Value { get; set; }
        public string? Coordinates { get; set; }
    }

    public class SaveAnnotationRequest
    {
        public string AnnotationId { get; set; } = string.Empty;
        public int PackageDrawingId { get; set; }
        public string AnnotationType { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public bool IsMeasurement { get; set; }
        public string? InstantJson { get; set; }
    }
}
