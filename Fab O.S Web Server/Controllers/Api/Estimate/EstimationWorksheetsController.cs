using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;
using System.Text.Json;

namespace FabOS.WebServer.Controllers.Api.Estimate;

/// <summary>
/// API Controller for managing Estimation Worksheets and Rows.
/// Base endpoint: /api/{tenantSlug}/estimate/worksheets
/// </summary>
[Route("api/{tenantSlug}/estimate/worksheets")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class EstimationWorksheetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEstimationCalculationService _calculationService;
    private readonly IFormulaEngine _formulaEngine;
    private readonly ILogger<EstimationWorksheetsController> _logger;

    public EstimationWorksheetsController(
        ApplicationDbContext context,
        IEstimationCalculationService calculationService,
        IFormulaEngine formulaEngine,
        ILogger<EstimationWorksheetsController> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _formulaEngine = formulaEngine;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific worksheet with all columns and rows.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorksheetDetailDto>> GetWorksheet(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .Include(w => w.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder))
                .Include(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder))
                .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {id} not found");

            var dto = new WorksheetDetailDto
            {
                Id = worksheet.Id,
                PackageId = worksheet.PackageId,
                PackageName = worksheet.Package?.Name,
                RevisionLetter = worksheet.Package?.Revision?.RevisionLetter,
                TemplateId = worksheet.TemplateId,
                Name = worksheet.Name,
                Description = worksheet.Description,
                WorksheetType = worksheet.WorksheetType,
                SortOrder = worksheet.SortOrder,
                TotalMaterialCost = worksheet.TotalMaterialCost,
                TotalLaborHours = worksheet.TotalLaborHours,
                TotalLaborCost = worksheet.TotalLaborCost,
                TotalCost = worksheet.TotalCost,
                CreatedDate = worksheet.CreatedDate,
                ModifiedDate = worksheet.ModifiedDate,
                Columns = worksheet.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder).Select(c => new ColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.ColumnName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = c.IsReadOnly,
                    IsFrozen = c.IsFrozen,
                    IsHidden = c.IsHidden,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.Precision,
                    SortOrder = c.SortOrder
                }).ToList(),
                Rows = worksheet.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder).Select(r => new RowDto
                {
                    Id = r.Id,
                    RowData = r.RowData,
                    SortOrder = r.SortOrder,
                    IsGroupHeader = r.IsGroupHeader,
                    GroupName = r.GroupName,
                    ParentRowId = r.ParentRowId,
                    CalculatedTotal = r.CalculatedTotal
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worksheet {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new worksheet in a package.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorksheetDetailDto>> CreateWorksheet(
        string tenantSlug,
        [FromBody] CreateWorksheetRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            // Verify package exists
            var package = await _context.EstimationRevisionPackages
                .Include(p => p.Revision)
                .Include(p => p.Worksheets)
                .FirstOrDefaultAsync(p => p.Id == request.PackageId && p.CompanyId == companyId && !p.IsDeleted);

            if (package == null)
                return NotFound($"Package {request.PackageId} not found");

            if (package.Revision?.Status != "Draft")
                return BadRequest("Can only add worksheets to packages in draft revisions");

            // Get next sort order
            var nextSortOrder = package.Worksheets.Any()
                ? package.Worksheets.Max(w => w.SortOrder) + 1
                : 0;

            var worksheet = new EstimationWorksheet
            {
                CompanyId = companyId.Value,
                PackageId = request.PackageId,
                TemplateId = request.TemplateId,
                Name = request.Name,
                Description = request.Description,
                WorksheetType = request.WorksheetType ?? "Custom",
                SortOrder = request.SortOrder ?? nextSortOrder,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationWorksheets.Add(worksheet);
            await _context.SaveChangesAsync();

            // Copy columns from template if provided
            if (request.TemplateId.HasValue)
            {
                await CopyColumnsFromTemplate(request.TemplateId.Value, worksheet.Id);
            }
            else if (request.Columns != null && request.Columns.Any())
            {
                // Create columns from request
                foreach (var colReq in request.Columns)
                {
                    var column = new EstimationWorksheetInstanceColumn
                    {
                        WorksheetId = worksheet.Id,
                        ColumnKey = colReq.ColumnKey,
                        ColumnName = colReq.ColumnName,
                        DataType = colReq.DataType ?? "text",
                        Width = colReq.Width ?? 150,
                        IsRequired = colReq.IsRequired,
                        IsReadOnly = colReq.IsReadOnly,
                        Formula = colReq.Formula,
                        DefaultValue = colReq.DefaultValue,
                        SelectOptions = colReq.SelectOptions,
                        SortOrder = colReq.SortOrder
                    };
                    _context.EstimationWorksheetInstanceColumns.Add(column);
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Created worksheet {Name} in package {PackageId}", request.Name, request.PackageId);

            // Reload worksheet with columns and rows for response
            var createdWorksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .Include(w => w.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder))
                .Include(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder))
                .FirstOrDefaultAsync(w => w.Id == worksheet.Id);

            var responseDto = new WorksheetDetailDto
            {
                Id = createdWorksheet!.Id,
                PackageId = createdWorksheet.PackageId,
                PackageName = createdWorksheet.Package?.Name,
                RevisionLetter = createdWorksheet.Package?.Revision?.RevisionLetter,
                TemplateId = createdWorksheet.TemplateId,
                Name = createdWorksheet.Name,
                Description = createdWorksheet.Description,
                WorksheetType = createdWorksheet.WorksheetType,
                SortOrder = createdWorksheet.SortOrder,
                TotalMaterialCost = createdWorksheet.TotalMaterialCost,
                TotalLaborHours = createdWorksheet.TotalLaborHours,
                TotalLaborCost = createdWorksheet.TotalLaborCost,
                TotalCost = createdWorksheet.TotalCost,
                CreatedDate = createdWorksheet.CreatedDate,
                ModifiedDate = createdWorksheet.ModifiedDate,
                Columns = createdWorksheet.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder).Select(c => new ColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.ColumnName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = c.IsReadOnly,
                    IsFrozen = c.IsFrozen,
                    IsHidden = c.IsHidden,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.Precision,
                    SortOrder = c.SortOrder
                }).ToList(),
                Rows = createdWorksheet.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder).Select(r => new RowDto
                {
                    Id = r.Id,
                    RowData = r.RowData,
                    SortOrder = r.SortOrder,
                    IsGroupHeader = r.IsGroupHeader,
                    GroupName = r.GroupName,
                    ParentRowId = r.ParentRowId,
                    CalculatedTotal = r.CalculatedTotal
                }).ToList()
            };

            return CreatedAtAction(nameof(GetWorksheet), new { tenantSlug, id = worksheet.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating worksheet");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update worksheet metadata.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<WorksheetDetailDto>> UpdateWorksheet(
        string tenantSlug,
        int id,
        [FromBody] UpdateWorksheetRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {id} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only modify worksheets in draft revisions");

            worksheet.Name = request.Name ?? worksheet.Name;
            worksheet.Description = request.Description ?? worksheet.Description;
            worksheet.SortOrder = request.SortOrder ?? worksheet.SortOrder;
            worksheet.ModifiedBy = userId.Value;
            worksheet.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload worksheet with columns and rows for response
            var updatedWorksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .Include(w => w.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder))
                .Include(w => w.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder))
                .FirstOrDefaultAsync(w => w.Id == id);

            var responseDto = new WorksheetDetailDto
            {
                Id = updatedWorksheet!.Id,
                PackageId = updatedWorksheet.PackageId,
                PackageName = updatedWorksheet.Package?.Name,
                RevisionLetter = updatedWorksheet.Package?.Revision?.RevisionLetter,
                TemplateId = updatedWorksheet.TemplateId,
                Name = updatedWorksheet.Name,
                Description = updatedWorksheet.Description,
                WorksheetType = updatedWorksheet.WorksheetType,
                SortOrder = updatedWorksheet.SortOrder,
                TotalMaterialCost = updatedWorksheet.TotalMaterialCost,
                TotalLaborHours = updatedWorksheet.TotalLaborHours,
                TotalLaborCost = updatedWorksheet.TotalLaborCost,
                TotalCost = updatedWorksheet.TotalCost,
                CreatedDate = updatedWorksheet.CreatedDate,
                ModifiedDate = updatedWorksheet.ModifiedDate,
                Columns = updatedWorksheet.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder).Select(c => new ColumnDto
                {
                    Id = c.Id,
                    ColumnKey = c.ColumnKey,
                    ColumnName = c.ColumnName,
                    DataType = c.DataType,
                    Width = c.Width,
                    IsRequired = c.IsRequired,
                    IsReadOnly = c.IsReadOnly,
                    IsFrozen = c.IsFrozen,
                    IsHidden = c.IsHidden,
                    Formula = c.Formula,
                    DefaultValue = c.DefaultValue,
                    SelectOptions = c.SelectOptions,
                    Precision = c.Precision,
                    SortOrder = c.SortOrder
                }).ToList(),
                Rows = updatedWorksheet.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.SortOrder).Select(r => new RowDto
                {
                    Id = r.Id,
                    RowData = r.RowData,
                    SortOrder = r.SortOrder,
                    IsGroupHeader = r.IsGroupHeader,
                    GroupName = r.GroupName,
                    ParentRowId = r.ParentRowId,
                    CalculatedTotal = r.CalculatedTotal
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating worksheet {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a worksheet (soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteWorksheet(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {id} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only delete worksheets in draft revisions");

            worksheet.IsDeleted = true;
            worksheet.ModifiedBy = userId.Value;
            worksheet.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate package totals
            await _calculationService.RecalculatePackageAsync(worksheet.PackageId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting worksheet {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Row Operations

    /// <summary>
    /// Add a row to a worksheet.
    /// </summary>
    [HttpPost("{worksheetId:int}/rows")]
    public async Task<ActionResult<RowDto>> AddRow(
        string tenantSlug,
        int worksheetId,
        [FromBody] CreateRowRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Columns.Where(c => !c.IsDeleted))
                .Include(w => w.Rows.Where(r => !r.IsDeleted))
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only add rows to worksheets in draft revisions");

            // Get next sort order
            var nextSortOrder = worksheet.Rows.Any()
                ? worksheet.Rows.Max(r => r.SortOrder) + 1
                : 0;

            var row = new EstimationWorksheetRow
            {
                CompanyId = companyId.Value,
                WorksheetId = worksheetId,
                RowData = request.RowData ?? "{}",
                SortOrder = request.SortOrder ?? nextSortOrder,
                IsGroupHeader = request.IsGroupHeader,
                GroupName = request.GroupName,
                ParentRowId = request.ParentRowId,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationWorksheetRows.Add(row);
            await _context.SaveChangesAsync();

            // Recalculate computed columns
            if (!row.IsGroupHeader)
            {
                var allRows = worksheet.Rows.Append(row).ToList();
                row = await _calculationService.RecalculateRowAsync(
                    row, worksheet, allRows, worksheet.Columns.ToList());
                await _context.SaveChangesAsync();
            }

            // Log change
            await LogRowChange(worksheetId, row.Id, "Added", userId.Value, null, row.RowData);

            return CreatedAtAction(nameof(GetWorksheet), new { tenantSlug, id = worksheetId },
                new RowDto
                {
                    Id = row.Id,
                    RowData = row.RowData,
                    SortOrder = row.SortOrder,
                    IsGroupHeader = row.IsGroupHeader,
                    GroupName = row.GroupName,
                    ParentRowId = row.ParentRowId,
                    CalculatedTotal = row.CalculatedTotal
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding row to worksheet {WorksheetId}", worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a row's data.
    /// </summary>
    [HttpPut("{worksheetId:int}/rows/{rowId:int}")]
    public async Task<ActionResult<RowDto>> UpdateRow(
        string tenantSlug,
        int worksheetId,
        int rowId,
        [FromBody] UpdateRowRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Columns.Where(c => !c.IsDeleted))
                .Include(w => w.Rows.Where(r => !r.IsDeleted))
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            var row = worksheet.Rows.FirstOrDefault(r => r.Id == rowId);
            if (row == null)
                return NotFound($"Row {rowId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only modify rows in worksheets in draft revisions");

            var oldData = row.RowData;

            // Update row data
            row.RowData = request.RowData ?? row.RowData;
            row.SortOrder = request.SortOrder ?? row.SortOrder;
            row.IsGroupHeader = request.IsGroupHeader ?? row.IsGroupHeader;
            row.GroupName = request.GroupName ?? row.GroupName;
            row.ModifiedBy = userId.Value;
            row.ModifiedDate = DateTime.UtcNow;

            // Recalculate computed columns
            if (!row.IsGroupHeader)
            {
                row = await _calculationService.RecalculateRowAsync(
                    row, worksheet, worksheet.Rows.ToList(), worksheet.Columns.ToList());
            }

            await _context.SaveChangesAsync();

            // Log change
            await LogRowChange(worksheetId, rowId, "Modified", userId.Value, oldData, row.RowData);

            return Ok(new RowDto
            {
                Id = row.Id,
                RowData = row.RowData,
                SortOrder = row.SortOrder,
                IsGroupHeader = row.IsGroupHeader,
                GroupName = row.GroupName,
                ParentRowId = row.ParentRowId,
                CalculatedTotal = row.CalculatedTotal
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating row {RowId} in worksheet {WorksheetId}", rowId, worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a row.
    /// </summary>
    [HttpDelete("{worksheetId:int}/rows/{rowId:int}")]
    public async Task<IActionResult> DeleteRow(
        string tenantSlug,
        int worksheetId,
        int rowId)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only delete rows in worksheets in draft revisions");

            var row = await _context.EstimationWorksheetRows
                .FirstOrDefaultAsync(r => r.Id == rowId && r.WorksheetId == worksheetId && !r.IsDeleted);

            if (row == null)
                return NotFound($"Row {rowId} not found");

            row.IsDeleted = true;
            row.ModifiedBy = userId.Value;
            row.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log change
            await LogRowChange(worksheetId, rowId, "Deleted", userId.Value, row.RowData, null);

            // Recalculate worksheet totals
            await _calculationService.RecalculateWorksheetAsync(worksheetId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting row {RowId} in worksheet {WorksheetId}", rowId, worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Bulk update rows (for pasting data or batch edits).
    /// </summary>
    [HttpPost("{worksheetId:int}/rows/bulk")]
    public async Task<ActionResult<IEnumerable<RowDto>>> BulkUpdateRows(
        string tenantSlug,
        int worksheetId,
        [FromBody] BulkRowsRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Columns.Where(c => !c.IsDeleted))
                .Include(w => w.Rows.Where(r => !r.IsDeleted))
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only modify rows in worksheets in draft revisions");

            var updatedRows = new List<RowDto>();

            foreach (var rowUpdate in request.Rows)
            {
                EstimationWorksheetRow row;

                if (rowUpdate.Id.HasValue)
                {
                    // Update existing row
                    row = worksheet.Rows.FirstOrDefault(r => r.Id == rowUpdate.Id.Value);
                    if (row == null)
                        continue;

                    row.RowData = rowUpdate.RowData ?? row.RowData;
                    row.SortOrder = rowUpdate.SortOrder ?? row.SortOrder;
                    row.ModifiedBy = userId.Value;
                    row.ModifiedDate = DateTime.UtcNow;
                }
                else
                {
                    // Create new row
                    var nextSortOrder = worksheet.Rows.Any()
                        ? worksheet.Rows.Max(r => r.SortOrder) + 1
                        : 0;

                    row = new EstimationWorksheetRow
                    {
                        CompanyId = companyId.Value,
                        WorksheetId = worksheetId,
                        RowData = rowUpdate.RowData ?? "{}",
                        SortOrder = rowUpdate.SortOrder ?? nextSortOrder,
                        IsGroupHeader = false,
                        CreatedBy = userId.Value,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.EstimationWorksheetRows.Add(row);
                    worksheet.Rows.Add(row);
                }
            }

            await _context.SaveChangesAsync();

            // Recalculate all rows
            foreach (var row in worksheet.Rows.Where(r => !r.IsDeleted && !r.IsGroupHeader))
            {
                await _calculationService.RecalculateRowAsync(
                    row, worksheet, worksheet.Rows.ToList(), worksheet.Columns.ToList());
            }

            await _context.SaveChangesAsync();

            // Return updated rows
            updatedRows = worksheet.Rows.Where(r => !r.IsDeleted).Select(r => new RowDto
            {
                Id = r.Id,
                RowData = r.RowData,
                SortOrder = r.SortOrder,
                IsGroupHeader = r.IsGroupHeader,
                GroupName = r.GroupName,
                ParentRowId = r.ParentRowId,
                CalculatedTotal = r.CalculatedTotal
            }).ToList();

            return Ok(updatedRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating rows in worksheet {WorksheetId}", worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Column Operations

    /// <summary>
    /// Add a column to a worksheet.
    /// </summary>
    [HttpPost("{worksheetId:int}/columns")]
    public async Task<ActionResult<ColumnDto>> AddColumn(
        string tenantSlug,
        int worksheetId,
        [FromBody] CreateColumnRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Columns)
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only add columns to worksheets in draft revisions");

            // Get next sort order
            var nextSortOrder = worksheet.Columns.Any()
                ? worksheet.Columns.Max(c => c.SortOrder) + 1
                : 0;

            var column = new EstimationWorksheetInstanceColumn
            {
                WorksheetId = worksheetId,
                ColumnKey = request.ColumnKey,
                ColumnName = request.ColumnName,
                DataType = request.DataType ?? "text",
                Width = request.Width ?? 150,
                IsRequired = request.IsRequired,
                IsReadOnly = request.IsReadOnly,
                IsFrozen = request.IsFrozen,
                IsHidden = request.IsHidden,
                Formula = request.Formula,
                DefaultValue = request.DefaultValue,
                SelectOptions = request.SelectOptions,
                Precision = request.Precision,
                SortOrder = request.SortOrder > 0 ? request.SortOrder : nextSortOrder
            };

            // Validate formula if provided
            if (!string.IsNullOrEmpty(request.Formula))
            {
                var validation = _formulaEngine.ValidateFormula(request.Formula, worksheet.Columns.ToList());
                if (!validation.IsValid)
                {
                    return BadRequest($"Invalid formula: {validation.ErrorMessage}");
                }
            }

            _context.EstimationWorksheetInstanceColumns.Add(column);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorksheet), new { tenantSlug, id = worksheetId },
                new ColumnDto
                {
                    Id = column.Id,
                    ColumnKey = column.ColumnKey,
                    ColumnName = column.ColumnName,
                    DataType = column.DataType,
                    Width = column.Width,
                    IsRequired = column.IsRequired,
                    IsReadOnly = column.IsReadOnly,
                    IsFrozen = column.IsFrozen,
                    IsHidden = column.IsHidden,
                    Formula = column.Formula,
                    DefaultValue = column.DefaultValue,
                    SelectOptions = column.SelectOptions,
                    Precision = column.Precision,
                    SortOrder = column.SortOrder
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding column to worksheet {WorksheetId}", worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a column.
    /// </summary>
    [HttpPut("{worksheetId:int}/columns/{columnId:int}")]
    public async Task<ActionResult<ColumnDto>> UpdateColumn(
        string tenantSlug,
        int worksheetId,
        int columnId,
        [FromBody] UpdateColumnRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Columns)
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only modify columns in worksheets in draft revisions");

            var column = worksheet.Columns.FirstOrDefault(c => c.Id == columnId && !c.IsDeleted);
            if (column == null)
                return NotFound($"Column {columnId} not found");

            // Validate new formula if provided
            if (request.Formula != null && !string.IsNullOrEmpty(request.Formula))
            {
                var validation = _formulaEngine.ValidateFormula(request.Formula, worksheet.Columns.ToList());
                if (!validation.IsValid)
                {
                    return BadRequest($"Invalid formula: {validation.ErrorMessage}");
                }
            }

            column.ColumnName = request.ColumnName ?? column.ColumnName;
            column.DataType = request.DataType ?? column.DataType;
            column.Width = request.Width ?? column.Width;
            column.IsRequired = request.IsRequired ?? column.IsRequired;
            column.IsReadOnly = request.IsReadOnly ?? column.IsReadOnly;
            column.IsFrozen = request.IsFrozen ?? column.IsFrozen;
            column.IsHidden = request.IsHidden ?? column.IsHidden;
            column.Formula = request.Formula ?? column.Formula;
            column.DefaultValue = request.DefaultValue ?? column.DefaultValue;
            column.SelectOptions = request.SelectOptions ?? column.SelectOptions;
            column.Precision = request.Precision ?? column.Precision;
            column.SortOrder = request.SortOrder ?? column.SortOrder;

            await _context.SaveChangesAsync();

            // If formula changed, recalculate all rows
            if (request.Formula != null)
            {
                await _calculationService.RecalculateWorksheetAsync(worksheetId);
            }

            return Ok(new ColumnDto
            {
                Id = column.Id,
                ColumnKey = column.ColumnKey,
                ColumnName = column.ColumnName,
                DataType = column.DataType,
                Width = column.Width,
                IsRequired = column.IsRequired,
                IsReadOnly = column.IsReadOnly,
                IsFrozen = column.IsFrozen,
                IsHidden = column.IsHidden,
                Formula = column.Formula,
                DefaultValue = column.DefaultValue,
                SelectOptions = column.SelectOptions,
                Precision = column.Precision,
                SortOrder = column.SortOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating column {ColumnId} in worksheet {WorksheetId}",
                columnId, worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a column.
    /// </summary>
    [HttpDelete("{worksheetId:int}/columns/{columnId:int}")]
    public async Task<IActionResult> DeleteColumn(
        string tenantSlug,
        int worksheetId,
        int columnId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheet = await _context.EstimationWorksheets
                .Include(w => w.Package)
                    .ThenInclude(p => p!.Revision)
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {worksheetId} not found");

            if (worksheet.Package?.Revision?.Status != "Draft")
                return BadRequest("Can only delete columns in worksheets in draft revisions");

            var column = await _context.EstimationWorksheetInstanceColumns
                .FirstOrDefaultAsync(c => c.Id == columnId && c.WorksheetId == worksheetId && !c.IsDeleted);

            if (column == null)
                return NotFound($"Column {columnId} not found");

            column.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting column {ColumnId} in worksheet {WorksheetId}",
                columnId, worksheetId);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

    #region Calculations

    /// <summary>
    /// Recalculate worksheet totals.
    /// </summary>
    [HttpPost("{id:int}/recalculate")]
    public async Task<ActionResult<WorksheetSummary>> RecalculateWorksheet(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheet = await _context.EstimationWorksheets
                .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {id} not found");

            await _calculationService.RecalculateWorksheetAsync(id);
            var summary = await _calculationService.GetWorksheetSummaryAsync(id);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating worksheet {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get column totals for a worksheet.
    /// </summary>
    [HttpGet("{id:int}/totals")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetColumnTotals(string tenantSlug, int id)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var worksheet = await _context.EstimationWorksheets
                .FirstOrDefaultAsync(w => w.Id == id && w.CompanyId == companyId && !w.IsDeleted);

            if (worksheet == null)
                return NotFound($"Worksheet {id} not found");

            var totals = await _calculationService.GetColumnTotalsAsync(id);

            return Ok(totals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting totals for worksheet {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion

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

    private async Task CopyColumnsFromTemplate(int templateId, int worksheetId)
    {
        var templateColumns = await _context.EstimationWorksheetColumns
            .Where(c => c.WorksheetTemplateId == templateId && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        foreach (var templateCol in templateColumns)
        {
            var column = new EstimationWorksheetInstanceColumn
            {
                WorksheetId = worksheetId,
                ColumnKey = templateCol.ColumnKey,
                ColumnName = templateCol.DisplayName,
                DataType = templateCol.DataType,
                Width = templateCol.Width,
                IsRequired = templateCol.IsRequired,
                IsReadOnly = !templateCol.IsEditable,
                IsFrozen = templateCol.IsFrozen,
                IsHidden = !templateCol.IsVisible,
                Formula = templateCol.Formula,
                DefaultValue = null,
                SelectOptions = templateCol.SelectOptions,
                Precision = templateCol.DecimalPlaces,
                SortOrder = templateCol.DisplayOrder,
                LinkToCatalogue = templateCol.LinkToCatalogue,
                CatalogueField = templateCol.CatalogueField,
                AutoPopulateFromCatalogue = templateCol.AutoPopulateFromCatalogue
            };

            _context.EstimationWorksheetInstanceColumns.Add(column);
        }

        await _context.SaveChangesAsync();
    }

    private async Task LogRowChange(int worksheetId, int rowId, string action, int userId, string? oldValue, string? newValue)
    {
        var change = new EstimationWorksheetChange
        {
            WorksheetId = worksheetId,
            RowId = rowId,
            ChangeType = action,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = userId,
            ChangedDate = DateTime.UtcNow
        };

        _context.EstimationWorksheetChanges.Add(change);
        await _context.SaveChangesAsync();
    }

    #endregion
}

#region DTOs

public class WorksheetDetailDto
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public string? PackageName { get; set; }
    public string? RevisionLetter { get; set; }
    public int? TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string WorksheetType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public IList<ColumnDto> Columns { get; set; } = new List<ColumnDto>();
    public IList<RowDto> Rows { get; set; } = new List<RowDto>();
}

public class ColumnDto
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

public class RowDto
{
    public int Id { get; set; }
    public string RowData { get; set; } = "{}";
    public int SortOrder { get; set; }
    public bool IsGroupHeader { get; set; }
    public string? GroupName { get; set; }
    public int? ParentRowId { get; set; }
    public decimal? CalculatedTotal { get; set; }
}

public class CreateWorksheetRequest
{
    public int PackageId { get; set; }
    public int? TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WorksheetType { get; set; }
    public int? SortOrder { get; set; }
    public IList<CreateColumnRequest>? Columns { get; set; }
}

public class UpdateWorksheetRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}

public class CreateRowRequest
{
    public string? RowData { get; set; }
    public int? SortOrder { get; set; }
    public bool IsGroupHeader { get; set; }
    public string? GroupName { get; set; }
    public int? ParentRowId { get; set; }
}

public class UpdateRowRequest
{
    public string? RowData { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsGroupHeader { get; set; }
    public string? GroupName { get; set; }
}

public class BulkRowsRequest
{
    public IList<BulkRowItem> Rows { get; set; } = new List<BulkRowItem>();
}

public class BulkRowItem
{
    public int? Id { get; set; }
    public string? RowData { get; set; }
    public int? SortOrder { get; set; }
}

public class CreateColumnRequest
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
    public int SortOrder { get; set; }
}

public class UpdateColumnRequest
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

#endregion
