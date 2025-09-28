using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.Filtering;
using FabOS.WebServer.Models.ViewState;
using Microsoft.JSInterop;

namespace FabOS.WebServer.Components.Shared;

public partial class GenericTableView<TItem> : ComponentBase where TItem : class
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public List<TItem> Items { get; set; } = new();
    [Parameter] public List<TItem> AllItems { get; set; } = new();
    [Parameter] public List<TableColumn<TItem>> Columns { get; set; } = new();
    [Parameter] public EventCallback<TItem> OnRowClick { get; set; }
    [Parameter] public EventCallback<TItem> OnRowDoubleClick { get; set; }
    [Parameter] public bool AllowSelection { get; set; } = false;
    [Parameter] public bool MultiSelect { get; set; } = true;
    [Parameter] public List<TItem> SelectedItems { get; set; } = new();
    [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public string CssClass { get; set; } = "";
    [Parameter] public bool ShowCheckboxes { get; set; } = false;
    [Parameter] public bool AllowSorting { get; set; } = true;
    [Parameter] public bool AllowFiltering { get; set; } = false;
    [Parameter] public RenderFragment<TItem>? CustomRowContent { get; set; }
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public string TableClass { get; set; } = "table";
    [Parameter] public string EmptyMessage { get; set; } = "No data available";
    [Parameter] public EventCallback<TItem> OnEdit { get; set; }
    [Parameter] public EventCallback<TItem> OnDelete { get; set; }
    [Parameter] public RenderFragment<TItem>? ActionsTemplate { get; set; }
    [Parameter] public Func<TItem, bool>? IsSelected { get; set; }

    // Advanced features parameters
    [Parameter] public bool EnableAdvancedFeatures { get; set; } = false;
    [Parameter] public bool EnableColumnManagement { get; set; } = false;
    [Parameter] public bool EnableFiltering { get; set; } = false;
    [Parameter] public bool EnableViewPreferences { get; set; } = false;
    [Parameter] public string EntityType { get; set; } = "";

    private bool ShowSelection => AllowSelection || ShowCheckboxes;
    private bool ShowActions => OnEdit.HasDelegate || OnDelete.HasDelegate || ActionsTemplate != null;

    private string currentSortColumn = "";
    private bool isAscending = true;
    private Dictionary<string, string> columnFilters = new();

    // Advanced features state
    private List<ColumnDefinition> managedColumns = new();
    private ViewState currentViewState = new();
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<FilterRule> activeFilters = new();

    public class TableColumn<T>
    {
        public string Title { get; set; } = "";
        public string Header { get; set; } = "";
        public Func<T, object?>? ValueSelector { get; set; }
        public string PropertyName { get; set; } = "";
        public string CssClass { get; set; } = "";
        public bool IsSortable { get; set; } = true;
        public bool IsFilterable { get; set; } = false;
        public RenderFragment<T>? Template { get; set; }
    }

    private async Task HandleRowClick(TItem item)
    {
        if (OnRowClick.HasDelegate)
        {
            await OnRowClick.InvokeAsync(item);
        }
    }

    private async Task HandleRowDoubleClick(TItem item)
    {
        if (OnRowDoubleClick.HasDelegate)
        {
            await OnRowDoubleClick.InvokeAsync(item);
        }
    }

    private void ToggleSelection(TItem item)
    {
        if (IsItemSelected(item))
        {
            SelectedItems.Remove(item);
        }
        else
        {
            if (!MultiSelect)
            {
                SelectedItems.Clear();
            }
            SelectedItems.Add(item);
        }

        if (SelectedItemsChanged.HasDelegate)
        {
            _ = SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
    }

    private bool IsItemSelected(TItem item)
    {
        return SelectedItems.Contains(item);
    }

    private async Task ToggleAllSelection()
    {
        if (SelectedItems.Count == Items.Count)
        {
            SelectedItems.Clear();
        }
        else
        {
            SelectedItems.Clear();
            SelectedItems.AddRange(Items);
        }

        if (SelectedItemsChanged.HasDelegate)
        {
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
        StateHasChanged();
    }

    private async Task HandleSort(TableColumn<TItem> column)
    {
        if (!AllowSorting || !column.IsSortable) return;

        if (currentSortColumn == column.Header)
        {
            isAscending = !isAscending;
        }
        else
        {
            currentSortColumn = column.Header;
            isAscending = true;
        }

        StateHasChanged();
    }

    private void FilterColumn(string columnName, string filterValue)
    {
        if (!AllowFiltering) return;

        if (string.IsNullOrWhiteSpace(filterValue))
        {
            columnFilters.Remove(columnName);
        }
        else
        {
            columnFilters[columnName] = filterValue;
        }

        // Filtering logic would be implemented here
        StateHasChanged();
    }

    private string GetSortIcon(string columnName)
    {
        if (currentSortColumn != columnName)
            return "fas fa-sort";
        return isAscending ? "fas fa-sort-up" : "fas fa-sort-down";
    }

    private string GetSortIconClass(TableColumn<TItem> column)
    {
        if (!column.IsSortable || currentSortColumn != column.Header)
            return "fas fa-sort";
        return isAscending ? "fas fa-sort-up" : "fas fa-sort-down";
    }

    private async Task HandleItemSelectionChanged(TItem item, bool isChecked)
    {
        if (isChecked && !SelectedItems.Contains(item))
        {
            if (!MultiSelect)
            {
                SelectedItems.Clear();
            }
            SelectedItems.Add(item);
        }
        else if (!isChecked && SelectedItems.Contains(item))
        {
            SelectedItems.Remove(item);
        }

        if (SelectedItemsChanged.HasDelegate)
        {
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
    }

    // Advanced features methods
    protected override void OnInitialized()
    {
        if (EnableAdvancedFeatures && Columns.Any())
        {
            InitializeColumnsFromDefinitions();
        }
        if (AllItems == null || !AllItems.Any())
        {
            AllItems = Items;
        }
    }

    protected override void OnParametersSet()
    {
        // Ensure columns are initialized if they weren't in OnInitialized
        if (EnableAdvancedFeatures && Columns.Any() && !managedColumns.Any())
        {
            InitializeColumnsFromDefinitions();
        }
        base.OnParametersSet();
    }

    private void InitializeColumnsFromDefinitions()
    {
        managedColumns = Columns.Select(c => new ColumnDefinition
        {
            PropertyName = c.PropertyName,
            DisplayName = c.Header,
            IsVisible = true,
            IsFrozen = false,
            IsRequired = false,
            Width = null
        }).ToList();
        StateHasChanged();
    }

    private List<TableColumn<TItem>> GetVisibleColumns()
    {
        if (!EnableColumnManagement)
            return Columns;

        return Columns.Where(c =>
        {
            var managed = managedColumns.FirstOrDefault(mc => mc.PropertyName == c.PropertyName);
            return managed == null || managed.IsVisible;
        }).ToList();
    }

    private bool IsColumnFrozen(TableColumn<TItem> column)
    {
        if (!EnableColumnManagement)
            return false;

        var managed = managedColumns.FirstOrDefault(mc => mc.PropertyName == column.PropertyName);
        return managed?.IsFrozen == true;
    }

    private async Task HandleColumnsChanged(List<ColumnDefinition>? columns)
    {
        if (columns == null)
        {
            // Reset to defaults
            InitializeColumnsFromDefinitions();
        }
        else
        {
            managedColumns = columns;
            hasCustomColumnConfig = true;
        }
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private async Task HandleFiltersChanged(List<FilterRule> filters)
    {
        activeFilters = filters;
        ApplyFilters();
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private void ApplyFilters()
    {
        if (!activeFilters.Any())
        {
            Items = AllItems;
            return;
        }

        // Apply filter logic here based on activeFilters
        // This is a simplified implementation
        Items = AllItems.Where(item =>
        {
            foreach (var filter in activeFilters)
            {
                // Get property value using reflection
                var propertyInfo = typeof(TItem).GetProperty(filter.FieldName ?? filter.Field);
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(item);
                    if (!MatchesFilter(value, filter))
                        return false;
                }
            }
            return true;
        }).ToList();
    }

    private bool MatchesFilter(object? value, FilterRule filter)
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
            _ => true
        };
    }

    private async Task HandleViewLoaded(ViewState? state)
    {
        if (state == null)
        {
            // Reset to defaults
            InitializeColumnsFromDefinitions();
            activeFilters.Clear();
            ApplyFilters();
        }
        else
        {
            currentViewState = state;
            if (state.Columns.Any())
            {
                managedColumns = state.Columns;
            }
            if (state.Filters.Any())
            {
                activeFilters = state.Filters;
                ApplyFilters();
            }
        }
        hasUnsavedChanges = false;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && EnableColumnManagement)
        {
            await JSRuntime.InvokeVoidAsync("initColumnResize");
        }
    }
}