using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Services.Interfaces;
using System.Security.Claims;

namespace FabOS.WebServer.Components.Pages;

public partial class TakeoffPackages : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int TakeoffId { get; set; }
    [Parameter] public int? RevisionId { get; set; }

    [Inject] private ITakeoffCardService TakeoffService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<TakeoffPackages> Logger { get; set; } = default!;

    private Takeoff? takeoff;
    private List<Package> packages = new();
    private List<Package> filteredPackages = new();
    private bool isLoading = true;
    private string searchTerm = "";
    private string? errorMessage;

    // Authentication state
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // View state
    private GenericViewSwitcher<Package>.ViewType currentView = GenericViewSwitcher<Package>.ViewType.Table;

    // Selection tracking
    private List<Package> selectedTableItems = new();
    private List<Package> selectedListItems = new();
    private List<Package> selectedCardItems = new();

    // View state management
    private ViewState? currentViewState;
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();
    private List<Package> allPackages = new();

    // Table columns
    private List<GenericTableView<Package>.TableColumn<Package>> tableColumns = new();

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
            Logger.LogWarning("User is not authenticated or missing required claims for TakeoffPackages page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadTakeoff();
        InitializeTableColumns();
        await LoadPackages();
    }

    private async Task LoadTakeoff()
    {
        try
        {
            // Use service for company-isolated access
            takeoff = await TakeoffService.GetTakeoffByIdAsync(TakeoffId, currentCompanyId);

            if (takeoff == null)
            {
                Logger.LogWarning("Takeoff {TakeoffId} not found or access denied for company {CompanyId}", TakeoffId, currentCompanyId);
                errorMessage = "Takeoff not found or you don't have access.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading takeoff {TakeoffId} for company {CompanyId}", TakeoffId, currentCompanyId);
            errorMessage = "Error loading takeoff. Please try again.";
        }
    }

    private void InitializeTableColumns()
    {
        tableColumns = new List<GenericTableView<Package>.TableColumn<Package>>
        {
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Package Number",
                ValueSelector = item => item.PackageNumber,
                CssClass = "text-start"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Package Name",
                ValueSelector = item => item.PackageName,
                CssClass = "text-start"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Description",
                ValueSelector = item => item.Description ?? "—",
                CssClass = "text-start"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Status",
                ValueSelector = item => item.Status ?? "Active",
                CssClass = "text-center"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Estimated Hours",
                ValueSelector = item => item.EstimatedHours.ToString("N2"),
                CssClass = "text-end"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Estimated Cost",
                ValueSelector = item => $"${item.EstimatedCost:N2}",
                CssClass = "text-end"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "Start Date",
                ValueSelector = item => item.StartDate?.ToString("MMM dd, yyyy") ?? "—",
                CssClass = "text-center"
            },
            new GenericTableView<Package>.TableColumn<Package>
            {
                Header = "End Date",
                ValueSelector = item => item.EndDate?.ToString("MMM dd, yyyy") ?? "—",
                CssClass = "text-center"
            }
        };
    }

    private async Task LoadPackages()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // Use service for company-isolated package access
            packages = await TakeoffService.GetPackagesByTakeoffAsync(TakeoffId, currentCompanyId, RevisionId);

            FilterPackages();
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
            filteredPackages = packages;
            allPackages = packages;
        }
        else
        {
            var searchLower = searchTerm.ToLower();
            filteredPackages = packages.Where(p =>
                (p.PackageNumber?.ToLower().Contains(searchLower) ?? false) ||
                (p.PackageName?.ToLower().Contains(searchLower) ?? false) ||
                (p.Description?.ToLower().Contains(searchLower) ?? false) ||
                (p.Status?.ToLower().Contains(searchLower) ?? false)
            ).ToList();
        }
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterPackages();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<Package>.ViewType newView)
    {
        currentView = newView;
    }

    private void HandleRowClick(Package package)
    {
        // Single click navigates to package card
        Navigation.NavigateTo($"/{TenantSlug}/trace/packages/{package.Id}");
    }

    private void HandleRowDoubleClick(Package package)
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/packages/{package.Id}");
    }

    private void HandleTableSelectionChanged(List<Package> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<Package> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<Package> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private void CreateNew()
    {
        // Navigate to new package with takeoff pre-selected
        Navigation.NavigateTo($"/{TenantSlug}/trace/packages/0?takeoffId={TakeoffId}");
    }

    private void EditPackage()
    {
        var selected = GetSelectedPackages();
        if (selected.Any())
        {
            Navigation.NavigateTo($"/{TenantSlug}/trace/packages/{selected.First().Id}");
        }
    }

    private async Task DeletePackages()
    {
        var selected = GetSelectedPackages();
        if (!selected.Any()) return;

        var packageIds = selected.Select(p => p.Id).ToList();
        var success = await TakeoffService.DeletePackagesAsync(packageIds, TakeoffId, currentCompanyId, currentUserId);

        if (success)
        {
            await LoadPackages();
        }
        else
        {
            Logger.LogWarning("Failed to delete packages for takeoff {TakeoffId}", TakeoffId);
        }
    }

    private List<Package> GetSelectedPackages()
    {
        return currentView switch
        {
            GenericViewSwitcher<Package>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<Package>.ViewType.List => selectedListItems,
            GenericViewSwitcher<Package>.ViewType.Card => selectedCardItems,
            _ => new List<Package>()
        };
    }

    private void NavigateToTakeoffs()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs");
    }

    private void NavigateBackToTakeoff()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs/{TakeoffId}");
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedPackages();
        var hasSelection = selected.Any();
        var singleSelection = selected.Count == 1;

        return new ToolbarActionGroup
        {
            PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
            {
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "New",
                    Label = "New",
                    Icon = "fas fa-plus",
                    ActionFunc = () => { CreateNew(); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "Create new package for this takeoff"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Edit",
                    Icon = "fas fa-edit",
                    ActionFunc = () => { EditPackage(); return Task.CompletedTask; },
                    IsDisabled = !singleSelection,
                    Tooltip = singleSelection ? "Edit selected package" : "Select a single package to edit"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = () => DeletePackages(),
                    IsDisabled = !hasSelection,
                    Tooltip = hasSelection ? "Delete selected packages" : "Select packages to delete"
                }
            },
            MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
            {
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Export",
                    Icon = "fas fa-file-export",
                    ActionFunc = () => Task.CompletedTask,
                    IsDisabled = !hasSelection,
                    Tooltip = "Export selected packages"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Duplicate",
                    Icon = "fas fa-copy",
                    ActionFunc = () => Task.CompletedTask,
                    IsDisabled = !singleSelection,
                    Tooltip = "Duplicate selected package"
                }
            },
            RelatedActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
            {
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Back to Takeoff",
                    Icon = "fas fa-arrow-left",
                    ActionFunc = () => { NavigateBackToTakeoff(); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "Return to takeoff details"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View All Packages",
                    Icon = "fas fa-box",
                    ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/trace/packages"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "View all packages"
                }
            }
        };
    }

    private void HandleViewLoaded(ViewState viewState)
    {
        // Handle loading a saved view preference
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

    public void Dispose()
    {
    }
}