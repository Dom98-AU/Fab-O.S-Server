using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared.Interfaces;

public interface IToolbarActionProvider
{
    ToolbarActionGroup GetActions();
}

public class ToolbarActionGroup
{
    public List<ToolbarAction> PrimaryActions { get; set; } = new();
    public List<ToolbarAction> MenuActions { get; set; } = new();
    public List<ToolbarAction> RelatedActions { get; set; } = new();
}

public class ToolbarAction : IToolbarAction
{
    public string Text { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Func<Task>? ActionFunc { get; set; }
    public EventCallback Action { get; set; }
    public bool IsDisabled { get; set; } = false;
    public bool IsEnabled { get => !IsDisabled; set => IsDisabled = !value; }
    public bool RequiresSelection { get; set; } = false;
    public string Tooltip { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string NavigateUrl { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public bool IsSeparator { get; set; } = false;
    public ToolbarActionStyle Style { get; set; } = ToolbarActionStyle.Default;
}

public enum ToolbarActionStyle
{
    Default,
    Primary,
    Secondary,
    Success,
    Warning,
    Danger
}
