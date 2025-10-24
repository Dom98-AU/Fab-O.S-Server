using FabOS.WebServer.Models;

namespace FabOS.WebServer.Services;

/// <summary>
/// Service for providing navigation items and module definitions
/// Centralizes all navigation data for the sidebar
/// </summary>
public class NavigationService
{
    private readonly List<ModuleDefinition> _modules = new()
    {
        new() { Name = "Trace", Icon = "ruler", Description = "Drawing Analysis & Takeoff", Type = "app" },
        new() { Name = "Estimate", Icon = "dollar-sign", Description = "Cost Estimation & Quotes", Type = "app" },
        new() { Name = "FabMate", Icon = "wrench", Description = "Production & Workflow", Type = "app" },
        new() { Name = "QDocs", Icon = "clipboard-list", Description = "Quality Documentation", Type = "app" },
    };

    private readonly List<ModuleDefinition> _settingsModules = new()
    {
        new() { Name = "Settings", Icon = "settings", Description = "Global Configuration", Type = "settings" },
    };

    private readonly List<NavigationItem> _allItems = new()
    {
        // Quick Actions
        new() {
            Label = "New Takeoff",
            Title = "New Takeoff",
            Description = "Start a new takeoff",
            Icon = "plus-circle",
            Url = "/takeoffs",
            Category = "QuickAction"
        },
        new() {
            Label = "Upload Drawing",
            Title = "Upload Drawing",
            Description = "Upload new drawings",
            Icon = "upload",
            Url = "/takeoffs/drawings",
            Category = "QuickAction"
        },
        new() {
            Label = "View Database",
            Title = "View Database",
            Description = "Database management",
            Icon = "database",
            Url = "/database",
            Category = "QuickAction"
        },

        // Trace Module
        new() {
            Label = "Takeoffs",
            Title = "Takeoffs",
            Description = "Manage takeoffs",
            Icon = "ruler",
            Url = "/takeoffs",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Drawings",
            Title = "Drawing Management",
            Description = "Manage drawings",
            Icon = "file-text",
            Url = "/trace/drawings",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Drawing Library",
            Title = "Drawing Library",
            Description = "Browse drawings",
            Icon = "folder",
            Url = "/drawing-management",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "PDF Viewer",
            Title = "PDF Viewer",
            Description = "View PDF files",
            Icon = "file-search",
            Url = "/test-pdf-viewer",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Takeoff Manager",
            Title = "Takeoff Manager",
            Description = "Manage takeoffs",
            Icon = "clipboard-list",
            Url = "/takeoffs/drawings",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Database",
            Title = "Database",
            Description = "Database management",
            Icon = "database",
            Url = "/database",
            Category = "Module",
            Module = "Trace"
        },

        // Estimate Module
        new() {
            Label = "Quotes",
            Title = "Quotes",
            Description = "Manage quotes",
            Icon = "receipt",
            Url = "/estimate/quotes",
            Category = "Module",
            Module = "Estimate"
        },
        new() {
            Label = "Estimations",
            Title = "Estimations",
            Description = "Manage estimations",
            Icon = "calculator",
            Url = "/estimate/estimations",
            Category = "Module",
            Module = "Estimate"
        },
        new() {
            Label = "Orders",
            Title = "Orders",
            Description = "Manage orders",
            Icon = "clipboard-list",
            Url = "/estimate/orders",
            Category = "Module",
            Module = "Estimate"
        },

        // FabMate Module
        new() {
            Label = "Work Orders",
            Title = "Work Orders",
            Description = "Manage work orders",
            Icon = "hammer",
            Url = "/fabmate/workorders",
            Category = "Module",
            Module = "FabMate"
        },
        new() {
            Label = "Production",
            Title = "Production",
            Description = "Production management",
            Icon = "factory",
            Url = "/fabmate/production",
            Category = "Module",
            Module = "FabMate"
        },
        new() {
            Label = "Resources",
            Title = "Resources",
            Description = "Manage resources",
            Icon = "users",
            Url = "/fabmate/resources",
            Category = "Module",
            Module = "FabMate"
        },

        // QDocs Module
        new() {
            Label = "Documents",
            Title = "Documents",
            Description = "Manage documents",
            Icon = "clipboard-list",
            Url = "/qdocs/documents",
            Category = "Module",
            Module = "QDocs"
        },
        new() {
            Label = "Templates",
            Title = "Templates",
            Description = "Manage templates",
            Icon = "file-text",
            Url = "/qdocs/templates",
            Category = "Module",
            Module = "QDocs"
        },
        new() {
            Label = "Compliance",
            Title = "Compliance",
            Description = "Compliance tracking",
            Icon = "check-circle",
            Url = "/qdocs/compliance",
            Category = "Module",
            Module = "QDocs"
        },

        // Settings Module
        new() {
            Label = "Home",
            Title = "Home",
            Description = "Dashboard home",
            Icon = "home",
            Url = "/",
            Category = "Module",
            Module = "Settings"
        },
        new() {
            Label = "Login Test",
            Title = "Login Test",
            Description = "Test login functionality",
            Icon = "log-in",
            Url = "/test-login",
            Category = "Module",
            Module = "Settings"
        },
        new() {
            Label = "Company",
            Title = "Company Settings",
            Description = "Configure company details",
            Icon = "building",
            Url = "/settings/company",
            Category = "Module",
            Module = "Settings"
        },
        new() {
            Label = "Users",
            Title = "User Management",
            Description = "Manage users",
            Icon = "users",
            Url = "/settings/users",
            Category = "Module",
            Module = "Settings"
        },
        new() {
            Label = "Security",
            Title = "Security Settings",
            Description = "Security configuration",
            Icon = "lock",
            Url = "/settings/security",
            Category = "Module",
            Module = "Settings"
        },

        // Recent Items
        new() {
            Label = "Recent Takeoffs",
            Title = "Recent Takeoffs",
            Description = "View recent takeoffs",
            Icon = "ruler",
            Url = "/takeoffs",
            Category = "Recent"
        },
        new() {
            Label = "Test PDF",
            Title = "Test PDF",
            Description = "PDF viewer test",
            Icon = "file-search",
            Url = "/test-pdf-viewer",
            Category = "Recent"
        },
        new() {
            Label = "Drawing Library",
            Title = "Drawing Library",
            Description = "Browse drawings",
            Icon = "folder",
            Url = "/drawing-management",
            Category = "Recent"
        },
    };

    /// <summary>
    /// Get all app modules (non-settings)
    /// </summary>
    public IEnumerable<ModuleDefinition> GetAppModules() => _modules;

    /// <summary>
    /// Get all settings modules
    /// </summary>
    public IEnumerable<ModuleDefinition> GetSettingsModules() => _settingsModules;

    /// <summary>
    /// Get quick action items
    /// </summary>
    public IEnumerable<NavigationItem> GetQuickActions()
        => _allItems.Where(i => i.Category == "QuickAction");

    /// <summary>
    /// Get navigation items for a specific module
    /// </summary>
    public IEnumerable<NavigationItem> GetModuleItems(string module)
        => _allItems.Where(i => i.Category == "Module" && i.Module == module);

    /// <summary>
    /// Get recent items
    /// </summary>
    public IEnumerable<NavigationItem> GetRecentItems()
        => _allItems.Where(i => i.Category == "Recent");

    /// <summary>
    /// Find a navigation item by URL
    /// </summary>
    public NavigationItem? FindItemByUrl(string url)
        => _allItems.FirstOrDefault(i => i.Url == url);
}
