using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared.Interfaces;

public interface IToolbarAction
{
    string Label { get; set; }
    string Icon { get; set; }
    string Tooltip { get; set; }
    string Badge { get; set; }
    string NavigateUrl { get; set; }
    bool IsVisible { get; set; }
    bool IsEnabled { get; set; }
    bool IsSeparator { get; set; }
    EventCallback Action { get; set; }
}

public class ToolbarActionItem : IToolbarAction
{
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public string Badge { get; set; } = "";
    public string NavigateUrl { get; set; } = "";
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; } = false;
    public EventCallback Action { get; set; }
}
