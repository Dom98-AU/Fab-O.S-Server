using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Models;
using FabOS.WebServer.Models.Filtering;
using FabOS.WebServer.Models.Columns;

namespace FabOS.WebServer.Components.Shared;

public partial class EmbeddableListPart<TItem> : ComponentBase where TItem : class
{
    // Core Parameters
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();
    [Parameter] public string CssClass { get; set; } = "";
    [Parameter] public string ItemsLabel { get; set; } = "items";
    [Parameter] public string EmptyMessage { get; set; } = "No items found";

    // Search Parameters
    [Parameter] public string SearchPlaceholder { get; set; } = "Search...";
    [Parameter] public Func<TItem, string, bool>? SearchPredicate { get; set; }
    [Parameter] public EventCallback<string> OnSearchChanged { get; set; }

    // View Parameters
    [Parameter] public ViewType DefaultView { get; set; } = ViewType.Table;
    [Parameter] public bool ShowViewSwitcher { get; set; } = true;
    [Parameter] public EventCallback<ViewType> OnViewChanged { get; set; }

    // Selection Parameters
    [Parameter] public bool AllowSelection { get; set; } = true;
    [Parameter] public List<TItem> SelectedItems { get; set; } = new();
    [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

    // Action Parameters
    [Parameter] public bool ShowActions { get; set; } = true;
    [Parameter] public IToolbarActionProvider? ActionProvider { get; set; }

    // Filter Parameters
    [Parameter] public bool ShowFilters { get; set; } = true;
    [Parameter] public IFilterProvider? FilterProvider { get; set; }
    [Parameter] public List<FilterDefinition> FilterDefinitions { get; set; } = new();
    [Parameter] public List<FilterRule> ActiveFilters { get; set; } = new();
    [Parameter] public EventCallback<List<FilterRule>> OnFiltersChanged { get; set; }

    // Column Management Parameters
    [Parameter] public bool ShowColumnManager { get; set; } = true;
    [Parameter] public List<ColumnDefinition> ColumnDefinitions { get; set; } = new();
    [Parameter] public List<ColumnDefinition>? DefaultColumns { get; set; }
    [Parameter] public EventCallback<List<ColumnDefinition>> OnColumnsChanged { get; set; }

    // Expand/Fullscreen Parameters
    [Parameter] public bool AllowExpand { get; set; } = true;
    [Parameter] public bool IsExpanded { get; set; } = false;
    [Parameter] public EventCallback<bool> OnExpandedChanged { get; set; }

    // Table View Parameters
    [Parameter] public RenderFragment<TItem>? TableActionsTemplate { get; set; }

    // Card View Parameters
    [Parameter] public Func<TItem, string>? CardTitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? CardSubtitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? CardDescriptionSelector { get; set; }
    [Parameter] public Func<TItem, string?>? CardImageSelector { get; set; }
    [Parameter] public Func<TItem, string?>? CardStatusSelector { get; set; }
    [Parameter] public Func<TItem, string?>? CardBadgeSelector { get; set; }

    // List View Parameters
    [Parameter] public RenderFragment<TItem>? ListIconTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? ListTitleTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? ListSubtitleTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? ListStatusTemplate { get; set; }
    [Parameter] public RenderFragment<TItem>? ListDetailsTemplate { get; set; }

    // Event Callbacks
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<TItem> OnItemDoubleClick { get; set; }

    // Private Fields
    private string searchTerm = "";
    private ViewType currentView;
    private List<TItem> selectedItems = new();
    private bool showActionsMenu = false;
    private bool isExpanded = false;
    private bool isModalOpening = false;

    // JavaScript Interop and Element References
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private ElementReference modalBackdrop;
    private ElementReference modalContent;
    private ElementReference expandButton;
    private ElementReference componentContainer;

    protected override void OnInitialized()
    {
        currentView = DefaultView;
        selectedItems = SelectedItems ?? new List<TItem>();
        isExpanded = IsExpanded;
    }

    protected override void OnParametersSet()
    {
        if (SelectedItems != null && !ReferenceEquals(selectedItems, SelectedItems))
        {
            selectedItems = SelectedItems;
        }

        // Sync expanded state from parameter if it changed externally
        if (isExpanded != IsExpanded)
        {
            isExpanded = IsExpanded;
        }
    }

    private List<TItem> GetFilteredItems()
    {
        var items = Items ?? Enumerable.Empty<TItem>();

        if (string.IsNullOrWhiteSpace(searchTerm))
            return items.ToList();

        if (SearchPredicate != null)
            return items.Where(item => SearchPredicate(item, searchTerm)).ToList();

        // Default search: convert to string and check if contains search term
        return items.Where(item =>
        {
            var itemString = item?.ToString() ?? "";
            return itemString.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }

    private async Task HandleSearch()
    {
        if (OnSearchChanged.HasDelegate)
        {
            await OnSearchChanged.InvokeAsync(searchTerm);
        }
        StateHasChanged();
    }

    private void ClearSearch()
    {
        searchTerm = "";
        _ = HandleSearch();
    }

    private async Task ChangeView(ViewType newView)
    {
        currentView = newView;
        if (OnViewChanged.HasDelegate)
        {
            await OnViewChanged.InvokeAsync(newView);
        }
        StateHasChanged();
    }

    private async Task HandleSelectionChanged(List<TItem> newSelection)
    {
        selectedItems = newSelection;
        if (SelectedItemsChanged.HasDelegate)
        {
            await SelectedItemsChanged.InvokeAsync(selectedItems);
        }
        StateHasChanged();
    }

    private async Task HandleItemClick(TItem item)
    {
        if (OnItemClick.HasDelegate)
        {
            await OnItemClick.InvokeAsync(item);
        }
    }

    private async Task HandleItemDoubleClick(TItem item)
    {
        if (OnItemDoubleClick.HasDelegate)
        {
            await OnItemDoubleClick.InvokeAsync(item);
        }
    }

    private void ToggleActionsMenu()
    {
        showActionsMenu = !showActionsMenu;
        StateHasChanged();
    }

    private async Task ExecuteAction(IToolbarAction action)
    {
        if (action.Action.HasDelegate)
        {
            await action.Action.InvokeAsync();
        }
        showActionsMenu = false;
        StateHasChanged();
    }

    private async Task HandleFiltersChanged(List<FilterRule> filters)
    {
        ActiveFilters = filters;
        if (OnFiltersChanged.HasDelegate)
        {
            await OnFiltersChanged.InvokeAsync(filters);
        }
        StateHasChanged();
    }

    private async Task HandleColumnsChanged(List<ColumnDefinition> columns)
    {
        ColumnDefinitions = columns;
        if (OnColumnsChanged.HasDelegate)
        {
            await OnColumnsChanged.InvokeAsync(columns);
        }
        StateHasChanged();
    }

    private async Task ToggleExpanded()
    {
        isExpanded = !isExpanded;

        // Force immediate state update
        await InvokeAsync(StateHasChanged);

        // Add small delay to ensure render completes
        await Task.Delay(50);

        // Force another state update to ensure visibility
        await InvokeAsync(StateHasChanged);

        if (OnExpandedChanged.HasDelegate)
        {
            await OnExpandedChanged.InvokeAsync(isExpanded);
        }
    }

    [JSInvokable]
    public async Task CloseModal()
    {
        if (isExpanded)
        {
            await ToggleExpanded();
        }
    }

    private async Task CloseOnBackdropClick()
    {
        if (isExpanded)
        {
            await ToggleExpanded();
        }
    }
}