using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;

namespace FabOS.WebServer.Components.Pages;

public partial class PackagesList : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private FabOS.WebServer.Services.BreadcrumbService BreadcrumbService { get; set; } = default!;

    private List<Package> packages = new();
    private List<Package> filteredPackages = new();
    private bool isLoading = true;
    private string searchTerm = "";

    // View state
    private GenericViewSwitcher<Package>.ViewType currentView = GenericViewSwitcher<Package>.ViewType.Table;

    // Selection tracking
    private List<Package> selectedTableItems = new();
    private List<Package> selectedListItems = new();
    private List<Package> selectedCardItems = new();

    // Table columns
    private List<GenericTableView<Package>.TableColumn<Package>> tableColumns = new();

    protected override async Task OnInitializedAsync()
    {
        BreadcrumbService.SetBreadcrumb("Packages");
        InitializeTableColumns();
        await LoadPackages();
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

            packages = await DbContext.Packages
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.PackageNumber)
                .ToListAsync();

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
        // Single click - could be used for selection
    }

    private void HandleRowDoubleClick(Package package)
    {
        Navigation.NavigateTo($"/packages/{package.Id}");
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
        Navigation.NavigateTo("/packages/0");
    }

    private void EditPackage()
    {
        var selected = GetSelectedPackages();
        if (selected.Any())
        {
            Navigation.NavigateTo($"/packages/{selected.First().Id}");
        }
    }

    private async Task DeletePackages()
    {
        var selected = GetSelectedPackages();
        if (!selected.Any()) return;

        foreach (var package in selected)
        {
            package.IsDeleted = true;
            package.LastModified = DateTime.UtcNow;
        }

        await DbContext.SaveChangesAsync();
        await LoadPackages();
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
                    Icon = "fas fa-plus",
                    ActionFunc = () => { CreateNew(); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "Create new package"
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
                    Text = "View Takeoffs",
                    Icon = "fas fa-ruler",
                    ActionFunc = () => { Navigation.NavigateTo("/takeoffs"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "View all takeoffs"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View Estimations",
                    Icon = "fas fa-calculator",
                    ActionFunc = () => { Navigation.NavigateTo("/estimations"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "View all estimations"
                }
            }
        };
    }

    public void Dispose()
    {
        BreadcrumbService.OnBreadcrumbChanged -= StateHasChanged;
    }
}