using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared.Common;

public partial class FabCard : ComponentBase
{
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Subtitle { get; set; } = "";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public string ImageUrl { get; set; } = "";
    [Parameter] public string CssClass { get; set; } = "";
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? BodyContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public bool IsSelectable { get; set; } = false;
    [Parameter] public bool IsSelected { get; set; } = false;
    [Parameter] public RenderFragment? Actions { get; set; }

    private string GetCardClasses()
    {
        var classes = new List<string> { CssClass };

        if (IsSelectable)
            classes.Add("selectable");
        if (IsSelected)
            classes.Add("selected");

        return string.Join(" ", classes);
    }

    private async Task HandleClick()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
        }
    }
}