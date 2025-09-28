using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared;

public partial class GenericCardView<TItem> : ComponentBase where TItem : class
{
    [Parameter] public List<TItem> Items { get; set; } = new();
    [Parameter] public Func<TItem, string>? TitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? SubtitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? DescriptionSelector { get; set; }
    [Parameter] public Func<TItem, string?>? ImageUrlSelector { get; set; }
    [Parameter] public Func<TItem, string?>? StatusSelector { get; set; }
    [Parameter] public Func<TItem, string?>? BadgeSelector { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomCardContent { get; set; }
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<TItem> OnItemDoubleClick { get; set; }
    [Parameter] public bool AllowSelection { get; set; } = false;
    [Parameter] public List<TItem> SelectedItems { get; set; } = new();
    [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public string CssClass { get; set; } = "";

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

    private async Task HandleSelectionChanged((TItem item, bool selected) args)
    {
        if (args.selected && !SelectedItems.Contains(args.item))
        {
            SelectedItems.Add(args.item);
        }
        else if (!args.selected && SelectedItems.Contains(args.item))
        {
            SelectedItems.Remove(args.item);
        }

        if (SelectedItemsChanged.HasDelegate)
        {
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
    }

    private bool IsItemSelected(TItem item)
    {
        return SelectedItems.Contains(item);
    }

    private bool ShowSelection => AllowSelection;

    private bool IsSelected(TItem item)
    {
        return SelectedItems.Contains(item);
    }

    private async Task OnSelectionChanged((TItem item, bool selected) args)
    {
        await HandleSelectionChanged(args);
    }

    [Parameter] public GenericCard<TItem>.CardConfig? CardConfig { get; set; }

    [Parameter] public RenderFragment? EmptyContent { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemActions { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomCardActions { get; set; }
}