using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace FabOS.WebServer.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Allow anonymous for testing
    public class TestTraceController : ControllerBase
    {
        private readonly ITraceService _traceService;
        private readonly IPdfProcessingService _pdfService;
        private readonly ITakeoffService _takeoffService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestTraceController> _logger;

        public TestTraceController(
            ITraceService traceService,
            IPdfProcessingService pdfService,
            ITakeoffService takeoffService,
            ApplicationDbContext context,
            ILogger<TestTraceController> logger)
        {
            _traceService = traceService;
            _pdfService = pdfService;
            _takeoffService = takeoffService;
            _context = context;
            _logger = logger;
        }

        // GET: api/testTrace/catalogueItems
        [HttpGet("catalogueItems")]
        [SwaggerOperation(Summary = "Get catalogue items", Description = "Retrieves catalogue items with optional category filter")]
        [SwaggerResponse(200, "Successfully retrieved catalogue items")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GetCatalogueItems([FromQuery] string category = null, [FromQuery] int limit = 20)
        {
            try
            {
                var query = _context.CatalogueItems.AsQueryable();

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(c => c.Category == category);
                }

                var items = await query.Take(limit).ToListAsync();

                return Ok(new
                {
                    TotalCount = await _context.CatalogueItems.CountAsync(),
                    Categories = await _context.CatalogueItems.Select(c => c.Category).Distinct().CountAsync(),
                    Items = items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving catalogue items");
                return StatusCode(500, new { Error = "An error occurred while retrieving catalogue items" });
            }
        }

        // GET: api/testTrace/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.CatalogueItems
                    .GroupBy(c => c.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Category)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { Error = "An error occurred while retrieving categories" });
            }
        }

        // POST: api/testTrace/createTrace
        [HttpPost("createTrace")]
        public async Task<IActionResult> CreateTestTrace()
        {
            try
            {
                var traceRecord = new TraceRecord
                {
                    EntityType = TraceableType.WorkOrder,
                    EntityId = 1,
                    Description = "Test Trace for Takeoff Development",
                    CompanyId = 1
                };

                var created = await _traceService.CreateTraceRecordAsync(traceRecord);

                return Ok(new
                {
                    TraceId = created.TraceId,
                    TraceNumber = created.TraceNumber,
                    Message = "Trace record created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trace record");
                return StatusCode(500, new { Error = "An error occurred while creating trace record" });
            }
        }

        // POST: api/testTrace/createTakeoff
        [HttpPost("createTakeoff")]
        public async Task<IActionResult> CreateTestTakeoff([FromBody] CreateTakeoffRequest request)
        {
            // Validate input
            if (request == null || request.TraceId == Guid.Empty)
            {
                return BadRequest("Invalid request data");
            }

            try
            {
                // Get the trace record
                var trace = await _context.TraceRecords
                    .FirstOrDefaultAsync(t => t.TraceId == request.TraceId);

                if (trace == null)
                {
                    return NotFound("Trace record not found");
                }

                // Create takeoff
                var takeoff = await _traceService.CreateTakeoffFromPdfAsync(
                    trace.Id,
                    request.PdfUrl ?? "test.pdf",
                    request.DrawingId);

                return Ok(new
                {
                    TakeoffId = takeoff.Id,
                    TraceRecordId = takeoff.TraceRecordId,
                    Message = "Takeoff created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating takeoff");
                return StatusCode(500, new { Error = "An error occurred while creating takeoff" });
            }
        }

        // POST: api/testTrace/addMeasurement
        [HttpPost("addMeasurement")]
        public async Task<IActionResult> AddTestMeasurement([FromBody] AddMeasurementRequest request)
        {
            // Validate input
            if (request == null || request.TakeoffId <= 0 || request.Value <= 0 || string.IsNullOrEmpty(request.Unit))
            {
                return BadRequest("Invalid measurement data");
            }

            try
            {
                var measurement = new TraceTakeoffMeasurement
                {
                    MeasurementType = request.MeasurementType,
                    Value = request.Value,
                    Unit = request.Unit,
                    Description = request.Description,
                    CatalogueItemId = request.CatalogueItemId,
                    PageNumber = request.PageNumber ?? 1,
                    Coordinates = request.Coordinates
                };

                var added = await _traceService.AddMeasurementAsync(request.TakeoffId, measurement);

                // Calculate weight if catalogue item is linked
                if (request.CatalogueItemId.HasValue)
                {
                    added.CalculatedWeight = await _traceService.CalculateWeightFromCatalogueAsync(
                        request.CatalogueItemId.Value,
                        request.Value,
                        request.Unit);

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    MeasurementId = added.Id,
                    CalculatedWeight = added.CalculatedWeight,
                    Message = "Measurement added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding measurement");
                return StatusCode(500, new { Error = "An error occurred while adding measurement" });
            }
        }

        // GET: api/testTrace/bom/{takeoffId}
        [HttpGet("bom/{takeoffId}")]
        public async Task<IActionResult> GenerateBOM(int takeoffId)
        {
            try
            {
                var bom = await _traceService.GenerateBOMFromTakeoffAsync(takeoffId);

                return Ok(bom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating BOM");
                return StatusCode(500, new { Error = "An error occurred while generating BOM" });
            }
        }

        // GET: api/testTrace/searchCatalogue
        [HttpGet("searchCatalogue")]
        public async Task<IActionResult> SearchCatalogue([FromQuery] string search, [FromQuery] string category = null)
        {
            try
            {
                var items = await _traceService.SuggestCatalogueItemsAsync(search, category);

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching catalogue");
                return StatusCode(500, new { Error = "An error occurred while searching catalogue" });
            }
        }

        // POST: api/testTrace/calculateWeight
        [HttpPost("calculateWeight")]
        public async Task<IActionResult> CalculateWeight([FromBody] CalculateWeightRequest request)
        {
            try
            {
                var weight = await _traceService.CalculateWeightFromCatalogueAsync(
                    request.CatalogueItemId,
                    request.Quantity,
                    request.Unit);

                var item = await _context.CatalogueItems.FindAsync(request.CatalogueItemId);

                return Ok(new
                {
                    CatalogueItemId = request.CatalogueItemId,
                    ItemCode = item?.ItemCode,
                    Description = item?.Description,
                    Quantity = request.Quantity,
                    Unit = request.Unit,
                    CalculatedWeight = weight,
                    Message = $"Weight calculated: {weight:F2} kg"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating weight");
                return StatusCode(500, new { Error = "An error occurred while calculating weight" });
            }
        }
    }

    // Request DTOs
    public class CreateTakeoffRequest
    {
        public Guid TraceId { get; set; }
        public string PdfUrl { get; set; }
        public int? DrawingId { get; set; }
    }

    public class AddMeasurementRequest
    {
        public int TakeoffId { get; set; }
        public string MeasurementType { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
        public int? CatalogueItemId { get; set; }
        public int? PageNumber { get; set; }
        public string Coordinates { get; set; }
    }

    public class CalculateWeightRequest
    {
        public int CatalogueItemId { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}