namespace FabOS.WebServer.Models;

/// <summary>
/// Represents a navigation item in the sidebar menu
/// </summary>
public class NavigationItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = ""; // Lucide icon name
    public string Url { get; set; } = "";
    public string Category { get; set; } = ""; // QuickAction, Module, Recent
    public string Module { get; set; } = ""; // Trace, Estimate, FabMate, QDocs, Settings
    public int? BadgeCount { get; set; }
    public BadgeType? BadgeType { get; set; }
}

/// <summary>
/// Represents a module definition (app or settings module)
/// </summary>
public class ModuleDefinition
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = ""; // Lucide icon name
    public string Description { get; set; } = "";
    public string Type { get; set; } = "app"; // app or settings
}

/// <summary>
/// Badge type for navigation items
/// </summary>
public enum BadgeType
{
    Default,
    Success,
    Warning,
    Error,
    Info
}
