using FabOS.WebServer.Models;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services;

/// <summary>
/// Service for providing navigation items and module definitions
/// Centralizes all navigation data for the sidebar
/// </summary>
public class NavigationService
{
    private readonly ITenantService _tenantService;
    private string? _cachedTenantSlug;

    public NavigationService(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Get the current tenant slug (cached for performance)
    /// </summary>
    private async Task<string> GetTenantSlugAsync()
    {
        if (_cachedTenantSlug == null)
        {
            _cachedTenantSlug = await _tenantService.GetCurrentTenantSlugAsync() ?? "default";
        }
        return _cachedTenantSlug;
    }
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
            Url = "/trace/takeoffs",
            Category = "QuickAction"
        },
        new() {
            Label = "Upload Drawing",
            Title = "Upload Drawing",
            Description = "Upload new drawings",
            Icon = "upload",
            Url = "/trace/takeoffs/drawings",
            Category = "QuickAction"
        },

        // Trace Module
        new() {
            Label = "Takeoffs",
            Title = "Takeoffs",
            Description = "Manage takeoffs",
            Icon = "ruler",
            Url = "/trace/takeoffs",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Customers",
            Title = "Customers",
            Description = "Manage customers",
            Icon = "building",
            Url = "/trace/customers",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Contacts",
            Title = "Contacts",
            Description = "Manage contacts",
            Icon = "users",
            Url = "/trace/contacts",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Packages",
            Title = "Packages",
            Description = "Manage packages",
            Icon = "package",
            Url = "/trace/packages",
            Category = "Module",
            Module = "Trace"
        },
        new() {
            Label = "Catalogues",
            Title = "Catalogues",
            Description = "Manage catalogues",
            Icon = "book-open",
            Url = "/trace/catalogues",
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
            Url = "/trace/takeoffs",
            Category = "Recent"
        },
        new() {
            Label = "Recent Customers",
            Title = "Recent Customers",
            Description = "View recent customers",
            Icon = "building",
            Url = "/trace/customers",
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
    /// Get quick action items with tenant-aware URLs
    /// </summary>
    public async Task<IEnumerable<NavigationItem>> GetQuickActionsAsync()
    {
        var tenantSlug = await GetTenantSlugAsync();
        return _allItems
            .Where(i => i.Category == "QuickAction")
            .Select(i => new NavigationItem
            {
                Label = i.Label,
                Title = i.Title,
                Description = i.Description,
                Icon = i.Icon,
                Url = ApplyTenantSlug(i.Url, tenantSlug),
                Category = i.Category,
                Module = i.Module
            });
    }

    /// <summary>
    /// Get navigation items for a specific module with tenant-aware URLs
    /// </summary>
    public async Task<IEnumerable<NavigationItem>> GetModuleItemsAsync(string module)
    {
        var tenantSlug = await GetTenantSlugAsync();
        return _allItems
            .Where(i => i.Category == "Module" && i.Module == module)
            .Select(i => new NavigationItem
            {
                Label = i.Label,
                Title = i.Title,
                Description = i.Description,
                Icon = i.Icon,
                Url = ApplyTenantSlug(i.Url, tenantSlug),
                Category = i.Category,
                Module = i.Module
            });
    }

    /// <summary>
    /// Get recent items with tenant-aware URLs
    /// </summary>
    public async Task<IEnumerable<NavigationItem>> GetRecentItemsAsync()
    {
        var tenantSlug = await GetTenantSlugAsync();
        return _allItems
            .Where(i => i.Category == "Recent")
            .Select(i => new NavigationItem
            {
                Label = i.Label,
                Title = i.Title,
                Description = i.Description,
                Icon = i.Icon,
                Url = ApplyTenantSlug(i.Url, tenantSlug),
                Category = i.Category,
                Module = i.Module
            });
    }

    /// <summary>
    /// Apply tenant slug to URLs that need it (skip /database, /test-*, etc.)
    /// </summary>
    private string ApplyTenantSlug(string url, string tenantSlug)
    {
        // Skip special URLs that don't need tenant slugs
        if (url.StartsWith("/database") || url.StartsWith("/test-") || url.StartsWith("/drawing-management"))
        {
            return url;
        }

        // If URL already has tenant slug, return as-is
        if (url.StartsWith($"/{tenantSlug}/"))
        {
            return url;
        }

        // Apply tenant slug
        return $"/{tenantSlug}{url}";
    }

    // Legacy synchronous methods for backward compatibility (deprecated)
    public IEnumerable<NavigationItem> GetQuickActions()
        => _allItems.Where(i => i.Category == "QuickAction");

    public IEnumerable<NavigationItem> GetModuleItems(string module)
        => _allItems.Where(i => i.Category == "Module" && i.Module == module);

    public IEnumerable<NavigationItem> GetRecentItems()
        => _allItems.Where(i => i.Category == "Recent");

    /// <summary>
    /// Find a navigation item by URL
    /// </summary>
    public NavigationItem? FindItemByUrl(string url)
        => _allItems.FirstOrDefault(i => i.Url == url);
}
