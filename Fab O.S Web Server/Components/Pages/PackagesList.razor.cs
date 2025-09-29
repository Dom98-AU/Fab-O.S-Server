using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.Filtering;
using FabOS.WebServer.Models.ViewState;

namespace FabOS.WebServer.Components.Pages;

public partial class PackagesList : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
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

    // Column management
    private List<ColumnDefinition> columnDefinitions = new();

    // Filter management
    private List<FilterRule> activeFilters = new();

    // View state management
    private ViewState currentViewState = new();
    private bool hasUnsavedChanges = false;

    protected override async Task OnInitializedAsync()
    {
        BreadcrumbService.SetBreadcrumb("Packages");
        await InitializeTableColumns();
        InitializeColumnDefinitions();
        await LoadPackages();
    }

    private async Task InitializeTableColumns()
    {
        await UpdateTableColumns();
    }

    private async Task UpdateTableColumns()
    {
        // Build table columns based on visible column definitions
        tableColumns = columnDefinitions
            .Where(c => c.IsVisible)
            .OrderBy(c => c.Order)
            .Select(c => CreateTableColumn(c))
            .Where(col => col != null)
            .ToList()!;

        // Apply frozen column states after table update
        await ApplyFrozenColumns();
    }

    private async Task ApplyFrozenColumns()
    {
        try
        {
            if (JSRuntime == null)
            {
                Console.WriteLine("JSRuntime is not available for frozen columns");
                return;
            }

            var frozenColumns = columnDefinitions
                .Where(c => c.IsVisible && c.IsFrozen && !string.IsNullOrEmpty(c.PropertyName))
                .OrderBy(c => c.Order)
                .Select(c => new
                {
                    PropertyName = c.PropertyName,
                    FreezePosition = c.FreezePosition.ToString(),
                    Order = c.Order
                })
                .ToArray();

            if (frozenColumns.Any())
            {
                await JSRuntime.InvokeVoidAsync("applyFrozenColumns", frozenColumns);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("clearFrozenColumns");
            }
        }
        catch (InvalidOperationException)
        {
            // JSRuntime not available during prerendering - ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying frozen columns: {ex.Message}");
        }
    }

    private GenericTableView<Package>.TableColumn<Package>? CreateTableColumn(ColumnDefinition columnDef)
    {
        var baseColumn = columnDef.PropertyName switch
        {
            "PackageNumber" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.PackageNumber,
                CssClass = "text-start"
            },
            "PackageName" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.PackageName,
                CssClass = "text-start"
            },
            "Description" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.Description ?? "—",
                CssClass = "text-start"
            },
            "Status" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.Status ?? "Active",
                CssClass = "text-center"
            },
            "EstimatedHours" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.EstimatedHours.ToString("N2"),
                CssClass = "text-end"
            },
            "EstimatedCost" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => $"${item.EstimatedCost:N2}",
                CssClass = "text-end"
            },
            "StartDate" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.StartDate?.ToString("MMM dd, yyyy") ?? "—",
                CssClass = "text-center"
            },
            "EndDate" => new GenericTableView<Package>.TableColumn<Package>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.EndDate?.ToString("MMM dd, yyyy") ?? "—",
                CssClass = "text-center"
            },
            _ => null
        };

        if (baseColumn != null)
        {
            // Add frozen column CSS classes
            if (columnDef.IsFrozen)
            {
                var frozenClass = columnDef.FreezePosition switch
                {
                    FreezePosition.Left => "frozen-column frozen-left",
                    FreezePosition.Right => "frozen-column frozen-right",
                    _ => "frozen-column"
                };
                baseColumn.CssClass += $" {frozenClass}";
            }

            // Add data attribute for JavaScript targeting
            baseColumn.PropertyName = columnDef.PropertyName;
        }

        return baseColumn;
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
        var result = packages.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            result = result.Where(p =>
                (p.PackageNumber?.ToLower().Contains(searchLower) ?? false) ||
                (p.PackageName?.ToLower().Contains(searchLower) ?? false) ||
                (p.Description?.ToLower().Contains(searchLower) ?? false) ||
                (p.Status?.ToLower().Contains(searchLower) ?? false)
            );
        }

        // Apply active filters
        if (activeFilters.Any())
        {
            result = result.Where(package =>
            {
                try
                {
                    foreach (var filter in activeFilters)
                    {
                        var fieldName = filter.Field ?? filter.FieldName;
                        if (string.IsNullOrEmpty(fieldName))
                            continue;

                        var propertyInfo = typeof(Package).GetProperty(fieldName);
                        if (propertyInfo != null && propertyInfo.CanRead)
                        {
                            var value = propertyInfo.GetValue(package);
                            if (!MatchesFilter(value, filter))
                                return false;
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error filtering package {package?.Id}: {ex.Message}");
                    return true; // Include item if filtering fails
                }
            });
        }

        filteredPackages = result.ToList();
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

    // Column management methods
    private void InitializeColumnDefinitions()
    {
        columnDefinitions = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                Id = "pkg-number",
                PropertyName = "PackageNumber",
                DisplayName = "Package Number",
                Order = 0,
                IsVisible = true,
                IsRequired = true
            },
            new ColumnDefinition
            {
                Id = "pkg-name",
                PropertyName = "PackageName",
                DisplayName = "Package Name",
                Order = 1,
                IsVisible = true,
                IsRequired = true
            },
            new ColumnDefinition
            {
                Id = "pkg-description",
                PropertyName = "Description",
                DisplayName = "Description",
                Order = 2,
                IsVisible = true
            },
            new ColumnDefinition
            {
                Id = "pkg-status",
                PropertyName = "Status",
                DisplayName = "Status",
                Order = 3,
                IsVisible = true
            },
            new ColumnDefinition
            {
                Id = "pkg-hours",
                PropertyName = "EstimatedHours",
                DisplayName = "Estimated Hours",
                Order = 4,
                IsVisible = true
            },
            new ColumnDefinition
            {
                Id = "pkg-cost",
                PropertyName = "EstimatedCost",
                DisplayName = "Estimated Cost",
                Order = 5,
                IsVisible = true
            },
            new ColumnDefinition
            {
                Id = "pkg-start",
                PropertyName = "StartDate",
                DisplayName = "Start Date",
                Order = 6,
                IsVisible = false
            },
            new ColumnDefinition
            {
                Id = "pkg-end",
                PropertyName = "EndDate",
                DisplayName = "End Date",
                Order = 7,
                IsVisible = false
            }
        };
    }

    private async Task OnColumnsChanged(List<ColumnDefinition> columns)
    {
        columnDefinitions = columns;
        await UpdateTableColumns();
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private void OnFiltersChanged(List<FilterRule> filters)
    {
        activeFilters = filters;
        FilterPackages();
        hasUnsavedChanges = true;
        StateHasChanged();
    }


    private bool MatchesFilter(object? value, FilterRule filter)
    {
        try
        {
            var stringValue = value?.ToString() ?? "";
            var filterValue = filter.Value?.ToString() ?? "";

            return filter.Operator switch
            {
                FilterOperator.Equals => stringValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.NotEquals => !stringValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.Contains => stringValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.StartsWith => stringValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.EndsWith => stringValue.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.GreaterThan => CompareNumeric(value, filter.Value, (a, b) => a > b),
                FilterOperator.LessThan => CompareNumeric(value, filter.Value, (a, b) => a < b),
                FilterOperator.Between => CompareBetween(value, filter.Value, filter.SecondValue),
                _ => true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying filter: {ex.Message}");
            return true; // If filter fails, include the item
        }
    }

    private bool CompareNumeric(object? value, object? filterValue, Func<decimal, decimal, bool> comparison)
    {
        try
        {
            if (decimal.TryParse(value?.ToString(), out var numValue) &&
                decimal.TryParse(filterValue?.ToString(), out var numFilter))
            {
                return comparison(numValue, numFilter);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool CompareBetween(object? value, object? minValue, object? maxValue)
    {
        try
        {
            if (decimal.TryParse(value?.ToString(), out var numValue) &&
                decimal.TryParse(minValue?.ToString(), out var minNum) &&
                decimal.TryParse(maxValue?.ToString(), out var maxNum))
            {
                return numValue >= minNum && numValue <= maxNum;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task OnViewLoaded(ViewState? state)
    {
        if (state != null)
        {
            currentViewState = state;
            // Apply loaded view state
            if (state.Columns.Any())
            {
                columnDefinitions = state.Columns;
                await UpdateTableColumns();
            }
            if (state.Filters.Any())
            {
                activeFilters = state.Filters;
            }
            FilterPackages();
        }
        else
        {
            // Reset to defaults
            InitializeColumnDefinitions();
            await UpdateTableColumns();
            activeFilters.Clear();
            FilterPackages();
        }
        hasUnsavedChanges = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        BreadcrumbService.OnBreadcrumbChanged -= StateHasChanged;
    }
}