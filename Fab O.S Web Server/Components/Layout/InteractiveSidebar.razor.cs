using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Components.Layout;

public partial class InteractiveSidebar : ComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ITenantService TenantService { get; set; } = default!;

    private string? tenantSlug;
    private bool IsCollapsed = false;
    private bool IsExpanded = false;
    private bool IsPinMode = false;
    private bool ShowModuleSelector = false;
    private string CurrentModule = "Trace";
    private string SearchTerm = "";
    private bool HasAdminPermissions = true; // TODO: Get from auth service

    private class NavItem
    {
        public string Label { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsPinned { get; set; } = false;
        public List<NavItem> Children { get; set; } = new();
        public bool IsExpanded { get; set; } = false;
    }

    private List<NavItem> NavigationItems = new()
    {
        new NavItem { Label = "Home", Title = "Home", Description = "Dashboard", Icon = "🏠", Url = "/" },
        new NavItem { Label = "Customers", Title = "Customers", Description = "Manage customers", Icon = "👥", Url = "/trace/customers" },
        new NavItem { Label = "Contacts", Title = "Contacts", Description = "Manage contacts", Icon = "📇", Url = "/trace/contacts" },
        new NavItem { Label = "Takeoffs", Title = "Takeoffs", Description = "Manage takeoffs", Icon = "📐", Url = "/trace/takeoffs" },
        new NavItem { Label = "Drawings", Title = "Drawing Management", Description = "Manage drawings", Icon = "📄", Url = "/trace/drawings" },
        new NavItem { Label = "Drawing Library", Title = "Drawing Library", Description = "Browse drawings", Icon = "📚", Url = "/drawing-management" },
        new NavItem { Label = "PDF Viewer", Title = "PDF Viewer", Description = "View PDF files", Icon = "📏", Url = "/test-pdf-viewer" },
        new NavItem { Label = "Database", Title = "Database", Description = "Database management", Icon = "💾", Url = "/database" }
    };

    private List<string> AvailableModules = new()
    {
        "Home", "Trace", "Estimate", "FabMate", "QDocs"
    };

    protected override async Task OnInitializedAsync()
    {
        // Get tenant slug from the current user
        tenantSlug = await TenantService.GetCurrentTenantSlugAsync();

        // Debug logging
        Console.WriteLine($"[InteractiveSidebar] OnInitializedAsync - tenantSlug: '{tenantSlug}'");

        // CRITICAL: If no tenant slug found, use a default for development
        // In production, this should redirect to a tenant selection page
        if (string.IsNullOrEmpty(tenantSlug))
        {
            Console.WriteLine("[InteractiveSidebar] WARNING: tenantSlug is null or empty!");
            Console.WriteLine("[InteractiveSidebar] Using default tenant 'default' for development");
            tenantSlug = "default"; // Default tenant for development (matches Company.Code = "DEFAULT")
        }

        // Update navigation URLs with tenant slug
        Console.WriteLine("[InteractiveSidebar] Updating navigation URLs with tenant slug");
        UpdateNavigationUrlsWithTenant();

        StateHasChanged();
    }

    private void UpdateNavigationUrlsWithTenant()
    {
        foreach (var item in NavigationItems)
        {
            // Skip test/database pages (keep them without tenant)
            if (item.Url.StartsWith("/database") || item.Url.StartsWith("/test"))
            {
                continue;
            }

            // Add tenant prefix if URL doesn't start with tenant
            if (!item.Url.StartsWith($"/{tenantSlug}"))
            {
                item.Url = $"/{tenantSlug}{item.Url}";
            }
        }
    }

    private async Task ToggleSidebar()
    {
        IsCollapsed = !IsCollapsed;
        if (IsCollapsed)
        {
            IsExpanded = false;
            ShowModuleSelector = false;
        }

        await UpdateSidebarBodyClass();
    }

    private async Task ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        if (!IsExpanded)
        {
            IsPinMode = false;
        }

        await UpdateSidebarBodyClass();
    }

    private void TogglePinMode()
    {
        IsPinMode = !IsPinMode;
    }

    private void ToggleModuleSelector()
    {
        ShowModuleSelector = !ShowModuleSelector;
    }

    private void SelectModule(string module)
    {
        CurrentModule = module;
        ShowModuleSelector = false;
        // Update navigation items based on module
        StateHasChanged();
    }

    private void TogglePinItem(NavItem item)
    {
        if (IsPinMode)
        {
            item.IsPinned = !item.IsPinned;
            StateHasChanged();
        }
    }

    private void ToggleItemExpanded(NavItem item)
    {
        item.IsExpanded = !item.IsExpanded;
        StateHasChanged();
    }

    private void FilterItems(string searchTerm)
    {
        SearchTerm = searchTerm;
        // Implement search/filter logic
        StateHasChanged();
    }

    private List<NavItem> PinnedItems => NavigationItems.Where(x => x.IsPinned).ToList();

    private void CloseModuleSelector()
    {
        ShowModuleSelector = false;
        StateHasChanged();
    }

    private void UnpinItem(NavItem item)
    {
        item.IsPinned = false;
        StateHasChanged();
    }

    private void TogglePin(NavItem item)
    {
        item.IsPinned = !item.IsPinned;
        StateHasChanged();
    }

    private List<NavItem> GetQuickActions()
    {
        var prefix = string.IsNullOrEmpty(tenantSlug) ? "" : $"/{tenantSlug}";
        return new List<NavItem>
        {
            new NavItem { Label = "New Takeoff", Title = "New Takeoff", Description = "Start a new takeoff", Icon = "➕", Url = $"{prefix}/trace/takeoffs" },
            new NavItem { Label = "Upload Drawing", Title = "Upload Drawing", Description = "Upload new drawings", Icon = "📤", Url = $"{prefix}/trace/takeoffs/drawings" },
            new NavItem { Label = "View Database", Title = "View Database", Description = "Database management", Icon = "💾", Url = "/database" }
        };
    }

    private List<NavItem> GetModuleNavigation()
    {
        var prefix = string.IsNullOrEmpty(tenantSlug) ? "" : $"/{tenantSlug}";

        // Return navigation items based on current module
        if (CurrentModule == "Settings")
        {
            return new List<NavItem>
            {
                new NavItem { Label = "Customers", Title = "Customers", Description = "Manage customers and contacts", Icon = "🏭", Url = $"{prefix}/trace/customers", IsPinned = PinnedItems.Any(p => p.Url == $"{prefix}/trace/customers") },
                new NavItem { Label = "Company", Title = "Company Settings", Description = "Configure company details", Icon = "🏢", Url = "/settings/company", IsPinned = PinnedItems.Any(p => p.Url == "/settings/company") },
                new NavItem { Label = "Users", Title = "User Management", Description = "Manage users and permissions", Icon = "👥", Url = "/settings/users", IsPinned = PinnedItems.Any(p => p.Url == "/settings/users") },
                new NavItem { Label = "Security", Title = "Security Settings", Description = "Security configuration", Icon = "🔒", Url = "/settings/security", IsPinned = PinnedItems.Any(p => p.Url == "/settings/security") }
            };
        }
        else if (CurrentModule == "Trace")
        {
            return new List<NavItem>
            {
                new NavItem { Label = "Takeoffs", Title = "Takeoffs", Description = "Manage takeoffs", Icon = "📐", Url = $"{prefix}/trace/takeoffs", IsPinned = PinnedItems.Any(p => p.Url == $"{prefix}/trace/takeoffs") },
                new NavItem { Label = "Drawings", Title = "Drawing Management", Description = "Manage drawings", Icon = "📄", Url = $"{prefix}/trace/drawings", IsPinned = PinnedItems.Any(p => p.Url == $"{prefix}/trace/drawings") },
                new NavItem { Label = "Drawing Library", Title = "Drawing Library", Description = "Browse drawings", Icon = "📚", Url = "/drawing-management", IsPinned = PinnedItems.Any(p => p.Url == "/drawing-management") },
                new NavItem { Label = "PDF Viewer", Title = "PDF Viewer", Description = "View PDF files", Icon = "📏", Url = "/test-pdf-viewer", IsPinned = PinnedItems.Any(p => p.Url == "/test-pdf-viewer") },
                new NavItem { Label = "Database", Title = "Database", Description = "Database management", Icon = "💾", Url = "/database", IsPinned = PinnedItems.Any(p => p.Url == "/database") }
            };
        }

        return NavigationItems;
    }

    private List<NavItem> GetRecentItems()
    {
        var prefix = string.IsNullOrEmpty(tenantSlug) ? "" : $"/{tenantSlug}";
        return new List<NavItem>
        {
            new NavItem { Label = "Recent Takeoffs", Title = "Recent Takeoffs", Description = "View recent takeoffs", Icon = "📐", Url = $"{prefix}/trace/takeoffs" },
            new NavItem { Label = "Test PDF", Title = "Test PDF", Description = "PDF viewer test", Icon = "📄", Url = "/test-pdf-viewer" },
            new NavItem { Label = "Drawing Library", Title = "Drawing Library", Description = "Browse drawings", Icon = "📚", Url = "/drawing-management" }
        };
    }

    private async Task UpdateSidebarBodyClass()
    {
        try
        {
            var isCollapsed = IsCollapsed;
            var isExpanded = IsExpanded;
            await JSRuntime.InvokeVoidAsync("updateSidebarBodyClass", isCollapsed, isExpanded);
        }
        catch (Exception)
        {
            // Ignore JavaScript errors during component disposal or when JS isn't available
        }
    }

    private void NavigateToModuleSettings()
    {
        ShowModuleSelector = false;
        var moduleUrl = CurrentModule.ToLower();
        NavigationManager.NavigateTo($"/settings/{moduleUrl}");
    }

    private void NavigateToGlobalSettings()
    {
        ShowModuleSelector = false;
        NavigationManager.NavigateTo("/settings/global");
    }
}