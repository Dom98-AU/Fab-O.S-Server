using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using System.Security.Claims;
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate.Templates;

public partial class TemplateLibrary : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<TemplateLibrary> Logger { get; set; } = default!;

    private List<EstimationWorksheetTemplate> allTemplates = new();
    private List<EstimationWorksheetTemplate> filteredTemplates = new();
    private bool isLoading = true;
    private string searchTerm = "";
    private string? errorMessage;
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // View state
    private GenericViewSwitcher<EstimationWorksheetTemplate>.ViewType currentView = GenericViewSwitcher<EstimationWorksheetTemplate>.ViewType.Table;

    // Selection tracking
    private List<EstimationWorksheetTemplate> selectedTableItems = new();
    private List<EstimationWorksheetTemplate> selectedListItems = new();
    private List<EstimationWorksheetTemplate> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns
    private List<GenericTableView<EstimationWorksheetTemplate>.TableColumn<EstimationWorksheetTemplate>> tableColumns = new();

    protected override async Task OnInitializedAsync()
    {
        // Validate authentication and get user context
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
            Logger.LogWarning("User is not authenticated or missing required claims for TemplateLibrary page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        InitializeTableColumns();
        await LoadTemplates();
    }

    private void InitializeTableColumns()
    {
        tableColumns = new List<GenericTableView<EstimationWorksheetTemplate>.TableColumn<EstimationWorksheetTemplate>>
        {
            new() { Header = "Template Name", ValueSelector = item => item.Name, IsSortable = true, CssClass = "text-start" },
            new() { Header = "Type", ValueSelector = item => item.WorksheetType, IsSortable = true, CssClass = "text-center" },
            new() { Header = "Description", ValueSelector = item => item.Description ?? "—", IsSortable = true, CssClass = "text-start" },
            new() { Header = "Columns", ValueSelector = item => (item.Columns?.Count(c => !c.IsDeleted) ?? 0).ToString(), IsSortable = true, CssClass = "text-center" },
            new() { Header = "System", ValueSelector = item => item.IsSystemTemplate ? "Yes" : "No", IsSortable = true, CssClass = "text-center" },
            new() { Header = "Default", ValueSelector = item => item.IsDefault ? "Yes" : "No", IsSortable = true, CssClass = "text-center" },
            new() { Header = "Published", ValueSelector = item => item.IsPublished ? "Yes" : "No", IsSortable = true, CssClass = "text-center" }
        };

        managedColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                PropertyName = "Name",
                DisplayName = "Template Name",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 200,
                Order = 0
            },
            new ColumnDefinition
            {
                PropertyName = "WorksheetType",
                DisplayName = "Type",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 1
            },
            new ColumnDefinition
            {
                PropertyName = "Description",
                DisplayName = "Description",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 250,
                Order = 2
            },
            new ColumnDefinition
            {
                PropertyName = "ColumnsCount",
                DisplayName = "Columns",
                Type = ColumnType.Number,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 80,
                Order = 3
            },
            new ColumnDefinition
            {
                PropertyName = "IsSystemTemplate",
                DisplayName = "System",
                Type = ColumnType.Boolean,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 80,
                Order = 4
            },
            new ColumnDefinition
            {
                PropertyName = "IsDefault",
                DisplayName = "Default",
                Type = ColumnType.Boolean,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 80,
                Order = 5
            },
            new ColumnDefinition
            {
                PropertyName = "IsPublished",
                DisplayName = "Published",
                Type = ColumnType.Boolean,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 80,
                Order = 6
            }
        };
    }

    private async Task LoadTemplates()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // Load templates for this company (including system templates)
            allTemplates = await DbContext.EstimationWorksheetTemplates
                .Include(t => t.Columns.Where(c => !c.IsDeleted))
                .Where(t => (t.CompanyId == currentCompanyId || t.IsSystemTemplate) && !t.IsDeleted)
                .OrderBy(t => t.WorksheetType)
                .ThenBy(t => t.Name)
                .ToListAsync();

            FilterTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading templates for company {CompanyId}", currentCompanyId);
            errorMessage = "Error loading templates. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void FilterTemplates()
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            filteredTemplates = allTemplates;
        }
        else
        {
            var searchLower = searchTerm.ToLower();
            filteredTemplates = allTemplates.Where(t =>
                (t.Name?.ToLower().Contains(searchLower) ?? false) ||
                (t.Description?.ToLower().Contains(searchLower) ?? false) ||
                (t.WorksheetType?.ToLower().Contains(searchLower) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterTemplates();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<EstimationWorksheetTemplate>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleRowClick(EstimationWorksheetTemplate template)
    {
        NavigateToTemplate(template.Id);
    }

    private void HandleRowDoubleClick(EstimationWorksheetTemplate template)
    {
        NavigateToTemplate(template.Id);
    }

    private void NavigateToTemplate(int templateId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/templates/{templateId}");
    }

    private void HandleTableSelectionChanged(List<EstimationWorksheetTemplate> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<EstimationWorksheetTemplate> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<EstimationWorksheetTemplate> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void CreateNew()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/templates/0");
    }

    private async Task DeleteTemplates()
    {
        var selected = GetSelectedTemplates();
        if (!selected.Any()) return;

        // Don't allow deletion of system templates
        var deletable = selected.Where(t => !t.IsSystemTemplate).ToList();
        if (!deletable.Any())
        {
            errorMessage = "System templates cannot be deleted.";
            return;
        }

        try
        {
            foreach (var template in deletable)
            {
                template.IsDeleted = true;
                template.ModifiedBy = currentUserId;
                template.ModifiedDate = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();
            await LoadTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting templates");
            errorMessage = "Error deleting templates. Please try again.";
        }
    }

    private async Task DuplicateTemplate()
    {
        var selected = GetSelectedTemplates().FirstOrDefault();
        if (selected == null) return;

        try
        {
            var newTemplate = new EstimationWorksheetTemplate
            {
                CompanyId = currentCompanyId,
                Name = $"{selected.Name} (Copy)",
                Description = selected.Description,
                WorksheetType = selected.WorksheetType,
                IsSystemTemplate = false,
                IsCompanyDefault = false,
                IsPublished = false,
                IsDefault = false,
                CreatedBy = currentUserId,
                AllowColumnReorder = selected.AllowColumnReorder,
                AllowAddRows = selected.AllowAddRows,
                AllowDeleteRows = selected.AllowDeleteRows,
                ShowRowNumbers = selected.ShowRowNumbers,
                ShowColumnTotals = selected.ShowColumnTotals,
                CreatedDate = DateTime.UtcNow
            };

            DbContext.EstimationWorksheetTemplates.Add(newTemplate);
            await DbContext.SaveChangesAsync();

            // Copy columns
            if (selected.Columns != null)
            {
                foreach (var col in selected.Columns.Where(c => !c.IsDeleted))
                {
                    var newCol = new EstimationWorksheetColumn
                    {
                        TemplateId = newTemplate.Id,
                        ColumnKey = col.ColumnKey,
                        ColumnName = col.ColumnName,
                        DataType = col.DataType,
                        Width = col.Width,
                        IsRequired = col.IsRequired,
                        IsReadOnly = col.IsReadOnly,
                        IsFrozen = col.IsFrozen,
                        IsHidden = col.IsHidden,
                        Formula = col.Formula,
                        DefaultValue = col.DefaultValue,
                        SelectOptions = col.SelectOptions,
                        Precision = col.Precision,
                        SortOrder = col.SortOrder
                    };
                    DbContext.EstimationWorksheetColumns.Add(newCol);
                }
                await DbContext.SaveChangesAsync();
            }

            Logger.LogInformation("Duplicated template {OriginalId} to {NewId}", selected.Id, newTemplate.Id);
            await LoadTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error duplicating template {TemplateId}", selected.Id);
            errorMessage = "Error duplicating template. Please try again.";
        }
    }

    private List<EstimationWorksheetTemplate> GetSelectedTemplates()
    {
        return currentView switch
        {
            GenericViewSwitcher<EstimationWorksheetTemplate>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<EstimationWorksheetTemplate>.ViewType.List => selectedListItems,
            GenericViewSwitcher<EstimationWorksheetTemplate>.ViewType.Card => selectedCardItems,
            _ => new List<EstimationWorksheetTemplate>()
        };
    }

    private void HandleViewLoaded(ViewState viewState)
    {
        currentViewState = viewState;
        hasUnsavedChanges = false;
        StateHasChanged();
    }

    private void HandleColumnsChanged(List<ColumnDefinition> columns)
    {
        managedColumns = columns;
        hasUnsavedChanges = true;
        hasCustomColumnConfig = true;
        StateHasChanged();
    }

    private string GetWorksheetTypeBadgeClass(string type)
    {
        return type switch
        {
            "MaterialCosts" => "bg-primary",
            "LaborCosts" => "bg-info",
            "WeldingCosts" => "bg-warning text-dark",
            "Equipment" => "bg-success",
            "Overhead" => "bg-secondary",
            "Summary" => "bg-dark",
            _ => "bg-secondary"
        };
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedTemplates();
        var hasSelection = selected.Any();
        var canDelete = hasSelection && selected.All(t => !t.IsSystemTemplate);

        return new ToolbarActionGroup
        {
            PrimaryActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "New",
                    Label = "New",
                    Icon = "fas fa-plus",
                    ActionFunc = () => { CreateNew(); return Task.CompletedTask; },
                    Style = ToolbarActionStyle.Primary,
                    Tooltip = "Create new worksheet template"
                },
                new ToolbarAction
                {
                    Text = "Delete",
                    Label = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = () => DeleteTemplates(),
                    IsDisabled = !canDelete,
                    Style = ToolbarActionStyle.Danger,
                    Tooltip = canDelete ? "Delete selected templates" : (hasSelection ? "System templates cannot be deleted" : "Select templates to delete")
                }
            },
            MenuActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "Duplicate",
                    Label = "Duplicate",
                    Icon = "fas fa-copy",
                    ActionFunc = () => DuplicateTemplate(),
                    IsDisabled = !hasSelection || selected.Count != 1,
                    Tooltip = "Duplicate selected template"
                }
            },
            RelatedActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "Estimations",
                    Label = "Estimations",
                    Icon = "fas fa-file-invoice-dollar",
                    ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations"); return Task.CompletedTask; },
                    Tooltip = "View all estimations"
                }
            }
        };
    }
}
