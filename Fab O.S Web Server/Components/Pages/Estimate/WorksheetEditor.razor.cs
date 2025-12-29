using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using FabOS.WebServer.Services.Estimate;
using System.Security.Claims;
using System.Text.Json;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class WorksheetEditor : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext Context { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IEstimationCalculationService CalculationService { get; set; } = default!;
    [Inject] private IFormulaEngine FormulaEngine { get; set; } = default!;
    [Inject] private ICatalogueMatchingService CatalogueMatchingService { get; set; } = default!;
    [Inject] private ILogger<WorksheetEditor> Logger { get; set; } = default!;

    private bool isLoading = true;
    private string? errorMessage;

    private EstimationWorksheet? worksheet;
    private List<EstimationWorksheetColumn> allColumns = new();
    private List<EstimationWorksheetColumn> visibleColumns = new();
    private List<EstimationWorksheetRow> dataRows = new();
    private Dictionary<string, decimal> columnTotals = new();

    // Edit state
    private bool isDraft = false;
    private HashSet<int> selectedRows = new();
    private bool selectAllRows = false;

    // Column manager
    private bool showColumnManager = false;

    // Catalogue picker state
    private bool showCataloguePicker = false;
    private EstimationWorksheetRow? cataloguePickerRow = null;
    private string? cataloguePickerMatchValue = null;

    // Authentication state
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst("user_id") ?? user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }

            var companyIdClaim = user.FindFirst("company_id") ?? user.FindFirst("CompanyId");
            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            {
                currentCompanyId = companyId;
            }
        }

        if (currentUserId == 0 || currentCompanyId == 0)
        {
            Logger.LogWarning("User is not authenticated or missing required claims for WorksheetEditor page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadWorksheet();
    }

    private async Task LoadWorksheet()
    {
        try
        {
            isLoading = true;

            worksheet = await Context.EstimationWorksheets
                .Include(w => w.EstimationRevisionPackage)
                    .ThenInclude(p => p!.EstimationRevision)
                        .ThenInclude(r => r!.Estimation)
                .Include(w => w.WorksheetTemplate)
                    .ThenInclude(t => t!.Columns.Where(c => !c.IsDeleted))
                .Include(w => w.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(w => w.Id == Id && w.CompanyId == currentCompanyId);

            if (worksheet == null)
            {
                errorMessage = "Worksheet not found.";
                return;
            }

            // Check if editable (only draft revisions)
            isDraft = worksheet.EstimationRevisionPackage?.EstimationRevision?.Status == "Draft";

            // Load columns from template
            if (worksheet.WorksheetTemplate != null)
            {
                allColumns = worksheet.WorksheetTemplate.Columns
                    .Where(c => !c.IsDeleted)
                    .OrderBy(c => c.DisplayOrder)
                    .ToList();
            }
            else
            {
                allColumns = new List<EstimationWorksheetColumn>();
            }

            visibleColumns = allColumns.Where(c => c.IsVisible).ToList();

            // Load rows
            dataRows = worksheet.Rows.Where(r => !r.IsDeleted).OrderBy(r => r.RowNumber).ToList();

            // Calculate column totals
            CalculateColumnTotals();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading worksheet {Id}", Id);
            errorMessage = "Error loading worksheet. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void CalculateColumnTotals()
    {
        columnTotals.Clear();

        foreach (var column in visibleColumns.Where(c => c.DataType is "number" or "currency" or "computed"))
        {
            decimal total = 0;
            foreach (var row in dataRows.Where(r => !r.IsGroupHeader))
            {
                var value = GetCellValue(row, column.ColumnKey);
                if (decimal.TryParse(value?.ToString(), out var numValue))
                {
                    total += numValue;
                }
            }
            columnTotals[column.ColumnKey] = total;
        }
    }

    private object? GetCellValue(EstimationWorksheetRow row, string columnKey)
    {
        if (string.IsNullOrEmpty(row.RowData))
            return null;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(row.RowData);
            if (data != null && data.TryGetValue(columnKey, out var element))
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => element.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error parsing row data for row {RowId}", row.Id);
        }

        return null;
    }

    private void SetCellValue(EstimationWorksheetRow row, string columnKey, object? value)
    {
        Dictionary<string, object?> data;

        if (string.IsNullOrEmpty(row.RowData))
        {
            data = new Dictionary<string, object?>();
        }
        else
        {
            try
            {
                data = JsonSerializer.Deserialize<Dictionary<string, object?>>(row.RowData) ?? new();
            }
            catch
            {
                data = new Dictionary<string, object?>();
            }
        }

        data[columnKey] = value;
        row.RowData = JsonSerializer.Serialize(data);
    }

    private async Task UpdateCell(EstimationWorksheetRow row, string columnKey, string? value)
    {
        try
        {
            // Get column for type conversion
            var column = allColumns.FirstOrDefault(c => c.ColumnKey == columnKey);
            object? typedValue = value;

            if (column != null)
            {
                typedValue = column.DataType switch
                {
                    "Number" or "Currency" or "number" or "currency" => decimal.TryParse(value, out var d) ? d : 0m,
                    "Checkbox" or "checkbox" => value?.ToLower() == "true",
                    _ => value
                };
            }

            SetCellValue(row, columnKey, typedValue);

            // Recalculate computed columns for this row
            foreach (var computedCol in allColumns.Where(c => !string.IsNullOrEmpty(c.Formula)))
            {
                var result = FormulaEngine.EvaluateFormula(computedCol.Formula!, row, dataRows, allColumns);
                if (result.HasValue)
                {
                    SetCellValue(row, computedCol.ColumnKey, result.Value);
                }
            }

            row.LastModifiedBy = currentUserId;
            row.LastModified = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            // Recalculate totals
            CalculateColumnTotals();

            Logger.LogDebug("Updated cell {ColumnKey} in row {RowId}", columnKey, row.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating cell {ColumnKey} in row {RowId}", columnKey, row.Id);
        }
    }

    private string FormatCellValue(object? value, string dataType, int? precision)
    {
        if (value == null)
            return "";

        return dataType switch
        {
            "currency" => decimal.TryParse(value.ToString(), out var c) ? c.ToString("C") : value.ToString() ?? "",
            "number" or "computed" => decimal.TryParse(value.ToString(), out var n)
                ? n.ToString($"N{precision ?? 2}")
                : value.ToString() ?? "",
            "date" => DateTime.TryParse(value.ToString(), out var d) ? d.ToString("MMM dd, yyyy") : value.ToString() ?? "",
            "checkbox" => value.ToString()?.ToLower() == "true" ? "Yes" : "No",
            _ => value.ToString() ?? ""
        };
    }

    private async Task AddRow()
    {
        if (worksheet == null || !isDraft)
            return;

        try
        {
            var nextRowNumber = dataRows.Any() ? dataRows.Max(r => r.RowNumber) + 1 : 1;

            var row = new EstimationWorksheetRow
            {
                CompanyId = currentCompanyId,
                EstimationWorksheetId = worksheet.Id,
                RowNumber = nextRowNumber,
                RowData = "{}",
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            // Initialize with default values
            var defaultData = new Dictionary<string, object?>();
            foreach (var column in allColumns)
            {
                if (!string.IsNullOrEmpty(column.Placeholder))
                {
                    defaultData[column.ColumnKey] = column.Placeholder;
                }
            }
            row.RowData = JsonSerializer.Serialize(defaultData);

            Context.EstimationWorksheetRows.Add(row);
            await Context.SaveChangesAsync();

            dataRows.Add(row);
            Logger.LogInformation("Added row {RowId} to worksheet {WorksheetId}", row.Id, worksheet.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding row to worksheet {WorksheetId}", worksheet?.Id);
            errorMessage = "Error adding row. Please try again.";
        }
    }

    private async Task DeleteRow(int rowId)
    {
        try
        {
            var row = dataRows.FirstOrDefault(r => r.Id == rowId);
            if (row == null)
                return;

            row.IsDeleted = true;
            row.LastModifiedBy = currentUserId;
            row.LastModified = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            dataRows.Remove(row);
            selectedRows.Remove(rowId);

            CalculateColumnTotals();

            Logger.LogInformation("Deleted row {RowId}", rowId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting row {RowId}", rowId);
            errorMessage = "Error deleting row. Please try again.";
        }
    }

    private async Task DeleteSelectedRows()
    {
        if (!selectedRows.Any())
            return;

        try
        {
            var deletedCount = selectedRows.Count;
            foreach (var rowId in selectedRows.ToList())
            {
                var row = dataRows.FirstOrDefault(r => r.Id == rowId);
                if (row != null)
                {
                    row.IsDeleted = true;
                    row.LastModifiedBy = currentUserId;
                    row.LastModified = DateTime.UtcNow;
                    dataRows.Remove(row);
                }
            }

            await Context.SaveChangesAsync();

            selectedRows.Clear();
            selectAllRows = false;

            CalculateColumnTotals();

            Logger.LogInformation("Deleted {Count} rows from worksheet {WorksheetId}", deletedCount, worksheet?.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting selected rows");
            errorMessage = "Error deleting rows. Please try again.";
        }
    }

    private void ToggleRowSelection(int rowId)
    {
        if (selectedRows.Contains(rowId))
        {
            selectedRows.Remove(rowId);
        }
        else
        {
            selectedRows.Add(rowId);
        }

        selectAllRows = selectedRows.Count == dataRows.Count(r => !r.IsGroupHeader);
    }

    private void OnSelectAllRowsChanged()
    {
        if (selectAllRows)
        {
            selectedRows = dataRows.Where(r => !r.IsGroupHeader).Select(r => r.Id).ToHashSet();
        }
        else
        {
            selectedRows.Clear();
        }
    }

    private async Task RecalculateWorksheet()
    {
        if (worksheet == null)
            return;

        try
        {
            await CalculationService.RecalculateWorksheetAsync(worksheet.Id);
            await LoadWorksheet();

            Logger.LogInformation("Recalculated worksheet {WorksheetId}", worksheet.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error recalculating worksheet {WorksheetId}", worksheet?.Id);
            errorMessage = "Error recalculating worksheet. Please try again.";
        }
    }

    #region Column Management

    private void AddColumn()
    {
        if (worksheet?.WorksheetTemplateId == null) return;

        var nextDisplayOrder = allColumns.Any() ? allColumns.Max(c => c.DisplayOrder) + 1 : 0;
        var columnKey = $"col_{Guid.NewGuid().ToString()[..8]}";

        var column = new EstimationWorksheetColumn
        {
            WorksheetTemplateId = worksheet.WorksheetTemplateId.Value,
            ColumnKey = columnKey,
            DisplayName = "New Column",
            DataType = "Text",
            Width = 120,
            DisplayOrder = nextDisplayOrder,
            IsVisible = true,
            IsEditable = true
        };

        allColumns.Add(column);
        visibleColumns = allColumns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
    }

    private void ToggleColumnVisibility(EstimationWorksheetColumn column)
    {
        column.IsVisible = !column.IsVisible;
        visibleColumns = allColumns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
    }

    private void DeleteColumn(EstimationWorksheetColumn column)
    {
        column.IsDeleted = true;
        allColumns.Remove(column);
        visibleColumns = allColumns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
    }

    private async Task SaveColumnChanges()
    {
        if (worksheet?.WorksheetTemplateId == null)
            return;

        try
        {
            // Add new columns to context
            foreach (var column in allColumns.Where(c => c.Id == 0))
            {
                Context.EstimationWorksheetColumns.Add(column);
            }

            // Mark deleted columns
            var deletedColumns = await Context.EstimationWorksheetColumns
                .Where(c => c.WorksheetTemplateId == worksheet.WorksheetTemplateId && !allColumns.Select(ac => ac.Id).Contains(c.Id))
                .ToListAsync();

            foreach (var col in deletedColumns)
            {
                col.IsDeleted = true;
            }

            await Context.SaveChangesAsync();

            showColumnManager = false;
            visibleColumns = allColumns.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();

            CalculateColumnTotals();

            Logger.LogInformation("Saved column changes for worksheet {WorksheetId}", worksheet.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving column changes for worksheet {WorksheetId}", worksheet?.Id);
            errorMessage = "Error saving column changes. Please try again.";
        }
    }

    #endregion

    #region Catalogue Matching

    private void ShowCataloguePicker(EstimationWorksheetRow row)
    {
        cataloguePickerRow = row;

        // Try to get a match value from the row data (Description or Profile)
        var descValue = GetCellValue(row, "description")?.ToString();
        var profileValue = GetCellValue(row, "profile")?.ToString();
        cataloguePickerMatchValue = !string.IsNullOrEmpty(descValue) ? descValue : profileValue;

        showCataloguePicker = true;
    }

    private void CloseCataloguePicker()
    {
        showCataloguePicker = false;
        cataloguePickerRow = null;
        cataloguePickerMatchValue = null;
    }

    private async Task OnCatalogueItemSelected(CatalogueItem item)
    {
        if (cataloguePickerRow == null) return;

        try
        {
            // Update the row with the catalogue item reference
            cataloguePickerRow.CatalogueItemId = item.Id;
            cataloguePickerRow.MatchStatus = "ManuallyAssigned";
            cataloguePickerRow.MatchConfidence = 1.0m;

            // Auto-populate catalogue-linked columns
            await AutoPopulateCatalogueColumns(cataloguePickerRow, item);

            cataloguePickerRow.LastModifiedBy = currentUserId;
            cataloguePickerRow.LastModified = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            // Recalculate computed columns
            foreach (var computedCol in allColumns.Where(c => !string.IsNullOrEmpty(c.Formula)))
            {
                var result = FormulaEngine.EvaluateFormula(computedCol.Formula!, cataloguePickerRow, dataRows, allColumns);
                if (result.HasValue)
                {
                    SetCellValue(cataloguePickerRow, computedCol.ColumnKey, result.Value);
                }
            }

            await Context.SaveChangesAsync();
            CalculateColumnTotals();

            Logger.LogInformation("Manually assigned catalogue item {ItemId} to row {RowId}", item.Id, cataloguePickerRow.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error assigning catalogue item to row {RowId}", cataloguePickerRow?.Id);
            errorMessage = "Error assigning catalogue item. Please try again.";
        }
        finally
        {
            CloseCataloguePicker();
        }
    }

    private async Task AutoPopulateCatalogueColumns(EstimationWorksheetRow row, CatalogueItem item)
    {
        // Get all catalogue-linked columns that should auto-populate
        var linkedColumns = allColumns.Where(c => c.LinkToCatalogue && c.AutoPopulateFromCatalogue && !string.IsNullOrEmpty(c.CatalogueField));

        foreach (var col in linkedColumns)
        {
            var value = CatalogueMatchingService.GetFieldValue(item, col.CatalogueField!);
            if (value != null)
            {
                SetCellValue(row, col.ColumnKey, value);
            }
        }

        await Task.CompletedTask;
    }

    private int GetUnmatchedRowCount()
    {
        return dataRows.Count(r => !r.IsGroupHeader && r.MatchStatus == "Unmatched");
    }

    private async Task ReviewUnmatchedRows()
    {
        // Navigate to first unmatched row or show picker for first unmatched
        var firstUnmatched = dataRows.FirstOrDefault(r => !r.IsGroupHeader && r.MatchStatus == "Unmatched");
        if (firstUnmatched != null)
        {
            ShowCataloguePicker(firstUnmatched);
        }
        await Task.CompletedTask;
    }

    #endregion

    private void GoBack()
    {
        if (worksheet?.EstimationRevisionPackageId != 0)
        {
            Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/{worksheet.EstimationRevisionPackageId}");
        }
        else
        {
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations");
        }
    }

    private string GetWorksheetTypeBadgeClass(string type)
    {
        return type switch
        {
            "MaterialCosts" => "bg-primary",
            "LaborCosts" => "bg-info",
            "WeldingCosts" => "bg-warning text-dark",
            "Equipment" => "bg-success",
            _ => "bg-secondary"
        };
    }

    #region IToolbarActionProvider Implementation

    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();

        group.PrimaryActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Back",
                Text = "Back to Package",
                Icon = "fas fa-arrow-left",
                ActionFunc = async () => { GoBack(); await Task.CompletedTask; },
                Style = ToolbarActionStyle.Secondary
            }
        };

        if (isDraft)
        {
            group.PrimaryActions.Add(new()
            {
                Label = "Add Row",
                Text = "Add Row",
                Icon = "fas fa-plus",
                ActionFunc = AddRow,
                Style = ToolbarActionStyle.Primary
            });

            if (selectedRows.Any())
            {
                group.PrimaryActions.Add(new()
                {
                    Label = "Delete",
                    Text = "Delete Selected",
                    Icon = "fas fa-trash",
                    ActionFunc = DeleteSelectedRows,
                    Style = ToolbarActionStyle.Danger
                });
            }
        }

        group.MenuActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Recalculate",
                Text = "Recalculate All",
                Icon = "fas fa-calculator",
                ActionFunc = RecalculateWorksheet
            }
        };

        if (isDraft)
        {
            group.MenuActions.Add(new()
            {
                Label = "Columns",
                Text = "Manage Columns",
                Icon = "fas fa-columns",
                ActionFunc = async () => { showColumnManager = true; StateHasChanged(); await Task.CompletedTask; }
            });
        }

        group.RelatedActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Package",
                Text = "View Package",
                Icon = "fas fa-box",
                ActionFunc = async () => {
                    if (worksheet?.EstimationRevisionPackageId != 0)
                    {
                        Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/{worksheet.EstimationRevisionPackageId}");
                    }
                    await Task.CompletedTask;
                }
            }
        };

        if (worksheet?.EstimationRevisionPackage?.EstimationRevision?.EstimationId != null)
        {
            group.RelatedActions.Add(new()
            {
                Label = "Estimation",
                Text = "View Estimation",
                Icon = "fas fa-file-invoice-dollar",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{worksheet.EstimationRevisionPackage.EstimationRevision.EstimationId}");
                    await Task.CompletedTask;
                }
            });
        }

        return group;
    }

    #endregion
}
