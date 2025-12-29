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

public partial class EstimationPackagesList : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int EstimationId { get; set; }
    [Parameter] public int RevisionId { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<EstimationPackagesList> Logger { get; set; } = default!;

    private EstimationRevision? revision;
    private Estimation? estimation;
    private List<EstimationRevisionPackage> allPackages = new();
    private List<EstimationRevisionPackage> filteredPackages = new();
    private bool isLoading = true;
    private string searchTerm = "";
    private string? errorMessage;
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // View state
    private GenericViewSwitcher<EstimationRevisionPackage>.ViewType currentView = GenericViewSwitcher<EstimationRevisionPackage>.ViewType.Table;

    // Selection tracking
    private List<EstimationRevisionPackage> selectedTableItems = new();
    private List<EstimationRevisionPackage> selectedListItems = new();
    private List<EstimationRevisionPackage> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    // Table columns
    private List<GenericTableView<EstimationRevisionPackage>.TableColumn<EstimationRevisionPackage>> tableColumns = new();

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
            Logger.LogWarning("User is not authenticated or missing required claims for EstimationPackagesList page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        InitializeTableColumns();
        await LoadRevision();
        await LoadPackages();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (RevisionId != revision?.Id)
        {
            await LoadRevision();
            await LoadPackages();
        }
    }

    private void InitializeTableColumns()
    {
        tableColumns = new List<GenericTableView<EstimationRevisionPackage>.TableColumn<EstimationRevisionPackage>>
        {
            new() { Header = "Package Name", ValueSelector = item => item.Name, IsSortable = true, CssClass = "text-start" },
            new() { Header = "Description", ValueSelector = item => item.Description ?? "—", IsSortable = true, CssClass = "text-start" },
            new() { Header = "Worksheets", ValueSelector = item => (item.Worksheets?.Count(w => !w.IsDeleted) ?? 0).ToString(), IsSortable = true, CssClass = "text-center" },
            new() { Header = "Material Cost", ValueSelector = item => item.MaterialCost.ToString("C"), IsSortable = true, CssClass = "text-end" },
            new() { Header = "Labor Cost", ValueSelector = item => item.LaborCost.ToString("C"), IsSortable = true, CssClass = "text-end" },
            new() { Header = "Total", ValueSelector = item => item.PackageTotal.ToString("C"), IsSortable = true, CssClass = "text-end" }
        };

        managedColumns = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                PropertyName = "Name",
                DisplayName = "Package Name",
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
                PropertyName = "Description",
                DisplayName = "Description",
                Type = ColumnType.Text,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 300,
                Order = 1
            },
            new ColumnDefinition
            {
                PropertyName = "WorksheetsCount",
                DisplayName = "Worksheets",
                Type = ColumnType.Number,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 100,
                Order = 2
            },
            new ColumnDefinition
            {
                PropertyName = "MaterialCost",
                DisplayName = "Material Cost",
                Type = ColumnType.Currency,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 3
            },
            new ColumnDefinition
            {
                PropertyName = "LaborCost",
                DisplayName = "Labor Cost",
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
                PropertyName = "PackageTotal",
                DisplayName = "Total",
                Type = ColumnType.Currency,
                IsVisible = true,
                IsSortable = true,
                IsResizable = true,
                IsReorderable = true,
                Width = 120,
                Order = 5
            }
        };
    }

    private async Task LoadRevision()
    {
        try
        {
            revision = await DbContext.EstimationRevisions
                .Include(r => r.Estimation)
                .FirstOrDefaultAsync(r => r.Id == RevisionId && r.EstimationId == EstimationId && r.CompanyId == currentCompanyId && !r.IsDeleted);

            if (revision == null)
            {
                Logger.LogWarning("Revision {RevisionId} not found or access denied for company {CompanyId}", RevisionId, currentCompanyId);
                errorMessage = "Revision not found or you don't have access.";
                return;
            }

            estimation = revision.Estimation;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading revision {RevisionId} for company {CompanyId}", RevisionId, currentCompanyId);
            errorMessage = "Error loading revision. Please try again.";
        }
    }

    private async Task LoadPackages()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            if (revision == null)
            {
                return;
            }

            allPackages = await DbContext.EstimationRevisionPackages
                .Include(p => p.Worksheets.Where(w => !w.IsDeleted))
                .Where(p => p.RevisionId == RevisionId && p.CompanyId == currentCompanyId && !p.IsDeleted)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            FilterPackages();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading packages for revision {RevisionId}", RevisionId);
            errorMessage = "Error loading packages. Please try again.";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void FilterPackages()
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            filteredPackages = allPackages;
        }
        else
        {
            var searchLower = searchTerm.ToLower();
            filteredPackages = allPackages.Where(p =>
                (p.Name?.ToLower().Contains(searchLower) ?? false) ||
                (p.Description?.ToLower().Contains(searchLower) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterPackages();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<EstimationRevisionPackage>.ViewType newView)
    {
        currentView = newView;
        StateHasChanged();
    }

    private void HandleRowClick(EstimationRevisionPackage package)
    {
        NavigateToPackage(package.Id);
    }

    private void HandleRowDoubleClick(EstimationRevisionPackage package)
    {
        NavigateToPackage(package.Id);
    }

    private void NavigateToPackage(int packageId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/{packageId}");
    }

    private void HandleTableSelectionChanged(List<EstimationRevisionPackage> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<EstimationRevisionPackage> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<EstimationRevisionPackage> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void CreateNew()
    {
        // Navigate to new package - will need to handle this in package card
        Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/0?revisionId={RevisionId}");
    }

    private async Task DeletePackages()
    {
        var selected = GetSelectedPackages();
        if (!selected.Any()) return;

        try
        {
            foreach (var package in selected)
            {
                package.IsDeleted = true;
                package.ModifiedBy = currentUserId;
                package.ModifiedDate = DateTime.UtcNow;
            }

            await DbContext.SaveChangesAsync();
            await LoadPackages();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting packages");
            errorMessage = "Error deleting packages. Please try again.";
        }
    }

    private List<EstimationRevisionPackage> GetSelectedPackages()
    {
        return currentView switch
        {
            GenericViewSwitcher<EstimationRevisionPackage>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<EstimationRevisionPackage>.ViewType.List => selectedListItems,
            GenericViewSwitcher<EstimationRevisionPackage>.ViewType.Card => selectedCardItems,
            _ => new List<EstimationRevisionPackage>()
        };
    }

    private void NavigateToRevisions()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions");
    }

    private void NavigateBackToRevision()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions/{RevisionId}");
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

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedPackages();
        var hasSelection = selected.Any();
        var canAddPackage = revision?.Status == "Draft";

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
                    IsDisabled = !canAddPackage,
                    Tooltip = canAddPackage ? "Create new package for this revision" : "Only draft revisions can have packages added"
                },
                new ToolbarAction
                {
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = () => DeletePackages(),
                    IsDisabled = !hasSelection || revision?.Status != "Draft",
                    Style = ToolbarActionStyle.Danger,
                    Tooltip = hasSelection ? "Delete selected packages" : "Select packages to delete"
                }
            },
            RelatedActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "Back to Revision",
                    Icon = "fas fa-arrow-left",
                    ActionFunc = () => { NavigateBackToRevision(); return Task.CompletedTask; },
                    Tooltip = "Return to revision details"
                },
                new ToolbarAction
                {
                    Text = "Back to Estimation",
                    Icon = "fas fa-file-alt",
                    ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}"); return Task.CompletedTask; },
                    Tooltip = "Return to estimation details"
                }
            }
        };
    }
}
