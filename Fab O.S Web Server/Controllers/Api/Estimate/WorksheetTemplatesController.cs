using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api.Estimate;

/// <summary>
/// API Controller for managing Worksheet Templates.
/// Base endpoint: /api/{tenantSlug}/estimate/worksheet-templates
/// </summary>
[Route("api/{tenantSlug}/estimate/worksheet-templates")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class WorksheetTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFormulaEngine _formulaEngine;
    private readonly ILogger<WorksheetTemplatesController> _logger;

    public WorksheetTemplatesController(
        ApplicationDbContext context,
        IFormulaEngine formulaEngine,
        ILogger<WorksheetTemplatesController> logger)
    {
        _context = context;
        _formulaEngine = formulaEngine;
        _logger = logger;
    }

    /// <summary>
    /// Get all templates available to the company.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TemplateListDto>>> GetTemplates(
        string tenantSlug,
        [FromQuery] string? worksheetType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var query = _context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted))
                .Where(t => t.CompanyId == companyId && !t.IsDeleted);

            if (!string.IsNullOrEmpty(worksheetType))
                query = query.Where(t => t.WorksheetType == worksheetType);

            var total = await query.CountAsync();

            var templates = await query
                .OrderBy(t => t.WorksheetType)
                .ThenBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TemplateListDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    WorksheetType = t.WorksheetType,
                    ColumnCount = t.Columns.Count(c => !c.IsDeleted),
                    IsDefault = t.IsDefault,
                    CreatedDate = t.CreatedDate
                })
                .ToListAsync();

            return Ok(new
            {
                Data = templates,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for tenant {TenantSlug}", tenantSlug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific template with all columns.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TemplateDetailDto>> GetTemplate(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {id} not found");

            var dto = new TemplateDetailDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                WorksheetType = template.WorksheetType,
                IsDefault = template.IsDefault,
                CreatedDate = template.CreatedDate,
                ModifiedDate = template.ModifiedDate,
                Columns = template.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).Select(c => new TemplateColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.DisplayName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = !c.IsEditable,
                    IsFrozen = c.IsFrozen,
                    IsHidden = !c.IsVisible,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.DecimalPlaces,
                    SortOrder = c.DisplayOrder
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new template.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TemplateDetailDto>> CreateTemplate(
        string tenantSlug,
        [FromBody] CreateTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = new EstimationWorksheetTemplate
            {
                CompanyId = companyId.Value,
                Name = request.Name,
                Description = request.Description,
                WorksheetType = request.WorksheetType ?? "Custom",
                IsDefault = request.IsDefault,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            // If setting as default, unset existing default for this type
            if (request.IsDefault)
            {
                var existingDefaults = await _context.EstimationWorksheetTemplates
                    .Where(t => t.CompanyId == companyId &&
                                t.WorksheetType == template.WorksheetType &&
                                t.IsDefault &&
                                !t.IsDeleted)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }

            _context.EstimationWorksheetTemplates.Add(template);
            await _context.SaveChangesAsync();

            // Add columns
            if (request.Columns != null && request.Columns.Any())
            {
                foreach (var colReq in request.Columns)
                {
                    var column = new EstimationWorksheetColumn
                    {
                        WorksheetTemplateId = template.Id,
                        ColumnKey = colReq.ColumnKey,
                        DisplayName = colReq.ColumnName,
                        DataType = colReq.DataType ?? "Text",
                        Width = colReq.Width ?? 100,
                        IsRequired = colReq.IsRequired,
                        IsEditable = !colReq.IsReadOnly,
                        IsFrozen = colReq.IsFrozen,
                        IsVisible = !colReq.IsHidden,
                        Formula = colReq.Formula,
                        DefaultValue = colReq.DefaultValue,
                        SelectOptions = colReq.SelectOptions,
                        DecimalPlaces = colReq.Precision,
                        DisplayOrder = colReq.SortOrder ?? 0
                    };
                    _context.EstimationWorksheetColumns.Add(column);
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Created template {Name} for tenant {TenantSlug}", request.Name, tenantSlug);

            // Reload template with columns for response
            var createdTemplate = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(t => t.Id == template.Id);

            var responseDto = new TemplateDetailDto
            {
                Id = createdTemplate!.Id,
                Name = createdTemplate.Name,
                Description = createdTemplate.Description,
                WorksheetType = createdTemplate.WorksheetType,
                IsDefault = createdTemplate.IsDefault,
                CreatedDate = createdTemplate.CreatedDate,
                ModifiedDate = createdTemplate.ModifiedDate,
                Columns = createdTemplate.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).Select(c => new TemplateColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.DisplayName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = !c.IsEditable,
                    IsFrozen = c.IsFrozen,
                    IsHidden = !c.IsVisible,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.DecimalPlaces,
                    SortOrder = c.DisplayOrder
                }).ToList()
            };

            return CreatedAtAction(nameof(GetTemplate), new { tenantSlug, id = template.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating template");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a template.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TemplateDetailDto>> UpdateTemplate(
        string tenantSlug,
        int id,
        [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _context.EstimationWorksheetTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {id} not found");

            template.Name = request.Name ?? template.Name;
            template.Description = request.Description ?? template.Description;
            template.WorksheetType = request.WorksheetType ?? template.WorksheetType;
            template.ModifiedBy = userId.Value;
            template.ModifiedDate = DateTime.UtcNow;

            // Handle default flag
            if (request.IsDefault.HasValue && request.IsDefault.Value && !template.IsDefault)
            {
                var existingDefaults = await _context.EstimationWorksheetTemplates
                    .Where(t => t.CompanyId == companyId &&
                                t.WorksheetType == template.WorksheetType &&
                                t.IsDefault &&
                                t.Id != id &&
                                !t.IsDeleted)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }

                template.IsDefault = true;
            }
            else if (request.IsDefault.HasValue)
            {
                template.IsDefault = request.IsDefault.Value;
            }

            await _context.SaveChangesAsync();

            // Reload template with columns for response
            var updatedTemplate = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(t => t.Id == id);

            var responseDto = new TemplateDetailDto
            {
                Id = updatedTemplate!.Id,
                Name = updatedTemplate.Name,
                Description = updatedTemplate.Description,
                WorksheetType = updatedTemplate.WorksheetType,
                IsDefault = updatedTemplate.IsDefault,
                CreatedDate = updatedTemplate.CreatedDate,
                ModifiedDate = updatedTemplate.ModifiedDate,
                Columns = updatedTemplate.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).Select(c => new TemplateColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.DisplayName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = !c.IsEditable,
                    IsFrozen = c.IsFrozen,
                    IsHidden = !c.IsVisible,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.DecimalPlaces,
                    SortOrder = c.DisplayOrder
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating template {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a template (soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTemplate(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var template = await _context.EstimationWorksheetTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {id} not found");

            // Check if template is in use
            var worksheetsUsingTemplate = await _context.EstimationWorksheets
                .CountAsync(w => w.TemplateId == id && !w.IsDeleted);

            if (worksheetsUsingTemplate > 0)
            {
                return BadRequest($"Cannot delete template. It is used by {worksheetsUsingTemplate} worksheet(s).");
            }

            template.IsDeleted = true;
            template.ModifiedBy = userId.Value;
            template.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add a column to a template.
    /// </summary>
    [HttpPost("{templateId:int}/columns")]
    public async Task<ActionResult<TemplateColumnDto>> AddTemplateColumn(
        string tenantSlug,
        int templateId,
        [FromBody] CreateTemplateColumnRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns)
                .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {templateId} not found");

            // Validate column key is unique
            if (template.Columns.Any(c => c.ColumnKey == request.ColumnKey && !c.IsDeleted))
            {
                return BadRequest($"Column key '{request.ColumnKey}' already exists in this template");
            }

            // Validate formula if provided
            if (!string.IsNullOrEmpty(request.Formula))
            {
                var validation = _formulaEngine.ValidateTemplateFormula(request.Formula, template.Columns.ToList());
                if (!validation.IsValid)
                {
                    return BadRequest($"Invalid formula: {validation.ErrorMessage}");
                }
            }

            var nextDisplayOrder = template.Columns.Any()
                ? template.Columns.Max(c => c.DisplayOrder) + 1
                : 0;

            var column = new EstimationWorksheetColumn
            {
                WorksheetTemplateId = templateId,
                ColumnKey = request.ColumnKey,
                DisplayName = request.ColumnName,
                DataType = request.DataType ?? "Text",
                Width = request.Width ?? 100,
                IsRequired = request.IsRequired,
                IsEditable = !request.IsReadOnly,
                IsFrozen = request.IsFrozen,
                IsVisible = !request.IsHidden,
                Formula = request.Formula,
                DefaultValue = request.DefaultValue,
                SelectOptions = request.SelectOptions,
                DecimalPlaces = request.Precision,
                DisplayOrder = request.SortOrder ?? nextDisplayOrder
            };

            _context.EstimationWorksheetColumns.Add(column);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplate), new { tenantSlug, id = templateId },
                new TemplateColumnDto
                {
                    Id = column.Id,
                    ColumnKey = column.ColumnKey,
                    ColumnName = column.DisplayName,
                    DataType = column.DataType,
                    Width = column.Width,
                    IsRequired = column.IsRequired,
                    IsReadOnly = !column.IsEditable,
                    IsFrozen = column.IsFrozen,
                    IsHidden = !column.IsVisible,
                    Formula = column.Formula,
                    DefaultValue = column.DefaultValue,
                    SelectOptions = column.SelectOptions,
                    Precision = column.DecimalPlaces,
                    SortOrder = column.DisplayOrder
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding column to template {TemplateId}", templateId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a template column.
    /// </summary>
    [HttpPut("{templateId:int}/columns/{columnId:int}")]
    public async Task<ActionResult<TemplateColumnDto>> UpdateTemplateColumn(
        string tenantSlug,
        int templateId,
        int columnId,
        [FromBody] UpdateTemplateColumnRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns)
                .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {templateId} not found");

            var column = template.Columns.FirstOrDefault(c => c.Id == columnId && !c.IsDeleted);
            if (column == null)
                return NotFound($"Column {columnId} not found");

            // Validate formula if provided
            if (request.Formula != null && !string.IsNullOrEmpty(request.Formula))
            {
                var validation = _formulaEngine.ValidateTemplateFormula(request.Formula, template.Columns.ToList());
                if (!validation.IsValid)
                {
                    return BadRequest($"Invalid formula: {validation.ErrorMessage}");
                }
            }

            column.DisplayName = request.ColumnName ?? column.DisplayName;
            column.DataType = request.DataType ?? column.DataType;
            column.Width = request.Width ?? column.Width;
            column.IsRequired = request.IsRequired ?? column.IsRequired;
            column.IsEditable = request.IsReadOnly.HasValue ? !request.IsReadOnly.Value : column.IsEditable;
            column.IsFrozen = request.IsFrozen ?? column.IsFrozen;
            column.IsVisible = request.IsHidden.HasValue ? !request.IsHidden.Value : column.IsVisible;
            column.Formula = request.Formula ?? column.Formula;
            column.DefaultValue = request.DefaultValue ?? column.DefaultValue;
            column.SelectOptions = request.SelectOptions ?? column.SelectOptions;
            column.DecimalPlaces = request.Precision ?? column.DecimalPlaces;
            column.DisplayOrder = request.SortOrder ?? column.DisplayOrder;

            await _context.SaveChangesAsync();

            return Ok(new TemplateColumnDto
            {
                Id = column.Id,
                ColumnKey = column.ColumnKey,
                ColumnName = column.DisplayName,
                DataType = column.DataType,
                Width = column.Width,
                IsRequired = column.IsRequired,
                IsReadOnly = !column.IsEditable,
                IsFrozen = column.IsFrozen,
                IsHidden = !column.IsVisible,
                Formula = column.Formula,
                DefaultValue = column.DefaultValue,
                SelectOptions = column.SelectOptions,
                Precision = column.DecimalPlaces,
                SortOrder = column.DisplayOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating column {ColumnId} in template {TemplateId}",
                columnId, templateId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a template column.
    /// </summary>
    [HttpDelete("{templateId:int}/columns/{columnId:int}")]
    public async Task<IActionResult> DeleteTemplateColumn(
        string tenantSlug,
        int templateId,
        int columnId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _context.EstimationWorksheetTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {templateId} not found");

            var column = await _context.EstimationWorksheetColumns
                .FirstOrDefaultAsync(c => c.Id == columnId && c.WorksheetTemplateId == templateId && !c.IsDeleted);

            if (column == null)
                return NotFound($"Column {columnId} not found");

            column.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting column {ColumnId} from template {TemplateId}",
                columnId, templateId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reorder columns in a template.
    /// </summary>
    [HttpPost("{templateId:int}/columns/reorder")]
    public async Task<IActionResult> ReorderTemplateColumns(
        string tenantSlug,
        int templateId,
        [FromBody] ReorderColumnsRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var template = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns)
                .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId && !t.IsDeleted);

            if (template == null)
                return NotFound($"Template {templateId} not found");

            foreach (var item in request.Columns)
            {
                var column = template.Columns.FirstOrDefault(c => c.Id == item.ColumnId && !c.IsDeleted);
                if (column != null)
                {
                    column.DisplayOrder = item.SortOrder;
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering columns in template {TemplateId}", templateId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Duplicate a template.
    /// </summary>
    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<TemplateDetailDto>> DuplicateTemplate(
        string tenantSlug,
        int id,
        [FromBody] DuplicateTemplateRequest? request = null)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var source = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId && !t.IsDeleted);

            if (source == null)
                return NotFound($"Template {id} not found");

            // Generate a unique name for the duplicate
            var baseName = request?.NewName ?? $"{source.Name} (Copy)";
            var newName = baseName;
            var counter = 1;

            // Check for existing names and add counter if needed
            // Note: Do NOT filter by IsDeleted - the database unique constraint includes ALL records
            while (await _context.EstimationWorksheetTemplates
                .AnyAsync(t => t.CompanyId == companyId && t.Name == newName))
            {
                newName = $"{baseName} {counter++}";
            }

            var newTemplate = new EstimationWorksheetTemplate
            {
                CompanyId = companyId.Value,
                Name = newName,
                Description = source.Description,
                WorksheetType = source.WorksheetType,
                IsDefault = false,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationWorksheetTemplates.Add(newTemplate);
            await _context.SaveChangesAsync();

            // Copy columns
            foreach (var sourceCol in source.Columns.Where(c => !c.IsDeleted))
            {
                var newCol = new EstimationWorksheetColumn
                {
                    WorksheetTemplateId = newTemplate.Id,
                    ColumnKey = sourceCol.ColumnKey,
                    DisplayName = sourceCol.DisplayName,
                    DataType = sourceCol.DataType,
                    Width = sourceCol.Width,
                    IsRequired = sourceCol.IsRequired,
                    IsEditable = sourceCol.IsEditable,
                    IsFrozen = sourceCol.IsFrozen,
                    IsVisible = sourceCol.IsVisible,
                    Formula = sourceCol.Formula,
                    DefaultValue = sourceCol.DefaultValue,
                    SelectOptions = sourceCol.SelectOptions,
                    DecimalPlaces = sourceCol.DecimalPlaces,
                    DisplayOrder = sourceCol.DisplayOrder
                };
                _context.EstimationWorksheetColumns.Add(newCol);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Duplicated template {SourceId} to {NewId}", id, newTemplate.Id);

            // Reload template with columns for response
            var createdTemplate = await _context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder))
                .FirstOrDefaultAsync(t => t.Id == newTemplate.Id);

            var responseDto = new TemplateDetailDto
            {
                Id = createdTemplate!.Id,
                Name = createdTemplate.Name,
                Description = createdTemplate.Description,
                WorksheetType = createdTemplate.WorksheetType,
                IsDefault = createdTemplate.IsDefault,
                CreatedDate = createdTemplate.CreatedDate,
                ModifiedDate = createdTemplate.ModifiedDate,
                Columns = createdTemplate.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).Select(c => new TemplateColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.DisplayName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = !c.IsEditable,
                    IsFrozen = c.IsFrozen,
                    IsHidden = !c.IsVisible,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.DecimalPlaces,
                    SortOrder = c.DisplayOrder
                }).ToList()
            };

            return CreatedAtAction(nameof(GetTemplate), new { tenantSlug, id = newTemplate.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating template {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Helpers

    private int? GetCompanyId()
    {
        var claim = User.FindFirst("company_id");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    #endregion
}

#region DTOs

public class TemplateListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorksheetType { get; set; } = string.Empty;
    public int ColumnCount { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class TemplateDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorksheetType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public IList<TemplateColumnDto> Columns { get; set; } = new List<TemplateColumnDto>();
}

public class TemplateColumnDto
{
    public int Id { get; set; }
    public string ColumnKey { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int Width { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsHidden { get; set; }
    public string? Formula { get; set; }
    public string? DefaultValue { get; set; }
    public string? SelectOptions { get; set; }
    public int? Precision { get; set; }
    public int SortOrder { get; set; }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WorksheetType { get; set; }
    public bool IsDefault { get; set; }
    public IList<CreateTemplateColumnRequest>? Columns { get; set; }
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? WorksheetType { get; set; }
    public bool? IsDefault { get; set; }
}

public class CreateTemplateColumnRequest
{
    public string ColumnKey { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public int? Width { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsFrozen { get; set; }
    public bool IsHidden { get; set; }
    public string? Formula { get; set; }
    public string? DefaultValue { get; set; }
    public string? SelectOptions { get; set; }
    public int? Precision { get; set; }
    public int? SortOrder { get; set; }
}

public class UpdateTemplateColumnRequest
{
    public string? ColumnName { get; set; }
    public string? DataType { get; set; }
    public int? Width { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsReadOnly { get; set; }
    public bool? IsFrozen { get; set; }
    public bool? IsHidden { get; set; }
    public string? Formula { get; set; }
    public string? DefaultValue { get; set; }
    public string? SelectOptions { get; set; }
    public int? Precision { get; set; }
    public int? SortOrder { get; set; }
}

public class ReorderColumnsRequest
{
    public IList<ColumnOrderItem> Columns { get; set; } = new List<ColumnOrderItem>();
}

public class ColumnOrderItem
{
    public int ColumnId { get; set; }
    public int SortOrder { get; set; }
}

public class DuplicateTemplateRequest
{
    public string? NewName { get; set; }
}

#endregion
