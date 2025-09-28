using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared;

public partial class GenericListView<TItem> : ComponentBase where TItem : class
{
    [Parameter] public List<TItem> Items { get; set; } = new();
    [Parameter] public Func<TItem, string>? TitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? SubtitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? DescriptionSelector { get; set; }
    [Parameter] public Func<TItem, string?>? IconSelector { get; set; }
    [Parameter] public Func<TItem, string?>? StatusSelector { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomListItemContent { get; set; }
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }
    [Parameter] public EventCallback<TItem> OnItemDoubleClick { get; set; }
    [Parameter] public bool AllowSelection { get; set; } = false;
    [Parameter] public bool MultiSelect { get; set; } = true;
    [Parameter] public List<TItem> SelectedItems { get; set; } = new();
    [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }
    [Parameter] public string CssClass { get; set; } = "";
    [Parameter] public bool ShowCheckboxes { get; set; } = false;
    [Parameter] public RenderFragment<TItem>? ItemIcon { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemTitle { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemSubtitle { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemStatus { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemDetails { get; set; }
    [Parameter] public Func<TItem, bool>? IsSelected { get; set; }
    [Parameter] public RenderFragment<TItem>? ItemActions { get; set; }
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    private bool ShowSelection => AllowSelection || ShowCheckboxes;

    private async Task HandleItemClick(TItem item)
    {
        if (AllowSelection)
        {
            if (MultiSelect)
            {
                if (SelectedItems.Contains(item))
                {
                    SelectedItems.Remove(item);
                }
                else
                {
                    SelectedItems.Add(item);
                }
            }
            else
            {
                SelectedItems.Clear();
                SelectedItems.Add(item);
            }

            if (SelectedItemsChanged.HasDelegate)
            {
                await SelectedItemsChanged.InvokeAsync(SelectedItems);
            }
        }

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

    private bool IsItemSelected(TItem item)
    {
        return SelectedItems.Contains(item);
    }

    private async Task HandleSelectionChanged(TItem item, bool isChecked)
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

    private async Task ToggleItemSelection(TItem item)
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
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        }
    }
}