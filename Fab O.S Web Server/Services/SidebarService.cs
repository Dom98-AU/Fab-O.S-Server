using Microsoft.JSInterop;

namespace FabOS.WebServer.Services;

/// <summary>
/// Service for managing sidebar state (collapse, expand, pin mode, pinned items)
/// Persists pinned items to browser localStorage
/// </summary>
public class SidebarService
{
    private readonly IJSRuntime _jsRuntime;
    private const string STORAGE_KEY = "fabos_pinned_items";

    private HashSet<string> _pinnedUrls = new();

    public bool IsCollapsed { get; private set; }
    public bool IsExpanded { get; private set; }
    public bool IsPinMode { get; private set; }
    public string CurrentModule { get; private set; } = "Trace";
    public bool ShowModuleSelector { get; private set; }

    public event Action? OnStateChanged;

    public SidebarService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize sidebar state from localStorage
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var pinnedJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
            if (!string.IsNullOrEmpty(pinnedJson))
            {
                var pinnedUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(pinnedJson);
                if (pinnedUrls != null)
                {
                    _pinnedUrls = new HashSet<string>(pinnedUrls);
                }
            }
        }
        catch (Exception)
        {
            // localStorage not available or JSON parse error - ignore
        }
    }

    /// <summary>
    /// Toggle sidebar collapsed state
    /// </summary>
    public async Task ToggleCollapsed()
    {
        Console.WriteLine($"========================================");
        Console.WriteLine($"[SidebarService] ⚡ ToggleCollapsed called!");
        Console.WriteLine($"[SidebarService] Current state - IsCollapsed: {IsCollapsed}, IsExpanded: {IsExpanded}");

        IsCollapsed = !IsCollapsed;

        // If collapsing, exit expanded mode
        if (IsCollapsed)
        {
            IsExpanded = false;
            IsPinMode = false;
            Console.WriteLine($"[SidebarService] ✅ Sidebar is now COLLAPSED");
        }
        else
        {
            Console.WriteLine($"[SidebarService] ✅ Sidebar is now EXPANDED (normal)");
        }

        Console.WriteLine($"[SidebarService] New state - IsCollapsed: {IsCollapsed}, IsExpanded: {IsExpanded}");
        Console.WriteLine($"[SidebarService] 🔄 Calling JavaScript to update body classes...");

        // Update body classes via JavaScript
        await UpdateSidebarBodyClassAsync();

        Console.WriteLine($"[SidebarService] ✓ JavaScript update completed");
        Console.WriteLine($"========================================");

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Update body classes via JavaScript to sync CSS variables
    /// </summary>
    private async Task UpdateSidebarBodyClassAsync()
    {
        try
        {
            Console.WriteLine($"[SidebarService] 📞 UpdateSidebarBodyClassAsync - Preparing to call JavaScript");
            Console.WriteLine($"[SidebarService] 📊 Parameters: isCollapsed={IsCollapsed}, isExpanded={IsExpanded}");
            Console.WriteLine($"[SidebarService] 🔧 Calling JSRuntime.InvokeVoidAsync('updateSidebarBodyClass', {IsCollapsed}, {IsExpanded})...");

            await _jsRuntime.InvokeVoidAsync("updateSidebarBodyClass", IsCollapsed, IsExpanded);

            Console.WriteLine($"[SidebarService] ✅ JavaScript call completed successfully");
            Console.WriteLine($"[SidebarService] 💡 Expected body class: {(IsCollapsed ? "sidebar-collapsed" : IsExpanded ? "sidebar-expanded" : "none")}");
            Console.WriteLine($"[SidebarService] 💡 Expected --main-sidebar-width: {(IsCollapsed ? "60px" : IsExpanded ? "420px" : "280px")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SidebarService] ❌ UpdateSidebarBodyClassAsync - ERROR: {ex.Message}");
            Console.WriteLine($"[SidebarService] Stack trace: {ex.StackTrace}");
            // Ignore JavaScript errors during component disposal or when JS isn't available
        }
    }

    /// <summary>
    /// Toggle sidebar expanded state
    /// </summary>
    public async Task ToggleExpanded()
    {
        IsExpanded = !IsExpanded;

        // If expanding, ensure not collapsed
        if (IsExpanded)
        {
            IsCollapsed = false;
        }

        // Exit pin mode when leaving expanded view
        if (!IsExpanded)
        {
            IsPinMode = false;
        }

        // Update body classes via JavaScript
        await UpdateSidebarBodyClassAsync();

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Toggle pin mode (only available in expanded view)
    /// </summary>
    public void TogglePinMode()
    {
        if (!IsExpanded) return;

        IsPinMode = !IsPinMode;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Set the current active module
    /// </summary>
    public void SetModule(string module)
    {
        CurrentModule = module;
        ShowModuleSelector = false;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Toggle module selector dropdown
    /// </summary>
    public void ToggleModuleSelector()
    {
        ShowModuleSelector = !ShowModuleSelector;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Close module selector dropdown
    /// </summary>
    public void CloseModuleSelector()
    {
        ShowModuleSelector = false;
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Toggle pin/unpin a navigation item
    /// </summary>
    public async Task TogglePinAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        if (_pinnedUrls.Contains(url))
        {
            _pinnedUrls.Remove(url);
        }
        else
        {
            _pinnedUrls.Add(url);
        }

        await SavePinnedItemsAsync();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Check if a URL is pinned
    /// </summary>
    public bool IsPinned(string url) => _pinnedUrls.Contains(url);

    /// <summary>
    /// Get all pinned URLs
    /// </summary>
    public IEnumerable<string> GetPinnedUrls() => _pinnedUrls.ToList();

    /// <summary>
    /// Save pinned items to localStorage
    /// </summary>
    private async Task SavePinnedItemsAsync()
    {
        try
        {
            var pinnedJson = System.Text.Json.JsonSerializer.Serialize(_pinnedUrls.ToList());
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, pinnedJson);
        }
        catch (Exception)
        {
            // localStorage not available - ignore
        }
    }
}
