using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FabOS.WebServer.Components.Shared.Common;

public partial class FabButton : ComponentBase
{
    [Parameter] public string Text { get; set; } = "";
    [Parameter] public string Icon { get; set; } = "";
    [Parameter] public string CssClass { get; set; } = "btn btn-primary";
    [Parameter] public bool IsDisabled { get; set; } = false;
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public bool IsLoading { get; set; } = false;
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string ButtonType => Type;
    private string IconClass => Icon;

    private string GetButtonClasses()
    {
        var classes = new List<string> { CssClass };

        if (IsLoading)
            classes.Add("loading");
        if (IsDisabled)
            classes.Add("disabled");

        return string.Join(" ", classes);
    }

    private async Task HandleClick(MouseEventArgs args)
    {
        if (!IsDisabled && OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(args);
        }
    }
}