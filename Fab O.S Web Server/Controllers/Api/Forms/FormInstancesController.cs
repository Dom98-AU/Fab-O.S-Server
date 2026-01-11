using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api.Forms;

/// <summary>
/// API Controller for managing Form Instances (filled forms).
/// Base endpoint: /api/{tenantSlug}/forms/instances
/// </summary>
[Route("api/{tenantSlug}/forms/instances")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class FormInstancesController : ControllerBase
{
    private readonly IFormInstanceService _instanceService;
    private readonly IFormPdfService _pdfService;
    private readonly ILogger<FormInstancesController> _logger;

    public FormInstancesController(
        IFormInstanceService instanceService,
        IFormPdfService pdfService,
        ILogger<FormInstancesController> logger)
    {
        _instanceService = instanceService;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Get form instances with filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<FormInstanceListResponse>> GetInstances(
        string tenantSlug,
        [FromQuery] FormModuleContext? module = null,
        [FromQuery] FormInstanceStatus? status = null,
        [FromQuery] int? templateId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] int? createdBy = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = true)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var filter = new FormInstanceFilterRequest
            {
                ModuleContext = module,
                Status = status,
                FormTemplateId = templateId,
                LinkedEntityType = entityType,
                LinkedEntityId = entityId,
                CreatedByUserId = createdBy,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDesc
            };

            var result = await _instanceService.GetInstancesAsync(companyId.Value, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form instances for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific form instance by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FormInstanceDto>> GetInstance(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var instance = await _instanceService.GetInstanceWithValuesAsync(id, companyId.Value);
            if (instance == null)
                return NotFound($"Form instance {id} not found");

            return Ok(instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get form instances linked to a specific entity
    /// </summary>
    [HttpGet("by-entity/{entityType}/{entityId:int}")]
    public async Task<ActionResult<List<FormInstanceSummaryDto>>> GetInstancesByEntity(
        string tenantSlug,
        string entityType,
        int entityId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var instances = await _instanceService.GetInstancesByEntityAsync(
                entityType, entityId, companyId.Value);

            return Ok(instances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form instances for entity {EntityType}/{EntityId} for tenant {TenantSlug}",
                entityType, entityId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new form instance from a template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FormInstanceDto>> CreateInstance(
        string tenantSlug,
        [FromBody] CreateFormInstanceRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.CreateInstanceAsync(
                request, companyId.Value, userId.Value);

            return CreatedAtAction(
                nameof(GetInstance),
                new { tenantSlug, id = instance.Id },
                instance);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form instance for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update form instance values
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FormInstanceDto>> UpdateInstance(
        string tenantSlug,
        int id,
        [FromBody] UpdateFormInstanceRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.UpdateInstanceAsync(
                id, request, companyId.Value, userId.Value);

            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a form instance
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteInstance(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var deleted = await _instanceService.DeleteInstanceAsync(id, companyId.Value);
            if (!deleted)
                return NotFound($"Form instance {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Submit form instance for review
    /// </summary>
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<FormInstanceDto>> SubmitForReview(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.SubmitForReviewAsync(id, companyId.Value, userId.Value);
            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Start reviewing a submitted form
    /// </summary>
    [HttpPost("{id:int}/start-review")]
    public async Task<ActionResult<FormInstanceDto>> StartReview(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.StartReviewAsync(id, companyId.Value, userId.Value);
            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting review for form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approve a form instance
    /// </summary>
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<FormInstanceDto>> Approve(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.ApproveAsync(id, companyId.Value, userId.Value);
            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reject a form instance
    /// </summary>
    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<FormInstanceDto>> Reject(
        string tenantSlug,
        int id,
        [FromBody] RejectFormInstanceRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.RejectAsync(
                id, request.Reason, companyId.Value, userId.Value);

            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Revert form instance back to draft
    /// </summary>
    [HttpPost("{id:int}/revert-to-draft")]
    public async Task<ActionResult<FormInstanceDto>> RevertToDraft(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var instance = await _instanceService.RevertToDraftAsync(id, companyId.Value, userId.Value);
            return Ok(instance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting form instance {Id} to draft for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Set a single field value
    /// </summary>
    [HttpPut("{id:int}/values/{fieldKey}")]
    public async Task<ActionResult<FormInstanceValueDto>> SetFieldValue(
        string tenantSlug,
        int id,
        string fieldKey,
        [FromBody] FormInstanceValueRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var value = await _instanceService.SetFieldValueAsync(
                id, fieldKey, request, companyId.Value, userId.Value);

            return Ok(value);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting field {FieldKey} value for form instance {Id} for tenant {TenantSlug}",
                fieldKey, id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a single field value
    /// </summary>
    [HttpGet("{id:int}/values/{fieldKey}")]
    public async Task<ActionResult<FormInstanceValueDto>> GetFieldValue(
        string tenantSlug,
        int id,
        string fieldKey)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var value = await _instanceService.GetFieldValueAsync(id, fieldKey, companyId.Value);
            if (value == null)
                return NotFound($"Field {fieldKey} not found");

            return Ok(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting field {FieldKey} value for form instance {Id} for tenant {TenantSlug}",
                fieldKey, id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Upload an attachment to a form instance
    /// </summary>
    [HttpPost("{id:int}/attachments")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<ActionResult<FormInstanceAttachmentDto>> UploadAttachment(
        string tenantSlug,
        int id,
        IFormFile file,
        [FromForm] string? fieldKey = null,
        [FromForm] string? caption = null,
        [FromForm] decimal? latitude = null,
        [FromForm] decimal? longitude = null)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            using var stream = file.OpenReadStream();
            var attachment = await _instanceService.UploadAttachmentAsync(
                id, fieldKey, stream, file.FileName, file.ContentType,
                latitude, longitude, caption, companyId.Value, userId.Value);

            return Ok(attachment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment to form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an attachment
    /// </summary>
    [HttpDelete("attachments/{attachmentId:int}")]
    public async Task<ActionResult> DeleteAttachment(string tenantSlug, int attachmentId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var deleted = await _instanceService.DeleteAttachmentAsync(attachmentId, companyId.Value);
            if (!deleted)
                return NotFound($"Attachment {attachmentId} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId} for tenant {TenantSlug}", attachmentId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate form instance
    /// </summary>
    [HttpGet("{id:int}/validate")]
    public async Task<ActionResult<FormValidationResult>> ValidateInstance(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var result = await _instanceService.ValidateInstanceAsync(id, companyId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Export form instance as HTML (for Nutrient PDF generation)
    /// </summary>
    /// <param name="tenantSlug">Tenant slug</param>
    /// <param name="id">Form instance ID</param>
    /// <param name="includeHeader">Include company header/logo</param>
    /// <param name="includeFooter">Include page footer</param>
    /// <param name="includeApprovalHistory">Include approval history section</param>
    /// <param name="includePhotos">Include attached photos inline</param>
    /// <param name="includeSignatures">Include signatures as images</param>
    [HttpGet("{id:int}/export-html")]
    public async Task<ActionResult<string>> ExportHtml(
        string tenantSlug,
        int id,
        [FromQuery] bool includeHeader = true,
        [FromQuery] bool includeFooter = true,
        [FromQuery] bool includeApprovalHistory = true,
        [FromQuery] bool includePhotos = true,
        [FromQuery] bool includeSignatures = true)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var options = new FormExportOptions
            {
                IncludeHeader = includeHeader,
                IncludeFooter = includeFooter,
                IncludeApprovalHistory = includeApprovalHistory,
                IncludePhotos = includePhotos,
                IncludeSignatures = includeSignatures
            };

            var html = await _pdfService.GenerateFormHtmlAsync(id, companyId.Value, options);
            return Content(html, "text/html");
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form instance {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting form instance {Id} for tenant {TenantSlug}", id, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Export a blank form template as HTML (for printing)
    /// </summary>
    [HttpGet("templates/{templateId:int}/export-html")]
    public async Task<ActionResult<string>> ExportBlankFormHtml(string tenantSlug, int templateId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var html = await _pdfService.GenerateBlankFormHtmlAsync(templateId, companyId.Value);
            return Content(html, "text/html");
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Form template {templateId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting blank form template {TemplateId} for tenant {TenantSlug}", templateId, tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get form statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<FormStatisticsDto>> GetStatistics(
        string tenantSlug,
        [FromQuery] FormModuleContext? module = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var stats = await _instanceService.GetStatisticsAsync(
                companyId.Value, module, dateFrom, dateTo);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting form statistics for tenant {TenantSlug}", tenantSlug);
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
