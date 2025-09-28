using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared;

public partial class GenericCard<TItem> : ComponentBase where TItem : class
{
    [Parameter] public TItem Item { get; set; } = default!;
    [Parameter] public Func<TItem, string>? TitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? SubtitleSelector { get; set; }
    [Parameter] public Func<TItem, string>? DescriptionSelector { get; set; }
    [Parameter] public Func<TItem, string?>? ImageUrlSelector { get; set; }
    [Parameter] public Func<TItem, string?>? StatusSelector { get; set; }
    [Parameter] public Func<TItem, string?>? BadgeSelector { get; set; }
    [Parameter] public RenderFragment<TItem>? CustomContent { get; set; }
    [Parameter] public EventCallback<TItem> OnClick { get; set; }
    [Parameter] public EventCallback<TItem> OnDoubleClick { get; set; }
    [Parameter] public bool IsSelectable { get; set; } = false;
    [Parameter] public bool IsSelected { get; set; } = false;
    [Parameter] public EventCallback<(TItem, bool)> OnSelectionChanged { get; set; }

    private async Task HandleClick()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(Item);
        }
    }

    private async Task HandleDoubleClick()
    {
        if (OnDoubleClick.HasDelegate)
        {
            await OnDoubleClick.InvokeAsync(Item);
        }
    }

    private async Task ToggleSelection()
    {
        if (IsSelectable)
        {
            IsSelected = !IsSelected;
            if (OnSelectionChanged.HasDelegate)
            {
                await OnSelectionChanged.InvokeAsync((Item, IsSelected));
            }
        }
    }

    // Additional missing properties and methods
    [Parameter] public string AdditionalCssClass { get; set; } = "";
    [Parameter] public CardConfig? Config { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    public class CardConfig
    {
        public bool ShowIcon { get; set; } = true;
        public bool ShowStatus { get; set; } = true;
        public bool ShowActions { get; set; } = true;
        public string Theme { get; set; } = "default";
        public List<CardStat> Stats { get; set; } = new();
        public List<CardAction> Actions { get; set; } = new();
    }

    public class CardStat
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public class CardAction
    {
        public string Label { get; set; } = "";
        public string Icon { get; set; } = "";
        public EventCallback Callback { get; set; }
        public string CssClass { get; set; } = "";
        public EventCallback OnClick { get; set; }
    }

    private string GetIcon()
    {
        if (ImageUrlSelector != null)
        {
            var url = ImageUrlSelector(Item);
            if (!string.IsNullOrEmpty(url))
                return url;
        }
        return "fas fa-file";
    }

    private string GetPrimaryText()
    {
        if (TitleSelector != null)
            return TitleSelector(Item) ?? "";
        return "";
    }

    private string GetSecondaryText()
    {
        if (SubtitleSelector != null)
            return SubtitleSelector(Item) ?? "";
        return "";
    }

    private string GetSecondaryIcon()
    {
        return "fas fa-info-circle";
    }

    private string GetTertiaryIcon()
    {
        return "fas fa-clock";
    }

    private string GetStatusText()
    {
        if (StatusSelector != null)
            return StatusSelector(Item) ?? "";
        return "";
    }

    private string GetStatusClass()
    {
        var status = GetStatusText().ToLower();
        return status switch
        {
            "active" => "status-active",
            "completed" => "status-completed",
            "pending" => "status-pending",
            _ => "status-default"
        };
    }

    private string GetStatValue()
    {
        return "0";
    }

    private string GetStatValue(CardStat stat)
    {
        return stat?.Value ?? "0";
    }

    private bool ShowSelection => IsSelectable;

    private bool HasStatus()
    {
        return StatusSelector != null && !string.IsNullOrEmpty(StatusSelector(Item));
    }

    private async Task HandleSelectionChange(bool isChecked)
    {
        IsSelected = isChecked;
        if (OnSelectionChanged.HasDelegate)
        {
            await OnSelectionChanged.InvokeAsync((Item, IsSelected));
        }
    }

    private string GetTertiaryText()
    {
        if (DescriptionSelector != null)
            return DescriptionSelector(Item) ?? "";
        return "";
    }
}