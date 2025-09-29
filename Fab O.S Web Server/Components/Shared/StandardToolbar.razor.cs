using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Components.Shared.Interfaces;

namespace FabOS.WebServer.Components.Shared;

public enum PageType
{
    List,           // List pages with search bar
    Card,           // Detail pages without search bar
    Document,       // Document pages without search bar
    Worksheet,      // Worksheet pages without search bar
    SharePointFiles // SharePoint file browser pages with folder navigation
}

public partial class StandardToolbar : ComponentBase
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Subtitle { get; set; } = "";
    [Parameter] public List<IToolbarAction>? Actions { get; set; }
    [Parameter] public List<IToolbarAction>? ContextActions { get; set; }
    [Parameter] public RenderFragment? LeftContent { get; set; }
    [Parameter] public RenderFragment? CenterContent { get; set; }
    [Parameter] public RenderFragment? RightContent { get; set; }
    [Parameter] public bool ShowSearch { get; set; } = true;
    [Parameter] public string SearchPlaceholder { get; set; } = "Search...";
    [Parameter] public EventCallback<string> OnSearch { get; set; }
    [Parameter] public bool ShowViewSwitcher { get; set; } = false;
    [Parameter] public EventCallback<string> OnViewChanged { get; set; }
    [Parameter] public string CssClass { get; set; } = "";

    private string searchTerm = "";
    private string currentView = "card";
    private bool showActionsMenu = false;
    private bool showRelatedMenu = false;

    [Parameter] public IToolbarActionProvider? ActionProvider { get; set; }
    [Parameter] public EventCallback<string> OnSearchChanged { get; set; }
    [Parameter] public PageType PageType { get; set; } = PageType.List;
    [Parameter] public string? Breadcrumb { get; set; }

    private string SearchTerm
    {
        get => searchTerm;
        set
        {
            searchTerm = value;
            if (OnSearchChanged.HasDelegate)
            {
                _ = OnSearchChanged.InvokeAsync(value);
            }
        }
    }

    private async Task HandleSearch()
    {
        if (OnSearch.HasDelegate)
        {
            await OnSearch.InvokeAsync(searchTerm);
        }
    }

    private async Task HandleViewChange(string view)
    {
        currentView = view;
        if (OnViewChanged.HasDelegate)
        {
            await OnViewChanged.InvokeAsync(view);
        }
    }

    private async Task ExecuteAction(IToolbarAction action)
    {
        if (action.Action.HasDelegate)
        {
            await action.Action.InvokeAsync();
        }
    }

    private void ToggleActionsMenu()
    {
        showActionsMenu = !showActionsMenu;
        if (showActionsMenu)
            showRelatedMenu = false;
        StateHasChanged();
    }

    private void ToggleRelatedMenu()
    {
        showRelatedMenu = !showRelatedMenu;
        if (showRelatedMenu)
            showActionsMenu = false;
        StateHasChanged();
    }

    private void ClearSearch()
    {
        SearchTerm = "";
        StateHasChanged();
    }
}