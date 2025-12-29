using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using System.Security.Claims;
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class EstimatePackageWorksheetsList : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int PackageId { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<EstimatePackageWorksheetsList> Logger { get; set; } = default!;

    private EstimationRevisionPackage? package;
    private List<EstimationWorksheet> allWorksheets = new();
    private List<EstimationWorksheet> filteredWorksheets = new();
    private bool isLoading = true;
    private string searchTerm = "";
    private string? errorMessage;
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // View state
    private GenericViewSwitcher<EstimationWorksheet>.ViewType currentView = GenericViewSwitcher<EstimationWorksheet>.ViewType.Table;

    // Selection tracking
    private List<EstimationWorksheet> selectedTableItems = new();
    private List<EstimationWorksheet> selectedListItems = new();
    private List<EstimationWorksheet> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns
    private List<GenericTableView<EstimationWorksheet>.TableColumn<EstimationWorksheet>> tableColumns = new();

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
            Logger.LogWarning("User is not authenticated or missing required claims for EstimatePackageWorksheetsList page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        InitializeTableColumns();
        await LoadPackage();
        await LoadWorksheets();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (PackageId != package?.Id)
        {
            await LoadPackage();
            await LoadWorksheets();
        }
    }

    private void InitializeTableColumns()
    {
        tableColumns = new List<GenericTableView<EstimationWorksheet>.TableColumn<EstimationWorksheet>>
        {
            new() { Header = "Worksheet Name", ValueSelector = item => item.Name, IsSortable = true, CssClass = "text-start" },
            new() { Header = "Type", ValueSelector = item => item.WorksheetType, IsSortable = true, CssClass = "text-center" },
            new() { Header = "Description", ValueSelector = item => item.Description ?? "—", IsSortable = true, CssClass = "text-start" },
            new() { Header = "Rows", ValueSelector = item => (item.Rows?.Count(r => !r.IsDeleted && !r.IsGroupHeader) ?? 0).ToString(), IsSortable = true, CssClass = "text-center" },
            new() { Header = "Material Cost", ValueSelector = item => item.TotalMaterialCost.ToString("C"), IsSortable = true, CssClass = "text-end" },
            new() { Header = "Labor Cost", ValueSelector = item => item.TotalLaborCost.ToString("C"), IsSortable = true, CssClass = "text-end" },
            new() { Header = "Total", ValueSelector = item => item.TotalCost.ToString("C"), IsSortable = true, CssClass = "text-end" }
        };

        managedColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                PropertyName = "Name",
                DisplayName = "Worksheet Name",
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
                PropertyName = "RowsCount",
                DisplayName = "Rows",
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
                PropertyName = "TotalMaterialCost",
                DisplayName = "Material Cost",
                Type = ColumnType.Currency,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 4
            },
            new ColumnDefinition
            {
                PropertyName = "TotalLaborCost",
                DisplayName = "Labor Cost",
                Type = ColumnType.Currency,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 5
            },
            new ColumnDefinition
            {
                PropertyName = "TotalCost",
                DisplayName = "Total",
                Type = ColumnType.Currency,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 6
            }
        };
    }

    private async Task LoadPackage()
    {
        try
        {
            package = await DbContext.EstimationRevisionPackages
                .Include(p => p.Revision)
                    .ThenInclude(r => r!.Estimation)
                .FirstOrDefaultAsync(p => p.Id == PackageId && p.CompanyId == currentCompanyId && !p.IsDeleted);

            if (package == null)
            {
                Logger.LogWarning("Package {PackageId} not found or access denied for company {CompanyId}", PackageId, currentCompanyId);
                errorMessage = "Package not found or you don't have access.";
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading package {PackageId} for company {CompanyId}", PackageId, currentCompanyId);
            errorMessage = "Error loading package. Please try again.";
        }
    }

    private async Task LoadWorksheets()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            if (package == null)
            {
                return;
            }

            allWorksheets = await DbContext.EstimationWorksheets
                .Include(w => w.Rows.Where(r => !r.IsDeleted))
                .Where(w => w.PackageId == PackageId && w.CompanyId == currentCompanyId && !w.IsDeleted)
                .OrderBy(w => w.SortOrder)
                .ToListAsync();

            FilterWorksheets();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading worksheets for package {PackageId}", PackageId);
            errorMessage = "Error loading worksheets. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void FilterWorksheets()
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            filteredWorksheets = allWorksheets;
        }
        else
        {
            var searchLower = searchTerm.ToLower();
            filteredWorksheets = allWorksheets.Where(w =>
                (w.Name?.ToLower().Contains(searchLower) ?? false) ||
                (w.Description?.ToLower().Contains(searchLower) ?? false) ||
                (w.WorksheetType?.ToLower().Contains(searchLower) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterWorksheets();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<EstimationWorksheet>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleRowClick(EstimationWorksheet worksheet)
    {
        NavigateToWorksheet(worksheet.Id);
    }

    private void HandleRowDoubleClick(EstimationWorksheet worksheet)
    {
        NavigateToWorksheet(worksheet.Id);
    }

    private void NavigateToWorksheet(int worksheetId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/worksheets/{worksheetId}");
    }

    private void HandleTableSelectionChanged(List<EstimationWorksheet> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<EstimationWorksheet> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<EstimationWorksheet> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void CreateNew()
    {
        // Navigate to new worksheet - will need to handle this in worksheet card
        Navigation.NavigateTo($"/{TenantSlug}/estimate/worksheets/0?packageId={PackageId}");
    }

    private async Task DeleteWorksheets()
    {
        var selected = GetSelectedWorksheets();
        if (!selected.Any()) return;

        try
        {
            foreach (var worksheet in selected)
            {
                worksheet.IsDeleted = true;
                worksheet.ModifiedBy = currentUserId;
                worksheet.ModifiedDate = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();
            await LoadWorksheets();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting worksheets");
            errorMessage = "Error deleting worksheets. Please try again.";
        }
    }

    private List<EstimationWorksheet> GetSelectedWorksheets()
    {
        return currentView switch
        {
            GenericViewSwitcher<EstimationWorksheet>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<EstimationWorksheet>.ViewType.List => selectedListItems,
            GenericViewSwitcher<EstimationWorksheet>.ViewType.Card => selectedCardItems,
            _ => new List<EstimationWorksheet>()
        };
    }

    private void NavigateToEstimations()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations");
    }

    private void NavigateBackToPackage()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/{PackageId}");
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
            _ => "bg-secondary"
        };
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedWorksheets();
        var hasSelection = selected.Any();
        var canAddWorksheet = package?.Revision?.Status == "Draft";

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
                    IsDisabled = !canAddWorksheet,
                    Tooltip = canAddWorksheet ? "Create new worksheet for this package" : "Only draft revisions can have worksheets added"
                },
                new ToolbarAction
                {
                    Text = "Delete",
                    Label = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = () => DeleteWorksheets(),
                    IsDisabled = !hasSelection || package?.Revision?.Status != "Draft",
                    Style = ToolbarActionStyle.Danger,
                    Tooltip = hasSelection ? "Delete selected worksheets" : "Select worksheets to delete"
                }
            },
            RelatedActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "Back to Package",
                    Label = "Back to Package",
                    Icon = "fas fa-arrow-left",
                    ActionFunc = () => { NavigateBackToPackage(); return Task.CompletedTask; },
                    Tooltip = "Return to package details"
                },
                new ToolbarAction
                {
                    Text = "Back to Estimation",
                    Label = "Back to Estimation",
                    Icon = "fas fa-file-alt",
                    ActionFunc = () => {
                        if (package?.Revision?.EstimationId != null)
                            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{package.Revision.EstimationId}");
                        return Task.CompletedTask;
                    },
                    Tooltip = "Return to estimation details"
                }
            }
        };
    }
}
