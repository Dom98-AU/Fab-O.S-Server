using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API Controller for Kit Template management
/// </summary>
[ApiController]
[Route("api/assets/kit-templates")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class KitTemplatesController : ControllerBase
{
    private readonly IKitTemplateService _templateService;
    private readonly ILogger<KitTemplatesController> _logger;

    public KitTemplatesController(
        IKitTemplateService templateService,
        ILogger<KitTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    private int GetCompanyId() => int.Parse(User.FindFirst("CompanyId")?.Value ?? "1");
    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    #region CRUD Endpoints

    /// <summary>
    /// Get all kit templates with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<KitTemplateListResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null)
    {
        var companyId = GetCompanyId();

        var templates = await _templateService.GetPagedAsync(companyId, page, pageSize, search, category, isActive);
        var totalCount = await _templateService.GetCountAsync(companyId, search, category, isActive);

        var response = new KitTemplateListResponse
        {
            Items = templates.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Get kit template by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<KitTemplateDto>> GetById(int id)
    {
        var template = await _templateService.GetWithItemsAsync(id);
        if (template == null)
            return NotFound(new { message = $"Kit template with ID {id} not found" });

        return Ok(MapToDto(template));
    }

    /// <summary>
    /// Get kit template by code
    /// </summary>
    [HttpGet("code/{templateCode}")]
    public async Task<ActionResult<KitTemplateDto>> GetByCode(string templateCode)
    {
        var companyId = GetCompanyId();
        var template = await _templateService.GetByCodeAsync(templateCode, companyId);

        if (template == null)
            return NotFound(new { message = $"Kit template with code '{templateCode}' not found" });

        return Ok(MapToDto(template));
    }

    /// <summary>
    /// Create new kit template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KitTemplateDto>> Create([FromBody] CreateKitTemplateRequest request)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();

        var template = new KitTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            IconClass = request.IconClass,
            DefaultCheckoutDays = request.DefaultCheckoutDays,
            RequiresSignature = request.RequiresSignature,
            RequiresConditionCheck = request.RequiresConditionCheck,
            IsActive = request.IsActive
        };

        var created = await _templateService.CreateAsync(template, companyId, userId);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToDto(created));
    }

    /// <summary>
    /// Update kit template
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<KitTemplateDto>> Update(int id, [FromBody] UpdateKitTemplateRequest request)
    {
        var userId = GetUserId();

        var existing = await _templateService.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { message = $"Kit template with ID {id} not found" });

        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Category = request.Category;
        existing.IconClass = request.IconClass;
        existing.DefaultCheckoutDays = request.DefaultCheckoutDays;
        existing.RequiresSignature = request.RequiresSignature;
        existing.RequiresConditionCheck = request.RequiresConditionCheck;
        existing.IsActive = request.IsActive;

        var updated = await _templateService.UpdateAsync(existing, userId);

        return Ok(MapToDto(updated));
    }

    /// <summary>
    /// Delete kit template
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool hardDelete = false)
    {
        var result = await _templateService.DeleteAsync(id, hardDelete);
        if (!result)
            return NotFound(new { message = $"Kit template with ID {id} not found" });

        return NoContent();
    }

    #endregion

    #region Template Items Endpoints

    /// <summary>
    /// Get template items
    /// </summary>
    [HttpGet("{id:int}/items")]
    public async Task<ActionResult<IEnumerable<KitTemplateItemDto>>> GetItems(int id)
    {
        var items = await _templateService.GetTemplateItemsAsync(id);
        return Ok(items.Select(MapTemplateItemToDto));
    }

    /// <summary>
    /// Add item to template
    /// </summary>
    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<KitTemplateItemDto>> AddItem(int id, [FromBody] AddTemplateItemRequest request)
    {
        try
        {
            var item = new KitTemplateItem
            {
                EquipmentTypeId = request.EquipmentTypeId,
                Quantity = request.Quantity,
                IsMandatory = request.IsMandatory,
                DisplayOrder = request.DisplayOrder,
                Notes = request.Notes
            };

            var added = await _templateService.AddTemplateItemAsync(id, item);
            return CreatedAtAction(nameof(GetItems), new { id }, MapTemplateItemToDto(added));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update template item
    /// </summary>
    [HttpPut("{id:int}/items/{itemId:int}")]
    public async Task<ActionResult<KitTemplateItemDto>> UpdateItem(int id, int itemId, [FromBody] UpdateTemplateItemRequest request)
    {
        try
        {
            var item = new KitTemplateItem
            {
                Id = itemId,
                Quantity = request.Quantity,
                IsMandatory = request.IsMandatory,
                DisplayOrder = request.DisplayOrder,
                Notes = request.Notes
            };

            var updated = await _templateService.UpdateTemplateItemAsync(item);
            return Ok(MapTemplateItemToDto(updated));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove item from template
    /// </summary>
    [HttpDelete("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> RemoveItem(int id, int itemId)
    {
        var result = await _templateService.RemoveTemplateItemAsync(id, itemId);
        if (!result)
            return NotFound(new { message = $"Template item with ID {itemId} not found in template {id}" });

        return NoContent();
    }

    /// <summary>
    /// Reorder template items
    /// </summary>
    [HttpPost("{id:int}/items/reorder")]
    public async Task<IActionResult> ReorderItems(int id, [FromBody] List<int> itemIds)
    {
        await _templateService.ReorderTemplateItemsAsync(id, itemIds);
        return NoContent();
    }

    #endregion

    #region Category Endpoints

    /// <summary>
    /// Get all template categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var companyId = GetCompanyId();
        var categories = await _templateService.GetCategoriesAsync(companyId);
        return Ok(categories);
    }

    /// <summary>
    /// Get template counts by category
    /// </summary>
    [HttpGet("categories/counts")]
    public async Task<ActionResult<Dictionary<string, int>>> GetCategoryCounts()
    {
        var companyId = GetCompanyId();
        var counts = await _templateService.GetTemplateCategoryCountsAsync(companyId);
        return Ok(counts);
    }

    #endregion

    #region Activation Endpoints

    /// <summary>
    /// Activate template
    /// </summary>
    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<KitTemplateDto>> Activate(int id)
    {
        try
        {
            var userId = GetUserId();
            var template = await _templateService.ActivateAsync(id, userId);
            return Ok(MapToDto(template));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate template
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<KitTemplateDto>> Deactivate(int id)
    {
        try
        {
            var userId = GetUserId();
            var template = await _templateService.DeactivateAsync(id, userId);
            return Ok(MapToDto(template));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get active templates only
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<KitTemplateDto>>> GetActive()
    {
        var companyId = GetCompanyId();
        var templates = await _templateService.GetActiveTemplatesAsync(companyId);
        return Ok(templates.Select(MapToDto));
    }

    #endregion

    #region Dashboard Endpoints

    /// <summary>
    /// Get template dashboard statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        var companyId = GetCompanyId();

        var total = await _templateService.GetTotalCountAsync(companyId);
        var active = await _templateService.GetActiveCountAsync(companyId);
        var byCategory = await _templateService.GetTemplatesByCategory(companyId);

        return Ok(new
        {
            TotalTemplates = total,
            ActiveTemplates = active,
            InactiveTemplates = total - active,
            ByCategory = byCategory
        });
    }

    #endregion

    #region Mapping Helpers

    private static KitTemplateDto MapToDto(KitTemplate template)
    {
        return new KitTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            TemplateCode = template.TemplateCode,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            IconClass = template.IconClass,
            DefaultCheckoutDays = template.DefaultCheckoutDays,
            RequiresSignature = template.RequiresSignature,
            RequiresConditionCheck = template.RequiresConditionCheck,
            IsActive = template.IsActive,
            CreatedDate = template.CreatedDate,
            CreatedByUserId = template.CreatedByUserId,
            LastModified = template.LastModified,
            LastModifiedByUserId = template.LastModifiedByUserId,
            TemplateItemCount = template.TemplateItems?.Count ?? 0,
            KitCount = template.Kits?.Count ?? 0,
            TemplateItems = template.TemplateItems?.Select(MapTemplateItemToDto).ToList() ?? new()
        };
    }

    private static KitTemplateItemDto MapTemplateItemToDto(KitTemplateItem item)
    {
        return new KitTemplateItemDto
        {
            Id = item.Id,
            KitTemplateId = item.KitTemplateId,
            EquipmentTypeId = item.EquipmentTypeId,
            EquipmentTypeName = item.EquipmentType?.Name,
            EquipmentCategoryName = item.EquipmentType?.EquipmentCategory?.Name,
            Quantity = item.Quantity,
            IsMandatory = item.IsMandatory,
            DisplayOrder = item.DisplayOrder,
            Notes = item.Notes
        };
    }

    #endregion
}
