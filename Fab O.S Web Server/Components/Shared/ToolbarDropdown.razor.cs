using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Components.Shared.Interfaces;

namespace FabOS.WebServer.Components.Shared;

public partial class ToolbarDropdown : ComponentBase
{
    [Parameter] public List<IToolbarAction> Actions { get; set; } = new();
    [Parameter] public string ButtonText { get; set; } = "Actions";
    [Parameter] public string ButtonIcon { get; set; } = "fas fa-ellipsis-v";
    [Parameter] public string ButtonClass { get; set; } = "";
    [Parameter] public EventCallback<IToolbarAction> OnActionExecuted { get; set; }

    private bool IsOpen = false;

    private void ToggleDropdown()
    {
        IsOpen = !IsOpen;
    }

    private void CloseDropdown()
    {
        IsOpen = false;
    }

    private async Task ExecuteAction(IToolbarAction action)
    {
        if (!action.IsEnabled)
            return;

        // Close the dropdown
        CloseDropdown();

        // Execute the action
        if (action.Action.HasDelegate)
        {
            await action.Action.InvokeAsync();
        }

        // Notify parent if callback is provided
        if (OnActionExecuted.HasDelegate)
        {
            await OnActionExecuted.InvokeAsync(action);
        }
    }

    protected override void OnInitialized()
    {
        // Ensure we have valid actions
        Actions ??= new List<IToolbarAction>();
    }
}