using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.ViewModels;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FabOS.WebServer.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TakeoffController : ControllerBase
    {
        private readonly ITakeoffService _takeoffService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TakeoffController> _logger;

        public TakeoffController(
            ITakeoffService takeoffService,
            ApplicationDbContext context,
            ILogger<TakeoffController> logger)
        {
            _takeoffService = takeoffService;
            _context = context;
            _logger = logger;
        }

        // POST: api/takeoff/calculate
        [HttpPost("calculate")]
        public async Task<ActionResult<TakeoffCalculationResult>> CalculateTakeoff([FromBody] TakeoffCalculationRequest request)
        {
            try
            {
                var result = new TakeoffCalculationResult
                {
                    TakeoffId = request.TakeoffId,
                    Measurements = new List<MeasurementCalculation>()
                };

                foreach (var measurementId in request.MeasurementIds)
                {
                    var measurement = await _context.TraceTakeoffMeasurements
                        .Include(m => m.CatalogueItem)
                        .FirstOrDefaultAsync(m => m.Id == measurementId);

                    if (measurement != null)
                    {
                        var calc = new MeasurementCalculation
                        {
                            MeasurementId = measurementId,
                            Type = measurement.MeasurementType,
                            Value = measurement.Value,
                            Unit = measurement.Unit
                        };

                        // Calculate weight if linked to catalogue item
                        if (measurement.CatalogueItem != null)
                        {
                            var weightCalc = await _takeoffService.CalculateWeightFromCatalogueAsync(
                                measurement.CatalogueItem.Id,
                                measurement.Value,
                                measurement.Unit);

                            if (weightCalc != null)
                            {
                                calc.Weight = new WeightCalculationDto
                                {
                                    Weight = weightCalc.Weight,
                                    Unit = weightCalc.WeightUnit,
                                    UnitWeight = weightCalc.UnitWeight,
                                    CalculationMethod = weightCalc.CalculationMethod
                                };
                            }

                            calc.ItemCode = measurement.CatalogueItem.ItemCode;
                            calc.Description = measurement.CatalogueItem.Description;
                        }

                        result.Measurements.Add(calc);
                        result.TotalQuantity += measurement.Value;
                        result.TotalWeight += calc.Weight?.Weight ?? 0;
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating takeoff: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/takeoff/{id}/bom
        [HttpGet("{takeoffId:int}/bom")]
        public async Task<ActionResult<BillOfMaterials>> GenerateBOM(int takeoffId)
        {
            try
            {
                var bom = await _takeoffService.GenerateBOMAsync(takeoffId);
                return Ok(bom);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating BOM: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/takeoff/export
        [HttpPost("export")]
        public async Task<IActionResult> ExportTakeoff([FromBody] ExportRequest request)
        {
            try
            {
                byte[] fileContent;
                string contentType;
                string fileName;

                switch (request.Format.ToLower())
                {
                    case "excel":
                        fileContent = await _takeoffService.ExportToExcelAsync(request.TakeoffId);
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        fileName = $"takeoff_{request.TakeoffId}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                        break;

                    case "csv":
                        fileContent = await _takeoffService.ExportToCsvAsync(request.TakeoffId);
                        contentType = "text/csv";
                        fileName = $"takeoff_{request.TakeoffId}_{DateTime.UtcNow:yyyyMMdd}.csv";
                        break;

                    case "pdf":
                        fileContent = await _takeoffService.ExportToPdfReportAsync(request.TakeoffId);
                        contentType = "application/pdf";
                        fileName = $"takeoff_report_{request.TakeoffId}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                        break;

                    case "json":
                        var jsonContent = await _takeoffService.ExportToJsonAsync(request.TakeoffId);
                        fileContent = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                        contentType = "application/json";
                        fileName = $"takeoff_{request.TakeoffId}_{DateTime.UtcNow:yyyyMMdd}.json";
                        break;

                    default:
                        return BadRequest($"Unsupported export format: {request.Format}");
                }

                return File(fileContent, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error exporting takeoff: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/takeoff/catalogue-match
        [HttpGet("catalogue-match")]
        public async Task<ActionResult<List<CatalogueSuggestion>>> MatchCatalogueItems(
            [FromQuery] string description,
            [FromQuery] decimal? dimension1,
            [FromQuery] decimal? dimension2)
        {
            try
            {
                var matchedItem = await _takeoffService.AutoMatchCatalogueItemAsync(
                    description,
                    dimension1,
                    dimension2);

                if (matchedItem == null)
                {
                    return Ok(new List<CatalogueSuggestion>());
                }

                var suggestions = new List<CatalogueSuggestion>
                {
                    new CatalogueSuggestion
                    {
                        Item = matchedItem,
                        ConfidenceScore = 0.95m,
                        MatchReason = "Matched based on description and dimensions"
                    }
                };

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error matching catalogue items: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/takeoff/save-session
        [HttpPost("save-session")]
        public async Task<ActionResult<TraceTakeoff>> SaveTakeoffSession([FromBody] TraceTakeoff takeoff)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updated = await _takeoffService.UpdateTakeoffSessionAsync(takeoff);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving takeoff session: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/takeoff/{projectId}/summary
        [HttpGet("project/{projectId:int}/summary")]
        public async Task<ActionResult<TakeoffProjectSummary>> GetProjectTakeoffSummary(int projectId)
        {
            try
            {
                var takeoffs = await _takeoffService.GetTakeoffsByProjectAsync(projectId);

                var summary = new TakeoffProjectSummary
                {
                    ProjectId = projectId,
                    TotalTakeoffs = takeoffs.Count,
                    TotalMeasurements = 0,
                    TotalWeight = 0,
                    Categories = new Dictionary<string, int>()
                };

                foreach (var takeoff in takeoffs)
                {
                    var measurements = await _takeoffService.GetMeasurementsByTakeoffAsync(takeoff.Id);
                    summary.TotalMeasurements += measurements.Count;

                    foreach (var measurement in measurements)
                    {
                        if (measurement.CalculatedWeight.HasValue)
                        {
                            summary.TotalWeight += measurement.CalculatedWeight.Value;
                        }

                        if (!summary.Categories.ContainsKey(measurement.MeasurementType))
                        {
                            summary.Categories[measurement.MeasurementType] = 0;
                        }
                        summary.Categories[measurement.MeasurementType]++;
                    }
                }

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting project takeoff summary: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/takeoff/quantity
        [HttpPost("quantity")]
        public async Task<ActionResult<TakeoffQuantity>> CalculateQuantity([FromBody] QuantityCalculationRequest request)
        {
            try
            {
                TakeoffQuantity quantity;

                switch (request.MeasurementType.ToLower())
                {
                    case "linear":
                        quantity = await _takeoffService.CalculateLinearQuantityAsync(
                            request.MeasurementId,
                            request.TargetUnit);
                        break;

                    case "area":
                        quantity = await _takeoffService.CalculateAreaQuantityAsync(
                            request.MeasurementId,
                            request.TargetUnit);
                        break;

                    case "count":
                        quantity = await _takeoffService.CalculateCountQuantityAsync(
                            request.MeasurementId);
                        break;

                    case "volume":
                        quantity = await _takeoffService.CalculateVolumeQuantityAsync(
                            request.MeasurementId,
                            request.Thickness ?? 0,
                            request.TargetUnit);
                        break;

                    default:
                        return BadRequest($"Unknown measurement type: {request.MeasurementType}");
                }

                return Ok(quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating quantity: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/takeoff/validate
        [HttpPost("validate/{takeoffId:int}")]
        public async Task<ActionResult<ValidationResult>> ValidateTakeoff(int takeoffId)
        {
            try
            {
                var validationResult = await _takeoffService.ValidateMeasurementsAsync(takeoffId);
                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating takeoff: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/takeoff/purchase-requirements/{takeoffId}
        [HttpGet("purchase-requirements/{takeoffId:int}")]
        public async Task<ActionResult<List<PurchaseRequirement>>> GetPurchaseRequirements(int takeoffId)
        {
            try
            {
                var requirements = await _takeoffService.GeneratePurchaseRequirementsAsync(takeoffId);
                return Ok(requirements);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating purchase requirements: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/takeoff/extract-items
        [HttpPost("extract-items")]
        public async Task<ActionResult<List<ExtractedItem>>> ExtractItemsFromText([FromBody] TextExtractionRequest request)
        {
            try
            {
                var items = await _takeoffService.ExtractItemsFromPdfTextAsync(request.Text);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting items from text: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // DTOs
    public class TakeoffCalculationRequest
    {
        public int TakeoffId { get; set; }
        public List<int> MeasurementIds { get; set; }
    }

    public class TakeoffCalculationResult
    {
        public int TakeoffId { get; set; }
        public List<MeasurementCalculation> Measurements { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
    }

    public class MeasurementCalculation
    {
        public int MeasurementId { get; set; }
        public string Type { get; set; }
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public WeightCalculation Weight { get; set; }
    }

    public class ExportRequest
    {
        public int TakeoffId { get; set; }
        public string Format { get; set; } // excel, csv, pdf, json
    }

    public class TakeoffProjectSummary
    {
        public int ProjectId { get; set; }
        public int TotalTakeoffs { get; set; }
        public int TotalMeasurements { get; set; }
        public decimal TotalWeight { get; set; }
        public Dictionary<string, int> Categories { get; set; }
    }

    public class QuantityCalculationRequest
    {
        public int MeasurementId { get; set; }
        public string MeasurementType { get; set; }
        public string TargetUnit { get; set; }
        public decimal? Thickness { get; set; }
    }

    public class TextExtractionRequest
    {
        public string Text { get; set; }
    }
}