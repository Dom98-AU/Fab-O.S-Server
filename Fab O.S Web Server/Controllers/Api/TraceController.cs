using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class TraceController : ControllerBase
    {
        private readonly ITraceService _traceService;
        private readonly IPdfProcessingService _pdfProcessingService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TraceController> _logger;

        public TraceController(
            ITraceService traceService,
            IPdfProcessingService pdfProcessingService,
            ApplicationDbContext context,
            ILogger<TraceController> logger)
        {
            _traceService = traceService;
            _pdfProcessingService = pdfProcessingService;
            _context = context;
            _logger = logger;
        }

        // GET: api/trace/{traceId}
        [HttpGet("{traceId:guid}")]
        public async Task<ActionResult<TraceRecord>> GetTraceRecord(Guid traceId)
        {
            try
            {
                var traceRecord = await _traceService.GetTraceRecordAsync(traceId);
                if (traceRecord == null)
                {
                    return NotFound($"Trace record with ID {traceId} not found");
                }
                return Ok(traceRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting trace record: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/trace
        [HttpPost]
        public async Task<ActionResult<TraceRecord>> CreateTraceRecord([FromBody] TraceRecord traceRecord)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var created = await _traceService.CreateTraceRecordAsync(traceRecord);
                return CreatedAtAction(nameof(GetTraceRecord), new { traceId = created.TraceId }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating trace record: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/trace/upload-pdf
        [HttpPost("upload-pdf")]
        public async Task<ActionResult<TraceTakeoff>> UploadPdfForTakeoff([FromForm] PdfUploadDto uploadDto)
        {
            try
            {
                if (uploadDto.PdfFile == null || uploadDto.PdfFile.Length == 0)
                {
                    return BadRequest("No PDF file uploaded");
                }

                // Save PDF file to storage (simplified - you'd typically use Azure Blob Storage)
                var fileName = $"{Guid.NewGuid()}_{uploadDto.PdfFile.FileName}";
                var filePath = Path.Combine("wwwroot", "uploads", "pdfs", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadDto.PdfFile.CopyToAsync(stream);
                }

                var pdfUrl = $"/uploads/pdfs/{fileName}";

                // Create takeoff session
                var takeoff = await _traceService.CreateTakeoffFromPdfAsync(
                    uploadDto.TraceRecordId,
                    pdfUrl,
                    uploadDto.DrawingId);

                return Ok(takeoff);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading PDF: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/{id}/takeoffs
        [HttpGet("{traceRecordId:int}/takeoffs")]
        public async Task<ActionResult<List<TraceTakeoff>>> GetTakeoffsByTraceRecord(int traceRecordId)
        {
            try
            {
                var takeoffs = await _context.TraceTakeoffs
                    .Where(t => t.TraceRecordId == traceRecordId)
                    .Include(t => t.Measurements)
                    .ToListAsync();

                return Ok(takeoffs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting takeoffs: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/trace/{id}/measurements
        [HttpPost("{takeoffId:int}/measurements")]
        public async Task<ActionResult<TraceTakeoffMeasurement>> AddMeasurement(
            int takeoffId,
            [FromBody] TraceTakeoffMeasurement measurement)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                measurement.TraceTakeoffId = takeoffId;
                var created = await _traceService.AddMeasurementAsync(takeoffId, measurement);

                return Ok(created);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding measurement: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/{id}/measurements
        [HttpGet("{takeoffId:int}/measurements")]
        public async Task<ActionResult<List<TraceTakeoffMeasurement>>> GetMeasurements(int takeoffId)
        {
            try
            {
                var measurements = await _traceService.GetTakeoffMeasurementsAsync(takeoffId);
                return Ok(measurements);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting measurements: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/trace/{id}/link-catalogue-item
        [HttpPost("{measurementId:int}/link-catalogue-item")]
        public async Task<ActionResult<TraceTakeoffMeasurement>> LinkMeasurementToCatalogue(
            int measurementId,
            [FromBody] LinkCatalogueDto linkDto)
        {
            try
            {
                var updated = await _traceService.LinkMeasurementToCatalogueAsync(
                    measurementId,
                    linkDto.CatalogueItemId);

                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error linking to catalogue: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/{id}/materials
        [HttpGet("{traceRecordId:int}/materials")]
        public async Task<ActionResult<List<TraceMaterial>>> GetMaterials(int traceRecordId)
        {
            try
            {
                var materials = await _traceService.GetMaterialsByTraceAsync(traceRecordId);
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting materials: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/trace/material-receipt
        [HttpPost("material-receipt")]
        public async Task<ActionResult<TraceMaterial>> RecordMaterialReceipt([FromBody] TraceMaterial material)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var created = await _traceService.RecordMaterialReceiptAsync(material);
                return Ok(created);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error recording material receipt: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/forward/{traceId}
        [HttpGet("forward/{traceId:guid}")]
        public async Task<ActionResult<List<TraceRecord>>> TraceForward(Guid traceId)
        {
            try
            {
                var traces = await _traceService.TraceForwardAsync(traceId);
                return Ok(traces);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tracing forward: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/backward/{traceId}
        [HttpGet("backward/{traceId:guid}")]
        public async Task<ActionResult<List<TraceRecord>>> TraceBackward(Guid traceId)
        {
            try
            {
                var traces = await _traceService.TraceBackwardAsync(traceId);
                return Ok(traces);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tracing backward: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/report/{traceId}
        [HttpGet("report/{traceId:guid}")]
        public async Task<ActionResult<TraceReportDto>> GenerateTraceReport(Guid traceId)
        {
            try
            {
                var report = await _traceService.GenerateTraceReportAsync(traceId);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating trace report: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/trace/catalogue/suggest
        [HttpGet("catalogue/suggest")]
        public async Task<ActionResult<List<CatalogueItem>>> SuggestCatalogueItems(
            [FromQuery] string searchText,
            [FromQuery] string category = null)
        {
            try
            {
                var suggestions = await _traceService.SuggestCatalogueItemsAsync(searchText, category);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error suggesting catalogue items: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/trace/measurements/{id}
        [HttpDelete("measurements/{measurementId:int}")]
        public async Task<ActionResult> DeleteMeasurement(int measurementId)
        {
            try
            {
                var measurement = await _context.TraceTakeoffMeasurements.FindAsync(measurementId);
                if (measurement == null)
                {
                    return NotFound();
                }

                _context.TraceTakeoffMeasurements.Remove(measurement);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting measurement: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // DTOs
    public class PdfUploadDto
    {
        public int TraceRecordId { get; set; }
        public int? DrawingId { get; set; }
        public IFormFile PdfFile { get; set; }
    }

    public class LinkCatalogueDto
    {
        public int CatalogueItemId { get; set; }
    }

    public class TraceReportDto
    {
        public Guid TraceId { get; set; }
        public string TraceNumber { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }
        public List<TraceMaterial> Materials { get; set; }
        public List<TraceProcess> Processes { get; set; }
        public List<TraceDocument> Documents { get; set; }
        public string GenealogyTree { get; set; }
        public string Summary { get; set; }
    }
}