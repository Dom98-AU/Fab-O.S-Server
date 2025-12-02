using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for Equipment Manuals in the Asset module
/// </summary>
[ApiController]
[Route("api/assets/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class ManualsController : ControllerBase
{
    private readonly IEquipmentManualService _manualService;
    private readonly ILogger<ManualsController> _logger;

    public ManualsController(
        IEquipmentManualService manualService,
        ILogger<ManualsController> logger)
    {
        _manualService = manualService;
        _logger = logger;
    }

    #region CRUD Operations

    /// <summary>
    /// Get all manuals
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ManualListResponse>> GetManuals(
        [FromQuery] int companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? equipmentId = null,
        [FromQuery] string? type = null)
    {
        try
        {
            var manuals = await _manualService.GetManualsPagedAsync(
                companyId, page, pageSize, equipmentId, type);
            var totalCount = await _manualService.GetManualsCountAsync(
                companyId, equipmentId, type);

            var items = manuals.Select(m => MapToDto(m)).ToList();

            return Ok(new ManualListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manuals");
            return StatusCode(500, new { message = "Error retrieving manuals" });
        }
    }

    /// <summary>
    /// Get manuals for equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}")]
    public async Task<ActionResult<List<ManualDto>>> GetByEquipment(int equipmentId)
    {
        try
        {
            var manuals = await _manualService.GetManualsByEquipmentAsync(equipmentId);
            return Ok(manuals.Select(m => MapToDto(m)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manuals for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving manuals" });
        }
    }

    /// <summary>
    /// Get manual by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ManualDto>> GetManual(int id)
    {
        try
        {
            var manual = await _manualService.GetManualByIdAsync(id);
            if (manual == null)
                return NotFound(new { message = $"Manual with ID {id} not found" });

            return Ok(MapToDto(manual));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual {Id}", id);
            return StatusCode(500, new { message = "Error retrieving manual" });
        }
    }

    /// <summary>
    /// Create manual
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ManualDto>> CreateManual([FromBody] CreateManualRequest request)
    {
        try
        {
            var manual = new EquipmentManual
            {
                EquipmentId = request.EquipmentId,
                ManualType = request.ManualType,
                Title = request.Title,
                Description = request.Description,
                DocumentUrl = request.DocumentUrl,
                Version = request.Version,
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = request.ContentType
            };

            var created = await _manualService.CreateManualAsync(manual);

            return CreatedAtAction(nameof(GetManual), new { id = created.Id }, MapToDto(created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual");
            return StatusCode(500, new { message = "Error creating manual" });
        }
    }

    /// <summary>
    /// Update manual
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ManualDto>> UpdateManual(int id, [FromBody] UpdateManualRequest request)
    {
        try
        {
            var existing = await _manualService.GetManualByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Manual with ID {id} not found" });

            existing.ManualType = request.ManualType;
            existing.Title = request.Title;
            existing.Description = request.Description;
            existing.DocumentUrl = request.DocumentUrl;
            existing.Version = request.Version;
            existing.FileName = request.FileName;
            existing.FileSize = request.FileSize;
            existing.ContentType = request.ContentType;

            var updated = await _manualService.UpdateManualAsync(existing);

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating manual {Id}", id);
            return StatusCode(500, new { message = "Error updating manual" });
        }
    }

    /// <summary>
    /// Delete manual
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteManual(int id)
    {
        try
        {
            var result = await _manualService.DeleteManualAsync(id);
            if (!result)
                return NotFound(new { message = $"Manual with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting manual {Id}", id);
            return StatusCode(500, new { message = "Error deleting manual" });
        }
    }

    #endregion

    #region Type Operations

    /// <summary>
    /// Get manual types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<List<string>>> GetManualTypes([FromQuery] int companyId)
    {
        try
        {
            var types = await _manualService.GetManualTypesAsync(companyId);
            return Ok(types.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual types");
            return StatusCode(500, new { message = "Error retrieving types" });
        }
    }

    /// <summary>
    /// Get manual types for equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/types")]
    public async Task<ActionResult<List<string>>> GetManualTypesForEquipment(int equipmentId)
    {
        try
        {
            var types = await _manualService.GetManualTypesForEquipmentAsync(equipmentId);
            return Ok(types.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual types for equipment");
            return StatusCode(500, new { message = "Error retrieving types" });
        }
    }

    /// <summary>
    /// Get manuals by type for equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/type/{manualType}")]
    public async Task<ActionResult<List<ManualDto>>> GetByType(int equipmentId, string manualType)
    {
        try
        {
            var manuals = await _manualService.GetManualsByTypeAsync(equipmentId, manualType);
            return Ok(manuals.Select(m => MapToDto(m)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manuals by type");
            return StatusCode(500, new { message = "Error retrieving manuals" });
        }
    }

    #endregion

    #region Search Operations

    /// <summary>
    /// Search manuals
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<ManualDto>>> SearchManuals(
        [FromQuery] int companyId,
        [FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required" });

            var manuals = await _manualService.SearchManualsAsync(companyId, term);
            return Ok(manuals.Select(m => MapToDto(m)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching manuals");
            return StatusCode(500, new { message = "Error searching manuals" });
        }
    }

    /// <summary>
    /// Search manuals by title
    /// </summary>
    [HttpGet("search/title")]
    public async Task<ActionResult<List<ManualDto>>> SearchByTitle(
        [FromQuery] int companyId,
        [FromQuery] string title)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "Title search term is required" });

            var manuals = await _manualService.SearchManualsByTitleAsync(companyId, title);
            return Ok(manuals.Select(m => MapToDto(m)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching manuals by title");
            return StatusCode(500, new { message = "Error searching manuals" });
        }
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Copy manuals to another equipment
    /// </summary>
    [HttpPost("equipment/{sourceEquipmentId:int}/copy/{targetEquipmentId:int}")]
    public async Task<ActionResult> CopyManuals(int sourceEquipmentId, int targetEquipmentId)
    {
        try
        {
            var result = await _manualService.CopyManualsToEquipmentAsync(sourceEquipmentId, targetEquipmentId);
            if (!result)
                return BadRequest(new { message = "Failed to copy manuals" });

            return Ok(new { message = "Manuals copied successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying manuals");
            return StatusCode(500, new { message = "Error copying manuals" });
        }
    }

    /// <summary>
    /// Delete all manuals for equipment
    /// </summary>
    [HttpDelete("equipment/{equipmentId:int}/all")]
    public async Task<ActionResult> DeleteAllForEquipment(int equipmentId)
    {
        try
        {
            var result = await _manualService.DeleteAllManualsForEquipmentAsync(equipmentId);
            if (!result)
                return BadRequest(new { message = "Failed to delete manuals" });

            return Ok(new { message = "All manuals deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting manuals for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error deleting manuals" });
        }
    }

    #endregion

    #region Document Operations

    /// <summary>
    /// Update manual document URL
    /// </summary>
    [HttpPatch("{id:int}/document")]
    public async Task<ActionResult> UpdateDocument(
        int id,
        [FromBody] UpdateManualDocumentRequest request)
    {
        try
        {
            var result = await _manualService.UpdateManualDocumentAsync(
                id,
                request.DocumentUrl,
                request.FileName,
                request.FileSize,
                request.ContentType);

            if (!result)
                return NotFound(new { message = $"Manual with ID {id} not found" });

            return Ok(new { message = "Document updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document for manual {Id}", id);
            return StatusCode(500, new { message = "Error updating document" });
        }
    }

    /// <summary>
    /// Get manual document URL
    /// </summary>
    [HttpGet("{id:int}/document-url")]
    public async Task<ActionResult<string>> GetDocumentUrl(int id)
    {
        try
        {
            var url = await _manualService.GetManualDocumentUrlAsync(id);
            if (url == null)
                return NotFound(new { message = $"Manual with ID {id} not found or has no document" });

            return Ok(new { documentUrl = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document URL for manual {Id}", id);
            return StatusCode(500, new { message = "Error retrieving document URL" });
        }
    }

    /// <summary>
    /// Update manual version
    /// </summary>
    [HttpPatch("{id:int}/version")]
    public async Task<ActionResult> UpdateVersion(int id, [FromBody] string version)
    {
        try
        {
            var result = await _manualService.UpdateManualVersionAsync(id, version);
            if (!result)
                return NotFound(new { message = $"Manual with ID {id} not found" });

            return Ok(new { message = "Version updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating version for manual {Id}", id);
            return StatusCode(500, new { message = "Error updating version" });
        }
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Get manual counts by equipment
    /// </summary>
    [HttpGet("analytics/by-equipment")]
    public async Task<ActionResult<Dictionary<int, int>>> GetCountByEquipment([FromQuery] int companyId)
    {
        try
        {
            var counts = await _manualService.GetManualCountsByEquipmentAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual counts by equipment");
            return StatusCode(500, new { message = "Error retrieving counts" });
        }
    }

    /// <summary>
    /// Get manual counts by type
    /// </summary>
    [HttpGet("analytics/by-type")]
    public async Task<ActionResult<Dictionary<string, int>>> GetCountByType([FromQuery] int companyId)
    {
        try
        {
            var counts = await _manualService.GetManualCountsByTypeAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting manual counts by type");
            return StatusCode(500, new { message = "Error retrieving counts" });
        }
    }

    /// <summary>
    /// Get recent manuals
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<List<ManualDto>>> GetRecentManuals(
        [FromQuery] int companyId,
        [FromQuery] int count = 10)
    {
        try
        {
            var manuals = await _manualService.GetRecentManualsAsync(companyId, count);
            return Ok(manuals.Select(m => MapToDto(m)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent manuals");
            return StatusCode(500, new { message = "Error retrieving manuals" });
        }
    }

    /// <summary>
    /// Get equipment without manuals
    /// </summary>
    [HttpGet("equipment-without-manuals")]
    public async Task<ActionResult<List<object>>> GetEquipmentWithoutManuals([FromQuery] int companyId)
    {
        try
        {
            var equipment = await _manualService.GetEquipmentWithoutManualsAsync(companyId);
            return Ok(equipment.Select(e => new
            {
                e.Id,
                e.EquipmentCode,
                e.Name,
                Category = e.EquipmentCategory?.Name,
                Type = e.EquipmentType?.Name
            }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting equipment without manuals");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    /// <summary>
    /// Get total storage size
    /// </summary>
    [HttpGet("analytics/storage")]
    public async Task<ActionResult<object>> GetStorageSize([FromQuery] int companyId)
    {
        try
        {
            var totalBytes = await _manualService.GetTotalManualStorageSizeAsync(companyId);
            var totalMB = totalBytes / (1024.0 * 1024.0);

            return Ok(new
            {
                totalBytes,
                totalMB = Math.Round(totalMB, 2),
                formatted = FormatFileSize(totalBytes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage size");
            return StatusCode(500, new { message = "Error retrieving storage size" });
        }
    }

    #endregion

    #region Helper Methods

    private static ManualDto MapToDto(EquipmentManual m)
    {
        return new ManualDto
        {
            Id = m.Id,
            EquipmentId = m.EquipmentId,
            EquipmentCode = m.Equipment?.EquipmentCode,
            EquipmentName = m.Equipment?.Name,
            ManualType = m.ManualType,
            Title = m.Title,
            Description = m.Description,
            DocumentUrl = m.DocumentUrl,
            Version = m.Version,
            FileName = m.FileName,
            FileSize = m.FileSize,
            ContentType = m.ContentType,
            UploadedDate = m.UploadedDate,
            UploadedBy = m.UploadedBy,
            LastModified = m.LastModified
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    #endregion
}

public class UpdateManualDocumentRequest
{
    public string DocumentUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
}
