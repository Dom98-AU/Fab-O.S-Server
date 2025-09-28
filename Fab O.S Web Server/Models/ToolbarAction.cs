using Microsoft.AspNetCore.Components;
using FabOS.WebServer.Components.Shared.Interfaces;

namespace FabOS.WebServer.Models;

public class ToolbarAction : IToolbarAction
{
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public string NavigateUrl { get; set; } = "";
    public EventCallback OnClick { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsPrimary { get; set; } = false;
    public string Tooltip { get; set; } = "";
    public string Badge { get; set; } = "";
    public bool IsSeparator { get; set; } = false;
    public EventCallback Action { get; set; }
}