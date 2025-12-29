using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for managing label templates and generating labels
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer")]
[ApiController]
[Route("api/assets/label-templates")]
[Produces("application/json")]
public class LabelTemplatesController : ControllerBase
{
    private readonly ILabelTemplateService _templateService;
    private readonly ILabelPrintingService _printingService;
    private readonly ILogger<LabelTemplatesController> _logger;

    public LabelTemplatesController(
        ILabelTemplateService templateService,
        ILabelPrintingService printingService,
        ILogger<LabelTemplatesController> logger)
    {
        _templateService = templateService;
        _printingService = printingService;
        _logger = logger;
    }

    #region Helper Methods

    private int GetCompanyId()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        if (int.TryParse(companyIdClaim, out int companyId))
            return companyId;

        // Default to company 1 if not found in claims
        return 1;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;

        return 0;
    }

    #endregion

    #region CRUD Endpoints

    /// <summary>
    /// Get all templates for company (includes system templates)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<LabelTemplateListResponse>> GetAll(
        [FromQuery] string? entityType = null,
        [FromQuery] bool includeSystem = true)
    {
        try
        {
            var companyId = GetCompanyId();
            var templates = await _templateService.GetAllForCompanyAsync(companyId, entityType, includeSystem);

            return Ok(new LabelTemplateListResponse
            {
                Items = templates.Select(MapToDto).ToList(),
                TotalCount = templates.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting label templates");
            return StatusCode(500, new { message = "Error retrieving templates" });
        }
    }

    /// <summary>
    /// Get system templates only
    /// </summary>
    [HttpGet("system")]
    public async Task<ActionResult<IEnumerable<LabelTemplateResponseDto>>> GetSystemTemplates(
        [FromQuery] string? entityType = null)
    {
        try
        {
            var templates = await _templateService.GetSystemTemplatesAsync(entityType);
            return Ok(templates.Select(MapToDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system templates");
            return StatusCode(500, new { message = "Error retrieving system templates" });
        }
    }

    /// <summary>
    /// Get template by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LabelTemplateResponseDto>> GetById(int id)
    {
        try
        {
            var template = await _templateService.GetByIdAsync(id);
            if (template == null)
                return NotFound(new { message = $"Template with ID {id} not found" });

            return Ok(MapToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {Id}", id);
            return StatusCode(500, new { message = "Error retrieving template" });
        }
    }

    /// <summary>
    /// Create a new label template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LabelTemplateResponseDto>> Create([FromBody] CreateLabelTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();

            var template = await _templateService.CreateAsync(companyId, request, userId);

            _logger.LogInformation("Created label template '{Name}' (ID: {Id}) for company {CompanyId}",
                template.Name, template.Id, companyId);

            return CreatedAtAction(nameof(GetById), new { id = template.Id }, MapToDto(template));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating label template");
            return StatusCode(500, new { message = "Error creating template" });
        }
    }

    /// <summary>
    /// Update an existing label template
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<LabelTemplateResponseDto>> Update(int id, [FromBody] UpdateLabelTemplateRequest request)
    {
        try
        {
            var userId = GetUserId();
            var template = await _templateService.UpdateAsync(id, request, userId);

            return Ok(MapToDto(template));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {Id}", id);
            return StatusCode(500, new { message = "Error updating template" });
        }
    }

    /// <summary>
    /// Delete a label template (soft delete, cannot delete system templates)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _templateService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = $"Template with ID {id} not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id}", id);
            return StatusCode(500, new { message = "Error deleting template" });
        }
    }

    #endregion

    #region Default Template Management

    /// <summary>
    /// Set a template as default for its entity type
    /// </summary>
    [HttpPost("{id:int}/set-default")]
    public async Task<IActionResult> SetAsDefault(int id)
    {
        try
        {
            var companyId = GetCompanyId();
            await _templateService.SetAsDefaultAsync(id, companyId);

            return Ok(new { message = "Template set as default successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting template {Id} as default", id);
            return StatusCode(500, new { message = "Error setting default template" });
        }
    }

    /// <summary>
    /// Get default template for entity type
    /// </summary>
    [HttpGet("default/{entityType}")]
    public async Task<ActionResult<LabelTemplateResponseDto>> GetDefault(string entityType)
    {
        try
        {
            var companyId = GetCompanyId();
            var template = await _templateService.GetDefaultTemplateAsync(companyId, entityType);

            if (template == null)
                return NotFound(new { message = $"No default template found for entity type '{entityType}'" });

            return Ok(MapToDto(template));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default template for {EntityType}", entityType);
            return StatusCode(500, new { message = "Error retrieving default template" });
        }
    }

    #endregion

    #region Preset Sizes

    /// <summary>
    /// Get available preset label sizes
    /// </summary>
    [HttpGet("preset-sizes")]
    public ActionResult<IEnumerable<LabelSizePreset>> GetPresetSizes()
    {
        var presets = _templateService.GetPresetSizes();
        return Ok(presets);
    }

    #endregion

    #region Label Generation

    /// <summary>
    /// Generate labels for equipment
    /// </summary>
    [HttpPost("generate/equipment")]
    public async Task<ActionResult<LabelGenerationResult>> GenerateEquipmentLabels(
        [FromBody] GenerateLabelsWithTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();

            // Resolve the template
            var template = await _templateService.ResolveTemplateAsync(
                companyId,
                request.TemplateId,
                request.TemplateName,
                "Equipment");

            // Generate labels
            var result = await _printingService.GenerateEquipmentLabelsAsync(
                companyId,
                request.EntityIds,
                template);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating equipment labels");
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    /// <summary>
    /// Generate labels for equipment kits
    /// </summary>
    [HttpPost("generate/kits")]
    public async Task<ActionResult<LabelGenerationResult>> GenerateKitLabels(
        [FromBody] GenerateLabelsWithTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();

            // Resolve the template
            var template = await _templateService.ResolveTemplateAsync(
                companyId,
                request.TemplateId,
                request.TemplateName,
                "Kit");

            // Generate labels
            var result = await _printingService.GenerateKitLabelsAsync(
                companyId,
                request.EntityIds,
                template);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating kit labels");
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    /// <summary>
    /// Generate labels for locations
    /// </summary>
    [HttpPost("generate/locations")]
    public async Task<ActionResult<LabelGenerationResult>> GenerateLocationLabels(
        [FromBody] GenerateLabelsWithTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();

            // Resolve the template
            var template = await _templateService.ResolveTemplateAsync(
                companyId,
                request.TemplateId,
                request.TemplateName,
                "Location");

            // Generate labels
            var result = await _printingService.GenerateLocationLabelsAsync(
                companyId,
                request.EntityIds,
                template);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating location labels");
            return StatusCode(500, new { message = "Error generating labels" });
        }
    }

    #endregion

    #region Mapping

    private static LabelTemplateResponseDto MapToDto(LabelTemplate template)
    {
        return new LabelTemplateResponseDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Name = template.Name,
            Description = template.Description,
            EntityType = template.EntityType,
            WidthMm = template.WidthMm,
            HeightMm = template.HeightMm,
            IncludeQRCode = template.IncludeQRCode,
            QRCodePixelsPerModule = template.QRCodePixelsPerModule,
            IncludeCode = template.IncludeCode,
            IncludeName = template.IncludeName,
            IncludeCategory = template.IncludeCategory,
            IncludeLocation = template.IncludeLocation,
            IncludeSerialNumber = template.IncludeSerialNumber,
            IncludeServiceDate = template.IncludeServiceDate,
            IncludeContactInfo = template.IncludeContactInfo,
            PrimaryFontSize = template.PrimaryFontSize,
            SecondaryFontSize = template.SecondaryFontSize,
            MarginMm = template.MarginMm,
            IsSystemTemplate = template.IsSystemTemplate,
            IsDefault = template.IsDefault,
            CreatedDate = template.CreatedDate,
            CreatedByUserId = template.CreatedByUserId,
            LastModified = template.LastModified,
            LastModifiedByUserId = template.LastModifiedByUserId
        };
    }

    #endregion
}
