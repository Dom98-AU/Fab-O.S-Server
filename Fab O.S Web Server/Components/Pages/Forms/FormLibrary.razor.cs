using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Services.Interfaces.Forms;
using System.Security.Claims;
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Forms;

public partial class FormLibrary : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IFormTemplateService TemplateService { get; set; } = default!;
    [Inject] private ILogger<FormLibrary> Logger { get; set; } = default!;

    // State
    private bool isLoading = true;
    private string? errorMessage;
    private string searchTerm = "";

    // Data
    private List<FormTemplateSummaryDto> allTemplates = new();
    private List<FormTemplateSummaryDto> filteredTemplates = new();

    // Authentication
    private int currentUserId = 0;
    private int currentCompanyId = 0;
    private FormModuleContext currentModule = FormModuleContext.Estimate;

    // View state
    private GenericViewSwitcher<FormTemplateSummaryDto>.ViewType currentView = GenericViewSwitcher<FormTemplateSummaryDto>.ViewType.Table;

    // Selection tracking
    private List<FormTemplateSummaryDto> selectedTableItems = new();
    private List<FormTemplateSummaryDto> selectedListItems = new();
    private List<FormTemplateSummaryDto> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns
    private List<GenericTableView<FormTemplateSummaryDto>.TableColumn<FormTemplateSummaryDto>> tableColumns = new();

    protected override async Task OnInitializedAsync()
    {
        await InitializeAuthState();

        if (currentCompanyId == 0)
        {
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        // Determine module from URL
        DetermineCurrentModule();

        InitializeTableColumns();
        await LoadTemplates();
    }

    private async Task InitializeAuthState()
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
    }

    private void DetermineCurrentModule()
    {
        var path = Navigation.Uri.ToLower();
        if (path.Contains("/estimate/"))
            currentModule = FormModuleContext.Estimate;
        else if (path.Contains("/fabmate/"))
            currentModule = FormModuleContext.FabMate;
        else if (path.Contains("/qdocs/"))
            currentModule = FormModuleContext.QDocs;
        else if (path.Contains("/assets/"))
            currentModule = FormModuleContext.Assets;
    }

    private void InitializeTableColumns()
    {
        tableColumns = new List<GenericTableView<FormTemplateSummaryDto>.TableColumn<FormTemplateSummaryDto>>
        {
            new() { Header = "Template Name", ValueSelector = item => item.Name, IsSortable = true, CssClass = "text-start" },
            new() { Header = "Type", ValueSelector = item => item.FormType ?? "—", IsSortable = true, CssClass = "text-center" },
            new() { Header = "Description", ValueSelector = item => item.Description ?? "—", IsSortable = true, CssClass = "text-start" },
            new() { Header = "Fields", ValueSelector = item => item.FieldCount.ToString(), IsSortable = true, CssClass = "text-center" },
            new() { Header = "Instances", ValueSelector = item => item.InstanceCount.ToString(), IsSortable = true, CssClass = "text-center" },
            new() { Header = "System", ValueSelector = item => item.IsSystemTemplate ? "Yes" : "No", IsSortable = true, CssClass = "text-center" },
            new() { Header = "Default", ValueSelector = item => item.IsCompanyDefault ? "Yes" : "No", IsSortable = true, CssClass = "text-center" },
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
                PropertyName = "FormType",
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
                PropertyName = "FieldCount",
                DisplayName = "Fields",
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
                PropertyName = "InstanceCount",
                DisplayName = "Instances",
                Type = ColumnType.Number,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 80,
                Order = 4
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
                Order = 5
            },
            new ColumnDefinition
            {
                PropertyName = "IsCompanyDefault",
                DisplayName = "Default",
                Type = ColumnType.Boolean,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 80,
                Order = 6
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
                Order = 7
            }
        };
    }

    private async Task LoadTemplates()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // Load templates for current module
            var result = await TemplateService.GetTemplatesAsync(
                currentCompanyId,
                currentModule,
                includeSystemTemplates: true,
                page: 1,
                pageSize: 1000);

            allTemplates = result.Items;
            FilterTemplates();
            errorMessage = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading form templates for company {CompanyId}", currentCompanyId);
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
                (t.FormType?.ToLower().Contains(searchLower) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterTemplates();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<FormTemplateSummaryDto>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleRowClick(FormTemplateSummaryDto template)
    {
        NavigateToTemplate(template.Id);
    }

    private void HandleRowDoubleClick(FormTemplateSummaryDto template)
    {
        NavigateToTemplate(template.Id);
    }

    private void NavigateToTemplate(int templateId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/{GetModulePath()}/forms/templates/{templateId}/design");
    }

    private string GetModulePath()
    {
        return currentModule switch
        {
            FormModuleContext.Estimate => "estimate",
            FormModuleContext.FabMate => "fabmate",
            FormModuleContext.QDocs => "qdocs",
            FormModuleContext.Assets => "assets",
            _ => "estimate"
        };
    }

    private void HandleTableSelectionChanged(List<FormTemplateSummaryDto> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<FormTemplateSummaryDto> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<FormTemplateSummaryDto> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
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

    private List<FormTemplateSummaryDto> GetSelectedTemplates()
    {
        return currentView switch
        {
            GenericViewSwitcher<FormTemplateSummaryDto>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<FormTemplateSummaryDto>.ViewType.List => selectedListItems,
            GenericViewSwitcher<FormTemplateSummaryDto>.ViewType.Card => selectedCardItems,
            _ => new List<FormTemplateSummaryDto>()
        };
    }

    #region Create Template

    private void CreateNew()
    {
        // Navigate to FormDesigner with ID=0 for creating a new template
        // This follows the same pattern as TemplateLibrary
        Navigation.NavigateTo($"/{TenantSlug}/{GetModulePath()}/forms/templates/0/design");
    }

    #endregion

    #region Delete Templates

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
                await TemplateService.DeleteTemplateAsync(template.Id, currentCompanyId);
            }

            Logger.LogInformation("Deleted {Count} form templates for company {CompanyId}", deletable.Count, currentCompanyId);
            await LoadTemplates();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting form templates");
            errorMessage = "Error deleting templates. Please try again.";
        }
    }

    #endregion

    #region Duplicate Template

    private async Task DuplicateTemplate()
    {
        var selected = GetSelectedTemplates().FirstOrDefault();
        if (selected == null) return;

        try
        {
            var duplicated = await TemplateService.DuplicateTemplateAsync(selected.Id, $"{selected.Name} (Copy)", currentCompanyId, currentUserId);

            if (duplicated != null)
            {
                Logger.LogInformation("Duplicated form template {OriginalId} to {NewId}", selected.Id, duplicated.Id);
                await LoadTemplates();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error duplicating form template {TemplateId}", selected.Id);
            errorMessage = "Error duplicating template. Please try again.";
        }
    }

    #endregion

    #region IToolbarActionProvider Implementation

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
                    Tooltip = "Create new form template"
                },
                new ToolbarAction
                {
                    Text = "Delete",
                    Label = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = async () => await DeleteTemplates(),
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
                    ActionFunc = async () => await DuplicateTemplate(),
                    IsDisabled = !hasSelection || selected.Count != 1,
                    Tooltip = "Duplicate selected template"
                }
            },
            RelatedActions = new List<ToolbarAction>()
        };
    }

    #endregion
}
