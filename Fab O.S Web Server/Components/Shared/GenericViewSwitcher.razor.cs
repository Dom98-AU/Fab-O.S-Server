using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared;

public partial class GenericViewSwitcher<TItem> : ComponentBase where TItem : class
{
    [Parameter] public List<TItem> Items { get; set; } = new();
    [Parameter] public ViewType CurrentView { get; set; } = ViewType.Card;
    [Parameter] public EventCallback<ViewType> CurrentViewChanged { get; set; }
    [Parameter] public Func<TItem, string>? TitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? SubtitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? DescriptionSelector { get; set; }
    [Parameter] public Func<TItem, string?>? ImageUrlSelector { get; set; }
    [Parameter] public Func<TItem, string?>? StatusSelector { get; set; }
    [Parameter] public Func<TItem, string?>? BadgeSelector { get; set; }
    [Parameter] public List<GenericTableView<TItem>.TableColumn<TItem>>? TableColumns { get; set; }
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<TItem> OnItemDoubleClick { get; set; }
    [Parameter] public bool AllowSelection { get; set; } = false;
    [Parameter] public bool MultiSelect { get; set; } = true;
    [Parameter] public List<TItem> SelectedItems { get; set; } = new();
    [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomCardContent { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomListItemContent { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomTableRowContent { get; set; }
    [Parameter] public string CssClass { get; set; } = "";

    public enum ViewType
    {
        Card,
        List,
        Table
    }

    private async Task SwitchView(ViewType viewType)
    {
        CurrentView = viewType;
        if (CurrentViewChanged.HasDelegate)
        {
            await CurrentViewChanged.InvokeAsync(CurrentView);
        }
        StateHasChanged();
    }

    private string GetViewIcon(ViewType viewType)
    {
        return viewType switch
        {
            ViewType.Card => "fas fa-th-large",
            ViewType.List => "fas fa-list",
            ViewType.Table => "fas fa-table",
            _ => "fas fa-th-large"
        };
    }

    private bool IsViewActive(ViewType viewType)
    {
        return CurrentView == viewType;
    }

    [Parameter] public bool ShowViewPreferences { get; set; } = false;
    [Parameter] public EventCallback OnViewPreferencesClick { get; set; }
}