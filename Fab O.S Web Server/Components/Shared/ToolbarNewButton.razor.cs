using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Components.Shared.Interfaces;

namespace FabOS.WebServer.Components.Shared;

public partial class ToolbarNewButton : ComponentBase
{
    [Parameter] public List<IToolbarAction>? Actions { get; set; }
    [Parameter] public string ButtonText { get; set; } = "New";
    [Parameter] public string DefaultIcon { get; set; } = "fas fa-plus";
    [Parameter] public string? DefaultUrl { get; set; }
    [Parameter] public EventCallback DefaultAction { get; set; }
    [Parameter] public bool ForceDropdown { get; set; } = false;
    [Parameter] public EventCallback<IToolbarAction> OnActionExecuted { get; set; }

    private async Task ExecuteSingleAction(IToolbarAction action)
    {
        if (action.Action.HasDelegate)
        {
            await action.Action.InvokeAsync();
        }

        if (OnActionExecuted.HasDelegate)
        {
            await OnActionExecuted.InvokeAsync(action);
        }
    }
}