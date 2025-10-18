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
        private readonly ILogger<TakeoffCatalogueController> _logger;

        public TakeoffCatalogueController(
            ITakeoffCatalogueService catalogueService,
            ILogger<TakeoffCatalogueController> logger)
        {
            _catalogueService = catalogueService;
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
                var companyId = GetCompanyId();
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

                var companyId = GetCompanyId();
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
                var companyId = GetCompanyId();
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

                var companyId = GetCompanyId();
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

                var companyId = GetCompanyId();
                var result = await _catalogueService.CalculateMeasurementAsync(
                    request.CatalogueItemId,
                    request.MeasurementType,
                    request.Value,
                    request.Unit,
                    companyId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid calculation request");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating measurement");
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

                var companyId = GetCompanyId();
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
                var companyId = GetCompanyId();
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

                var companyId = GetCompanyId();
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
                var companyId = GetCompanyId();
                var success = await _catalogueService.DeleteMeasurementAsync(id, companyId);

                if (!success)
                {
                    return NotFound(new { error = $"Measurement {id} not found" });
                }

                return NoContent();
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
                var companyId = GetCompanyId();
                var summary = await _catalogueService.GetDrawingSummaryAsync(drawingId, companyId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving drawing summary for drawing {DrawingId}", drawingId);
                return StatusCode(500, new { error = "Failed to retrieve drawing summary" });
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
}
