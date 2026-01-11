using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api.Forms;

/// <summary>
/// API Controller for managing Form Templates.
/// Base endpoint: /api/{tenantSlug}/forms/templates
/// </summary>
[Route("api/{tenantSlug}/forms/templates")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class FormTemplatesController : ControllerBase
{
    private readonly IFormTemplateService _templateService;
    private readonly ILogger<FormTemplatesController> _logger;

    public FormTemplatesController(
        IFormTemplateService templateService,
        ILogger<FormTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Get all form templates for the company
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<FormTemplateListResponse>> GetTemplates(
        string tenantSlug,
        [FromQuery] FormModuleContext? module = null,
        [FromQuery] bool includeSystem = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var result = await _templateService.GetTemplatesAsync(
                companyId.Value, module, includeSystem, page, pageSize, search);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form templates for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FormTemplateDto>> GetTemplate(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _templateService.GetTemplateWithFieldsAsync(id, companyId.Value);
            if (template == null)
                return NotFound($"Template {id} not found");

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form template {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new form template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FormTemplateDto>> CreateTemplate(
        string tenantSlug,
        [FromBody] CreateFormTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _templateService.CreateTemplateAsync(
                request, companyId.Value, userId.Value);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { tenantSlug, id = template.Id },
                template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form template for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing form template
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FormTemplateDto>> UpdateTemplate(
        string tenantSlug,
        int id,
        [FromBody] UpdateFormTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _templateService.UpdateTemplateAsync(
                id, request, companyId.Value, userId.Value);

            return Ok(template);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form template {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a form template
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTemplate(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var deleted = await _templateService.DeleteTemplateAsync(id, companyId.Value);
            if (!deleted)
                return NotFound($"Template {id} not found");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form template {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Duplicate a form template
    /// </summary>
    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<FormTemplateDto>> DuplicateTemplate(
        string tenantSlug,
        int id,
        [FromBody] DuplicateTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _templateService.DuplicateTemplateAsync(
                id, request.NewName, companyId.Value, userId.Value);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { tenantSlug, id = template.Id },
                template);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating form template {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Publish a form template
    /// </summary>
    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<FormTemplateDto>> PublishTemplate(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _templateService.PublishTemplateAsync(id, companyId.Value, userId.Value);
            return Ok(template);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing form template {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Unpublish a form template
    /// </summary>
    [HttpPost("{id:int}/unpublish")]
    public async Task<ActionResult<FormTemplateDto>> UnpublishTemplate(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _templateService.UnpublishTemplateAsync(id, companyId.Value, userId.Value);
            return Ok(template);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing form template {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Set template as company default
    /// </summary>
    [HttpPost("{id:int}/set-default")]
    public async Task<ActionResult<FormTemplateDto>> SetAsDefault(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _templateService.SetAsDefaultAsync(id, companyId.Value, userId.Value);
            return Ok(template);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting form template {Id} as default for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get default template for a module
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<FormTemplateDto>> GetDefaultTemplate(
        string tenantSlug,
        [FromQuery] FormModuleContext module,
        [FromQuery] string? formType = null)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _templateService.GetDefaultTemplateAsync(
                companyId.Value, module, formType);

            if (template == null)
                return NotFound("No default template found");

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default form template for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get form types used in the company
    /// </summary>
    [HttpGet("form-types")]
    public async Task<ActionResult<List<string>>> GetFormTypes(
        string tenantSlug,
        [FromQuery] FormModuleContext? module = null)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var types = await _templateService.GetFormTypesAsync(companyId.Value, module);
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form types for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available page size presets
    /// </summary>
    [HttpGet("page-sizes")]
    public ActionResult<IEnumerable<FormPageSizePreset>> GetPageSizes()
    {
        var presets = _templateService.GetPageSizePresets();
        return Ok(presets);
    }

    /// <summary>
    /// Add a field to a template
    /// </summary>
    [HttpPost("{templateId:int}/fields")]
    public async Task<ActionResult<FormTemplateFieldDto>> AddField(
        string tenantSlug,
        int templateId,
        [FromBody] CreateFormTemplateFieldRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var field = await _templateService.AddFieldAsync(
                templateId, request, companyId.Value, userId.Value);

            return Ok(field);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template {templateId} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding field to template {TemplateId} for tenant {TenantSlug}", templateId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a field in a template
    /// </summary>
    [HttpPut("{templateId:int}/fields/{fieldId:int}")]
    public async Task<ActionResult<FormTemplateFieldDto>> UpdateField(
        string tenantSlug,
        int templateId,
        int fieldId,
        [FromBody] UpdateFormTemplateFieldRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var field = await _templateService.UpdateFieldAsync(
                templateId, fieldId, request, companyId.Value, userId.Value);

            return Ok(field);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Template or field not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating field {FieldId} in template {TemplateId} for tenant {TenantSlug}", fieldId, templateId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a field from a template
    /// </summary>
    [HttpDelete("{templateId:int}/fields/{fieldId:int}")]
    public async Task<ActionResult> DeleteField(string tenantSlug, int templateId, int fieldId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var deleted = await _templateService.DeleteFieldAsync(templateId, fieldId, companyId.Value);
            if (!deleted)
                return NotFound($"Template or field not found");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting field {FieldId} from template {TemplateId} for tenant {TenantSlug}", fieldId, templateId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reorder fields within a template
    /// </summary>
    [HttpPost("{templateId:int}/fields/reorder")]
    public async Task<ActionResult> ReorderFields(
        string tenantSlug,
        int templateId,
        [FromBody] ReorderFieldsRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var success = await _templateService.ReorderFieldsAsync(
                templateId, request.FieldIds, companyId.Value, userId.Value);

            if (!success)
                return NotFound($"Template {templateId} not found");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering fields in template {TemplateId} for tenant {TenantSlug}", templateId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Helpers

    private int? GetCompanyId()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        return int.TryParse(companyIdClaim, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    #endregion
}

#region Request DTOs

public class DuplicateTemplateRequest
{
    public string NewName { get; set; } = string.Empty;
}

public class ReorderFieldsRequest
{
    public List<int> FieldIds { get; set; } = new();
}

#endregion
