using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Estimate;
using System.Security.Claims;
using System.Text.RegularExpressions;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate.Templates;

public partial class TemplateDesigner : ComponentBase, IToolbarActionProvider, IAsyncDisposable
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext Context { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<TemplateDesigner> Logger { get; set; } = default!;

    private bool isLoading = true;
    private string? errorMessage;

    private EstimationWorksheetTemplate? template;
    private List<EstimationWorksheetColumn> columns = new();

    // Sidebar state
    private bool sidebarVisible = true;
    private TemplateColumnSidebar? columnSidebar;

    // Drag-drop state
    private bool isDraggingFromSidebar = false;
    private bool isDraggingColumn = false;
    private int? draggingColumnId = null;
    private TemplateColumnSidebar.FieldDefinition? draggedFieldFromSidebar;

    // Column selection state
    private int? selectedColumnId;
    private EstimationWorksheetColumn? selectedColumn;
    private bool showPropertiesPanel = false;

    // Spreadsheet settings
    private int sampleRowCount = 5;
    private bool hasUnsavedChanges = false;
    private bool isEditMode = false;

    // Column resizing
    private bool isResizing = false;
    private int? resizingColumnId = null;

    // Catalogue fields for picker
    private List<CatalogueFieldGroup> catalogueFieldGroups = CatalogueFieldRegistry.GetAvailableFields();

    // Help dialog
    private bool showFormulaHelp = false;

    // Formula field focus state (for click-to-insert column keys)
    private bool isFormulaFieldFocused = false;

    // Document header type dropdown
    private bool showTypeDropdown = false;

    // Authentication state
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // JS interop reference
    private DotNetObjectReference<TemplateDesigner>? dotNetRef;
    private IJSObjectReference? sidebarHelpersModule;

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
            Logger.LogWarning("User is not authenticated or missing required claims for TemplateDesigner page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadTemplate();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotNetRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("templateDesigner.initialize", dotNetRef);

            // Load sidebar helpers module for body class management
            try
            {
                sidebarHelpersModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/template-sidebar-helpers.js");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load template-sidebar-helpers.js module");
            }
        }
    }

    private async Task LoadTemplate()
    {
        try
        {
            isLoading = true;

            template = await Context.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == Id && t.CompanyId == currentCompanyId && !t.IsDeleted);

            if (template == null)
            {
                errorMessage = "Template not found.";
                return;
            }

            columns = template.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading template {Id}", Id);
            errorMessage = "Error loading template. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    #region Sidebar and UI State

    private void ToggleSidebar()
    {
        sidebarVisible = !sidebarVisible;
    }

    private async Task SelectColumn(EstimationWorksheetColumn column)
    {
        // If formula field is focused, insert column key instead of selecting
        if (isFormulaFieldFocused && selectedColumn != null)
        {
            InsertColumnKeyIntoFormula(column.ColumnKey);
            return;
        }

        // Normal selection behavior
        selectedColumnId = column.Id;
        selectedColumn = column;
        showPropertiesPanel = true;
        await UpdatePropertiesPanelBodyClass(true);
    }

    private async Task EditColumnInline(EstimationWorksheetColumn column)
    {
        await SelectColumn(column);
        // The properties panel will open for editing
    }

    private async Task ClosePropertiesPanel()
    {
        showPropertiesPanel = false;
        selectedColumnId = null;
        selectedColumn = null;
        await UpdatePropertiesPanelBodyClass(false);
    }

    private async Task UpdatePropertiesPanelBodyClass(bool isVisible)
    {
        try
        {
            // Use direct JS to update body class - more reliable than module import
            if (isVisible)
            {
                await JSRuntime.InvokeVoidAsync("eval", "document.body.classList.add('properties-panel-open')");
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("eval", "document.body.classList.remove('properties-panel-open')");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update properties panel body class");
        }
    }

    private void MarkUnsaved()
    {
        hasUnsavedChanges = true;
    }

    #endregion

    #region Document Header Type Dropdown

    private void ToggleTypeDropdown()
    {
        if (!isEditMode) return;
        showTypeDropdown = !showTypeDropdown;
    }

    private void SelectType(string worksheetType)
    {
        if (template != null && template.WorksheetType != worksheetType)
        {
            template.WorksheetType = worksheetType;
            MarkUnsaved();
        }
        showTypeDropdown = false;
    }

    private string GetTypeBadgeClass()
    {
        return template?.WorksheetType?.ToLower() switch
        {
            "materialcosts" => "material",
            "laborcosts" => "labor",
            "weldingcosts" => "welding",
            "equipment" => "equipment",
            "custom" => "custom",
            _ => "custom"
        };
    }

    private string GetTypeDisplayName()
    {
        return template?.WorksheetType switch
        {
            "MaterialCosts" => "Material Costs",
            "LaborCosts" => "Labor Costs",
            "WeldingCosts" => "Welding Costs",
            "Equipment" => "Equipment",
            "Custom" => "Custom",
            _ => template?.WorksheetType ?? "Unknown"
        };
    }

    #endregion

    #region Formula Field Focus Handling

    private void OnFormulaFocus()
    {
        isFormulaFieldFocused = true;
    }

    private void OnFormulaBlur()
    {
        // Small delay to allow click to register before blur
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            await InvokeAsync(() =>
            {
                isFormulaFieldFocused = false;
                StateHasChanged();
            });
        });
    }

    private void InsertColumnKeyIntoFormula(string columnKey)
    {
        if (selectedColumn == null) return;

        // Append column key to existing formula
        if (string.IsNullOrEmpty(selectedColumn.Formula))
        {
            selectedColumn.Formula = columnKey;
        }
        else
        {
            selectedColumn.Formula += " " + columnKey;
        }

        MarkUnsaved();
    }

    #endregion

    #region Drag-Drop from Sidebar

    private void HandleSidebarFieldDragStart(TemplateColumnSidebar.FieldDefinition field)
    {
        isDraggingFromSidebar = true;
        draggedFieldFromSidebar = field;
        Logger.LogDebug("Sidebar drag start: {FieldName}", field.FieldName);
    }

    private void HandleSidebarFieldDragEnd()
    {
        isDraggingFromSidebar = false;
        draggedFieldFromSidebar = null;
        Logger.LogDebug("Sidebar drag end");
    }

    private void HandleDragOver(DragEventArgs e)
    {
        // Just allow drop - visual feedback handled by CSS
    }

    private async Task HandleDrop(DragEventArgs e)
    {
        if (draggedFieldFromSidebar != null)
        {
            // Add the field as a new column at the end
            await AddColumnFromField(draggedFieldFromSidebar, columns.Count);
        }

        isDraggingFromSidebar = false;
        draggedFieldFromSidebar = null;
    }

    private void HandleAddZoneDragOver(DragEventArgs e)
    {
        // Visual feedback handled by CSS class
    }

    private async Task HandleAddZoneDrop(DragEventArgs e)
    {
        if (draggedFieldFromSidebar != null)
        {
            await AddColumnFromField(draggedFieldFromSidebar, columns.Count);
        }
        else if (draggingColumnId.HasValue)
        {
            // Moving an existing column to the end
            await ReorderColumn(draggingColumnId.Value, columns.Count - 1);
        }

        isDraggingFromSidebar = false;
        isDraggingColumn = false;
        draggedFieldFromSidebar = null;
        draggingColumnId = null;
    }

    private async Task AddColumnFromField(TemplateColumnSidebar.FieldDefinition field, int displayOrder)
    {
        if (template == null) return;

        try
        {
            var columnKey = GenerateColumnKey(field.DisplayName);

            var column = new EstimationWorksheetColumn
            {
                WorksheetTemplateId = template.Id,
                ColumnKey = columnKey,
                DisplayName = field.DisplayName,
                DataType = MapColumnType(field.DataType),
                Width = GetDefaultWidth(field.DataType),
                LinkToCatalogue = field.IsCatalogueField,
                CatalogueField = field.CatalogueFieldMapping,
                AutoPopulateFromCatalogue = field.IsCatalogueField,
                IsEditable = !field.IsCatalogueField && field.DataType != "computed",
                Formula = field.DefaultFormula,
                SelectOptions = field.DefaultOptions,
                DecimalPlaces = field.DataType is "number" or "currency" or "computed" ? 2 : null,
                DisplayOrder = displayOrder
            };

            // Shift existing columns after this position
            foreach (var existingCol in columns.Where(c => c.DisplayOrder >= displayOrder))
            {
                existingCol.DisplayOrder++;
            }

            Context.EstimationWorksheetColumns.Add(column);
            await Context.SaveChangesAsync();

            columns.Add(column);
            columns = columns.OrderBy(c => c.DisplayOrder).ToList();

            hasUnsavedChanges = false;
            Logger.LogInformation("Added column {ColumnKey} from sidebar at position {Position}", column.ColumnKey, displayOrder);

            // Select the new column
            await SelectColumn(column);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding column from sidebar");
            errorMessage = "Error adding column. Please try again.";
        }
    }

    // JS interop method for handling field drop from sidebar
    [JSInvokable]
    public async Task HandleFieldDropped(TemplateColumnSidebar.FieldDefinition field, int dropIndex)
    {
        await AddColumnFromField(field, dropIndex);
        StateHasChanged();
    }

    #endregion

    #region Column Drag-Drop for Reordering

    private void HandleColumnDragStart(DragEventArgs e, EstimationWorksheetColumn column)
    {
        isDraggingColumn = true;
        draggingColumnId = column.Id;
        Logger.LogDebug("Column drag start: {ColumnId}", column.Id);
    }

    private void HandleColumnDragEnd(DragEventArgs e)
    {
        isDraggingColumn = false;
        draggingColumnId = null;
        Logger.LogDebug("Column drag end");
    }

    private async Task ReorderColumn(int columnId, int newDisplayOrder)
    {
        var column = columns.FirstOrDefault(c => c.Id == columnId);
        if (column == null) return;

        var oldOrder = column.DisplayOrder;
        if (oldOrder == newDisplayOrder) return;

        // Shift other columns
        if (newDisplayOrder > oldOrder)
        {
            foreach (var col in columns.Where(c => c.DisplayOrder > oldOrder && c.DisplayOrder <= newDisplayOrder))
            {
                col.DisplayOrder--;
            }
        }
        else
        {
            foreach (var col in columns.Where(c => c.DisplayOrder >= newDisplayOrder && c.DisplayOrder < oldOrder))
            {
                col.DisplayOrder++;
            }
        }

        column.DisplayOrder = newDisplayOrder;
        columns = columns.OrderBy(c => c.DisplayOrder).ToList();

        await Context.SaveChangesAsync();
        hasUnsavedChanges = false;

        Logger.LogInformation("Reordered column {ColumnId} from {OldOrder} to {NewOrder}", columnId, oldOrder, newDisplayOrder);
    }

    // JS interop method for column reorder
    [JSInvokable]
    public async Task HandleColumnReorder(int columnId, int newIndex)
    {
        await ReorderColumn(columnId, newIndex);
        StateHasChanged();
    }

    #endregion

    #region Column Resizing

    private void StartColumnResize(MouseEventArgs e, EstimationWorksheetColumn column)
    {
        isResizing = true;
        resizingColumnId = column.Id;
        // Actual resize handling would require JS interop for mouse move tracking
    }

    #endregion

    #region Cell Preview Rendering

    private RenderFragment RenderCellPreview(EstimationWorksheetColumn column, int rowIndex)
    {
        return builder =>
        {
            var sampleData = GetSampleData(column, rowIndex);

            switch (column.DataType?.ToLower())
            {
                case "checkbox":
                    builder.OpenElement(0, "input");
                    builder.AddAttribute(1, "type", "checkbox");
                    builder.AddAttribute(2, "class", "form-check-input");
                    builder.AddAttribute(3, "disabled", true);
                    builder.AddAttribute(4, "checked", rowIndex % 2 == 0);
                    builder.CloseElement();
                    break;

                case "choice":
                case "select":
                    builder.OpenElement(0, "select");
                    builder.AddAttribute(1, "class", "form-select form-select-sm cell-preview-select");
                    builder.AddAttribute(2, "disabled", true);
                    builder.OpenElement(3, "option");
                    builder.AddContent(4, sampleData);
                    builder.CloseElement();
                    builder.CloseElement();
                    break;

                case "computed":
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "computed-cell");
                    builder.OpenElement(2, "i");
                    builder.AddAttribute(3, "class", "fas fa-function me-1");
                    builder.CloseElement();
                    builder.AddContent(4, column.Formula ?? "formula");
                    builder.CloseElement();
                    break;

                case "date":
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "date-cell");
                    builder.AddContent(2, DateTime.Now.AddDays(rowIndex).ToString("dd/MM/yyyy"));
                    builder.CloseElement();
                    break;

                case "datetime":
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "datetime-cell");
                    builder.AddContent(2, DateTime.Now.AddDays(rowIndex).ToString("dd/MM/yyyy HH:mm"));
                    builder.CloseElement();
                    break;

                case "currency":
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "currency-cell");
                    builder.AddContent(2, $"${(rowIndex + 1) * 123.45m:N2}");
                    builder.CloseElement();
                    break;

                case "number":
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "number-cell");
                    builder.AddContent(2, ((rowIndex + 1) * 10).ToString());
                    builder.CloseElement();
                    break;

                default:
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "text-cell");
                    builder.AddContent(2, sampleData);
                    builder.CloseElement();
                    break;
            }
        };
    }

    private string GetSampleData(EstimationWorksheetColumn column, int rowIndex)
    {
        // Generate sample data based on column type and name
        if (column.LinkToCatalogue && !string.IsNullOrEmpty(column.CatalogueField))
        {
            return column.CatalogueField switch
            {
                "ItemCode" => $"ITEM-{1000 + rowIndex}",
                "Description" => $"Sample Item {rowIndex + 1}",
                "Material" => rowIndex % 2 == 0 ? "Steel" : "Aluminium",
                "Category" => "Structural",
                "Mass_kg_m" => $"{(rowIndex + 1) * 12.5:F1}",
                "Width_mm" => $"{100 + rowIndex * 50}",
                "Height_mm" => $"{200 + rowIndex * 25}",
                "UnitCost" => $"${(rowIndex + 1) * 45.00m:N2}",
                _ => $"Value {rowIndex + 1}"
            };
        }

        if (!string.IsNullOrEmpty(column.SelectOptions))
        {
            var options = column.SelectOptions.Split(',');
            return options[rowIndex % options.Length].Trim();
        }

        return column.DataType?.ToLower() switch
        {
            "text" => $"Text {rowIndex + 1}",
            "multilinetext" or "multiline" => $"Multi-line text content for row {rowIndex + 1}...",
            "number" => $"{(rowIndex + 1) * 10}",
            "currency" => $"${(rowIndex + 1) * 100.00m:N2}",
            "date" => DateTime.Now.AddDays(rowIndex).ToString("dd/MM/yyyy"),
            "datetime" => DateTime.Now.AddDays(rowIndex).ToString("dd/MM/yyyy HH:mm"),
            _ => $"Value {rowIndex + 1}"
        };
    }

    #endregion

    #region Column Management

    private async Task DeleteSelectedColumn()
    {
        if (selectedColumn == null) return;

        try
        {
            selectedColumn.IsDeleted = true;
            await Context.SaveChangesAsync();

            columns.Remove(selectedColumn);
            ReorderColumns();

            Logger.LogInformation("Deleted column {ColumnId}", selectedColumn.Id);

            await ClosePropertiesPanel();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting column {ColumnId}", selectedColumn.Id);
            errorMessage = "Error deleting column. Please try again.";
        }
    }

    private async Task HandlePositionChange(ChangeEventArgs e)
    {
        if (selectedColumn == null) return;

        if (int.TryParse(e.Value?.ToString(), out var newPosition))
        {
            // Convert 1-based user input to 0-based DisplayOrder
            var newDisplayOrder = Math.Clamp(newPosition - 1, 0, columns.Count - 1);

            if (newDisplayOrder != selectedColumn.DisplayOrder)
            {
                await ReorderColumn(selectedColumn.Id, newDisplayOrder);
                MarkUnsaved();
            }
        }
    }

    private string GenerateColumnKey(string name)
    {
        var key = Regex.Replace(name.ToLower(), @"[^a-z0-9]+", "_").Trim('_');

        var baseKey = key;
        var counter = 1;
        while (columns.Any(c => c.ColumnKey == key))
        {
            key = $"{baseKey}_{counter++}";
        }

        return key;
    }

    private int GetDefaultWidth(string dataType)
    {
        return dataType.ToLower() switch
        {
            "text" => 150,
            "number" => 100,
            "currency" => 120,
            "computed" => 120,
            "select" or "choice" => 130,
            "date" or "datetime" => 120,
            "checkbox" => 80,
            "multiline" or "multilinetext" => 200,
            "image" => 100,
            "location" => 150,
            "lookup" => 150,
            "person" => 150,
            _ => 120
        };
    }

    private string MapColumnType(string formType)
    {
        return formType.ToLower() switch
        {
            "text" => "Text",
            "multiline" or "multilinetext" => "MultilineText",
            "number" => "Number",
            "currency" => "Currency",
            "date" => "Date",
            "datetime" => "DateTime",
            "checkbox" => "Checkbox",
            "choice" or "select" => "Choice",
            "multichoice" or "multiselect" => "MultiChoice",
            "image" => "Image",
            "location" => "Location",
            "lookup" => "Lookup",
            "person" => "Person",
            "computed" => "Computed",
            "catalogue" => "Catalogue",
            _ => "Text"
        };
    }

    private void ReorderColumns()
    {
        for (int i = 0; i < columns.Count; i++)
        {
            columns[i].DisplayOrder = i;
        }
    }

    private string GetColumnIcon(EstimationWorksheetColumn column)
    {
        if (column.LinkToCatalogue)
            return "fas fa-book";

        return column.DataType?.ToLower() switch
        {
            "text" => "fas fa-font",
            "multilinetext" or "multiline" => "fas fa-align-left",
            "number" => "fas fa-hashtag",
            "currency" => "fas fa-dollar-sign",
            "computed" => "fas fa-calculator",
            "choice" or "select" => "fas fa-list",
            "multichoice" or "multiselect" => "fas fa-check-double",
            "checkbox" => "fas fa-check-square",
            "date" => "fas fa-calendar",
            "datetime" => "fas fa-calendar-alt",
            "time" => "fas fa-clock",
            "lookup" => "fas fa-link",
            "person" => "fas fa-user",
            "image" => "fas fa-image",
            "location" => "fas fa-map-marker-alt",
            _ => "fas fa-columns"
        };
    }

    #endregion

    #region Navigation and Saving

    private void GoBack()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/templates");
    }

    private async Task EnterEditMode()
    {
        isEditMode = true;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task SaveAndExitEditMode()
    {
        await SaveTemplate();
        isEditMode = false;
        StateHasChanged();
    }

    private async Task SaveTemplate()
    {
        if (template == null || string.IsNullOrWhiteSpace(template.Name))
            return;

        try
        {
            // If setting as default, clear existing defaults for this type
            if (template.IsCompanyDefault)
            {
                var existingDefaults = await Context.EstimationWorksheetTemplates
                    .Where(t => t.CompanyId == currentCompanyId && t.WorksheetType == template.WorksheetType && t.IsCompanyDefault && t.Id != template.Id)
                    .ToListAsync();

                foreach (var existing in existingDefaults)
                {
                    existing.IsCompanyDefault = false;
                }
            }

            template.ModifiedBy = currentUserId;
            template.ModifiedDate = DateTime.UtcNow;

            await Context.SaveChangesAsync();
            hasUnsavedChanges = false;

            Logger.LogInformation("Saved template {TemplateId}", template.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving template {TemplateId}", template?.Id);
            errorMessage = "Error saving template. Please try again.";
        }
    }

    private async Task DeleteTemplate()
    {
        if (template == null)
            return;

        try
        {
            template.IsDeleted = true;
            template.ModifiedBy = currentUserId;
            template.ModifiedDate = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            Logger.LogInformation("Deleted template {TemplateId}", template.Id);

            GoBack();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting template {TemplateId}", template?.Id);
            errorMessage = "Error deleting template. Please try again.";
        }
    }

    #endregion

    #region IToolbarActionProvider Implementation

    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();

        group.PrimaryActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Back",
                Text = "Back",
                Icon = "fas fa-arrow-left",
                Tooltip = "Return to templates list",
                ActionFunc = async () => { GoBack(); await Task.CompletedTask; },
                Style = ToolbarActionStyle.Secondary
            },
            new()
            {
                Label = isEditMode ? "Save" : "Edit",
                Text = isEditMode ? "Save" : "Edit",
                Icon = isEditMode ? "fas fa-save" : "fas fa-edit",
                Tooltip = isEditMode ? "Save changes" : "Edit template",
                ActionFunc = isEditMode ? SaveAndExitEditMode : EnterEditMode,
                IsDisabled = template == null,
                Style = ToolbarActionStyle.Primary
            }
        };

        group.MenuActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Import Mapping",
                Text = "Configure Import Mapping",
                Icon = "fas fa-file-import",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/templates/{Id}/import-mapping");
                    await Task.CompletedTask;
                }
            },
            new()
            {
                Label = "Formula Help",
                Text = "Formula Reference",
                Icon = "fas fa-question-circle",
                ActionFunc = async () => { showFormulaHelp = true; StateHasChanged(); await Task.CompletedTask; }
            },
            new()
            {
                Label = "Duplicate",
                Text = "Duplicate Template",
                Icon = "fas fa-copy",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/templates");
                    await Task.CompletedTask;
                }
            },
            new()
            {
                Label = "Delete",
                Text = "Delete Template",
                Icon = "fas fa-trash",
                ActionFunc = DeleteTemplate,
                Style = ToolbarActionStyle.Danger
            }
        };

        group.RelatedActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Templates",
                Text = "All Templates",
                Icon = "fas fa-table",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/templates");
                    await Task.CompletedTask;
                }
            },
            new()
            {
                Label = "Estimations",
                Text = "View Estimations",
                Icon = "fas fa-file-invoice-dollar",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations");
                    await Task.CompletedTask;
                }
            }
        };

        return group;
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (dotNetRef != null)
        {
            await JSRuntime.InvokeVoidAsync("templateDesigner.dispose");
            dotNetRef.Dispose();
        }

        // Cleanup properties panel body class
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", "document.body.classList.remove('properties-panel-open')");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error during properties panel cleanup");
        }

        if (sidebarHelpersModule != null)
        {
            try
            {
                await sidebarHelpersModule.DisposeAsync();
            }
            catch
            {
                // Ignore errors during module disposal
            }
        }
    }

    #endregion
}
