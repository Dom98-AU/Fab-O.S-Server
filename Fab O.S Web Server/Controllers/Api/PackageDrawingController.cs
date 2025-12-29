using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Hubs;
using FabOS.WebServer.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Controllers.Api;

[ApiController]
[Route("api/packagedrawings")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class PackageDrawingController : ControllerBase
{
    private readonly IPackageDrawingService _drawingService;
    private readonly ILogger<PackageDrawingController> _logger;
    private readonly IMeasurementHubService _measurementHubService;
    private readonly ApplicationDbContext _context;

    public PackageDrawingController(
        IPackageDrawingService drawingService,
        ILogger<PackageDrawingController> logger,
        IMeasurementHubService measurementHubService,
        ApplicationDbContext context)
    {
        _drawingService = drawingService;
        _logger = logger;
        _measurementHubService = measurementHubService;
        _context = context;
    }

    /// <summary>
    /// Get drawing information
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDrawing(int id)
    {
        try
        {
            // Get drawing directly from context to avoid navigation property serialization issues
            var drawing = await _context.PackageDrawings
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    d.Id,
                    d.DrawingNumber,
                    d.DrawingTitle,
                    d.InstantJson,
                    d.InstantJsonLastUpdated
                })
                .FirstOrDefaultAsync();

            if (drawing == null)
            {
                return NotFound();
            }

            return Ok(drawing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing {DrawingId}", id);
            return StatusCode(500, "An error occurred while retrieving the drawing");
        }
    }

    /// <summary>
    /// Get PDF content for a drawing (for viewer)
    /// </summary>
    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetDrawingContent(int id)
    {
        try
        {
            var stream = await _drawingService.GetDrawingContentAsync(id);
            return File(stream, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Drawing {DrawingId} not found", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing content for {DrawingId}", id);
            return StatusCode(500, "An error occurred while retrieving the file");
        }
    }

    /// <summary>
    /// Get all drawings for a package
    /// </summary>
    [HttpGet("package/{packageId}")]
    public async Task<IActionResult> GetPackageDrawings(int packageId)
    {
        try
        {
            var drawings = await _drawingService.GetPackageDrawingsAsync(packageId);
            return Ok(drawings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawings for package {PackageId}", packageId);
            return StatusCode(500, "An error occurred while retrieving the drawings");
        }
    }

    /// <summary>
    /// Upload a new drawing
    /// </summary>
    [HttpPost("package/{packageId}/upload")]
    public async Task<IActionResult> UploadDrawing(
        int packageId,
        [FromForm] IFormFile file,
        [FromForm] string drawingNumber,
        [FromForm] string drawingTitle)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        if (string.IsNullOrWhiteSpace(drawingNumber))
        {
            return BadRequest("Drawing number is required");
        }

        if (string.IsNullOrWhiteSpace(drawingTitle))
        {
            return BadRequest("Drawing title is required");
        }

        // Check file type
        var allowedExtensions = new[] { ".pdf" };
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest("Only PDF files are allowed");
        }

        // Check file size (250MB max)
        if (file.Length > 250 * 1024 * 1024)
        {
            return BadRequest("File size exceeds maximum allowed size of 250MB");
        }

        try
        {
            // Check if drawing number already exists
            if (await _drawingService.DrawingNumberExistsAsync(packageId, drawingNumber))
            {
                return BadRequest($"Drawing number {drawingNumber} already exists for this package");
            }

            using var stream = file.OpenReadStream();

            // TODO: Get actual user ID from authentication context
            var uploadedBy = 1;

            var drawing = await _drawingService.UploadDrawingAsync(
                packageId,
                stream,
                file.FileName,
                drawingNumber,
                drawingTitle,
                uploadedBy
            );

            return Ok(drawing);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation uploading drawing for package {PackageId}", packageId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading drawing for package {PackageId}", packageId);
            return StatusCode(500, "An error occurred while uploading the file");
        }
    }

    /// <summary>
    /// Delete a drawing (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDrawing(int id)
    {
        try
        {
            var result = await _drawingService.DeleteDrawingAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting drawing {DrawingId}", id);
            return StatusCode(500, "An error occurred while deleting the drawing");
        }
    }

    /// <summary>
    /// Get SharePoint preview URL for a drawing
    /// </summary>
    [HttpGet("{id}/preview-url")]
    public async Task<IActionResult> GetPreviewUrl(int id)
    {
        try
        {
            var url = await _drawingService.GetDrawingPreviewUrlAsync(id);
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Drawing {DrawingId} not found", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preview URL for drawing {DrawingId}", id);
            return StatusCode(500, "An error occurred while getting the preview URL");
        }
    }

    /// <summary>
    /// Get PDF content from SharePoint (with embedded measurements and scales)
    /// </summary>
    [HttpGet("{id}/sharepoint-content")]
    public async Task<IActionResult> GetSharePointPdfContent(int id)
    {
        try
        {
            var stream = await _drawingService.GetSharePointPdfContentAsync(id);
            return File(stream, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Drawing {DrawingId} not found or not in SharePoint", id);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SharePoint PDF content for {DrawingId}", id);
            return StatusCode(500, "An error occurred while retrieving the file from SharePoint");
        }
    }

    /// <summary>
    /// Save Instant JSON for autosave functionality
    /// Called automatically from JavaScript when annotations change
    /// </summary>
    [HttpPost("{id}/instant-json")]
    public async Task<IActionResult> SaveInstantJson(int id, [FromBody] SaveInstantJsonRequest request)
    {
        try
        {
            _logger.LogInformation("[API] SaveInstantJson called for drawing {DrawingId}, JSON length: {Length}",
                id, request.InstantJson?.Length ?? 0);

            // Get the drawing
            var drawing = await _context.PackageDrawings.FindAsync(id);
            if (drawing == null)
            {
                return NotFound(new { message = "Drawing not found" });
            }

            // Update Instant JSON
            drawing.InstantJson = request.InstantJson;
            drawing.InstantJsonLastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("[API] ✓ Instant JSON saved for drawing {DrawingId}", id);

            // Broadcast SignalR event to notify other tabs
            await _measurementHubService.NotifyInstantJsonUpdatedAsync(id);

            return Ok(new { success = true, message = "Instant JSON saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Error saving Instant JSON for drawing {DrawingId}", id);
            return StatusCode(500, new { message = "An error occurred while saving Instant JSON" });
        }
    }
}

public class SaveInstantJsonRequest
{
    public string? InstantJson { get; set; }
}