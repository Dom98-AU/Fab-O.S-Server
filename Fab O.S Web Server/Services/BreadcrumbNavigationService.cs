using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using static FabOS.WebServer.Components.Shared.Breadcrumb;

namespace FabOS.WebServer.Services;

/// <summary>
/// Service for automatically generating hierarchical breadcrumb navigation based on URL structure.
/// Uses NavigationManager to detect route changes and generates breadcrumbs from URL segments.
/// </summary>
public class BreadcrumbNavigationService : IDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<BreadcrumbNavigationService> _logger;

    // Cache for entity names to prevent repeated database queries
    private readonly Dictionary<(string entityType, int id), string> _entityNameCache = new();

    public event Action? OnBreadcrumbsChanged;

    private List<BreadcrumbItem> _currentBreadcrumbs = new();

    public BreadcrumbNavigationService(
        NavigationManager navigationManager,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<BreadcrumbNavigationService> logger)
    {
        _navigationManager = navigationManager;
        _dbContextFactory = dbContextFactory;
        _logger = logger;

        // Subscribe to location changes
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Gets the current breadcrumb trail
    /// </summary>
    public List<BreadcrumbItem> GetBreadcrumbs() => _currentBreadcrumbs;

    /// <summary>
    /// Builds breadcrumbs from the current URL
    /// </summary>
    public async Task<List<BreadcrumbItem>> GetBreadcrumbsAsync()
    {
        try
        {
            var uri = new Uri(_navigationManager.Uri);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // URL structure: /{tenantSlug}/{module}/{resource}/{id}/{sub-resource}
            // Example: /acme-steel/trace/takeoffs/123/packages

            if (segments.Length < 2)
            {
                // Not enough segments for breadcrumbs
                return new List<BreadcrumbItem>();
            }

            var breadcrumbs = new List<BreadcrumbItem>();
            var tenantSlug = segments[0];

            // Start from index 1 to skip tenant slug
            for (int i = 1; i < segments.Length; i++)
            {
                var segment = segments[i];
                var url = $"/{string.Join("/", segments.Take(i + 1))}";

                // Module level (e.g., "trace" → "Trace")
                if (i == 1)
                {
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Label = CapitalizeModule(segment),
                        Url = url,
                        IsActive = false
                    });
                    continue;
                }

                // Try to parse as ID
                if (int.TryParse(segment, out int id))
                {
                    // This is an entity ID - get friendly name from database
                    var entityType = segments[i - 1]; // e.g., "takeoffs", "customers"
                    var name = await GetEntityNameAsync(entityType, id);

                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Label = name,
                        Url = url,
                        IsActive = false
                    });
                }
                else
                {
                    // Resource level or action (e.g., "takeoffs", "edit", "new")
                    breadcrumbs.Add(new BreadcrumbItem
                    {
                        Label = CapitalizeResource(segment),
                        Url = url,
                        IsActive = false
                    });
                }
            }

            // Mark the last item as active
            if (breadcrumbs.Any())
            {
                breadcrumbs[^1].IsActive = true;
            }

            _currentBreadcrumbs = breadcrumbs;
            return breadcrumbs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating breadcrumbs for URL: {Url}", _navigationManager.Uri);
            return new List<BreadcrumbItem>();
        }
    }

    /// <summary>
    /// Handles navigation events
    /// </summary>
    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        await GetBreadcrumbsAsync();
        OnBreadcrumbsChanged?.Invoke();
    }

    /// <summary>
    /// Gets a friendly name for an entity from the database (with caching)
    /// </summary>
    private async Task<string> GetEntityNameAsync(string entityType, int id)
    {
        // Check cache first
        var cacheKey = (entityType.ToLower(), id);
        if (_entityNameCache.TryGetValue(cacheKey, out var cachedName))
        {
            return cachedName;
        }

        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            string name = entityType.ToLower() switch
            {
                "takeoffs" => await context.Takeoffs
                    .Where(t => t.Id == id)
                    .Select(t => t.TakeoffNumber ?? $"Takeoff #{id}")
                    .FirstOrDefaultAsync() ?? $"Takeoff #{id}",

                "customers" => await context.Customers
                    .Where(c => c.Id == id)
                    .Select(c => c.Name ?? $"Customer #{id}")
                    .FirstOrDefaultAsync() ?? $"Customer #{id}",

                "contacts" => await context.CustomerContacts
                    .Where(c => c.Id == id)
                    .Select(c => $"{c.FirstName} {c.LastName}")
                    .FirstOrDefaultAsync() ?? $"Contact #{id}",

                "packages" => await context.Packages
                    .Where(p => p.Id == id)
                    .Select(p => p.PackageNumber ?? $"Package #{id}")
                    .FirstOrDefaultAsync() ?? $"Package #{id}",

                "catalogues" => await context.Catalogues
                    .Where(c => c.Id == id)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync() ?? $"Catalogue #{id}",

                "catalogue-items" => await context.CatalogueItems
                    .Where(c => c.Id == id)
                    .Select(c => c.ItemCode ?? $"Item #{id}")
                    .FirstOrDefaultAsync() ?? $"Item #{id}",

                _ => $"#{id}"
            };

            // Cache the result
            _entityNameCache[cacheKey] = name;
            return name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity name for {EntityType} #{Id}", entityType, id);
            return $"#{id}";
        }
    }

    /// <summary>
    /// Capitalizes module names
    /// </summary>
    private string CapitalizeModule(string module)
    {
        return module.ToLower() switch
        {
            "trace" => "Trace",
            "estimate" => "Estimate",
            "fabmate" => "FabMate",
            "qdocs" => "QDocs",
            "assets" => "Assets",
            "settings" => "Settings",
            _ => CapitalizeFirst(module)
        };
    }

    /// <summary>
    /// Capitalizes resource names and handles special cases
    /// </summary>
    private string CapitalizeResource(string resource)
    {
        return resource.ToLower() switch
        {
            "takeoffs" => "Takeoffs",
            "customers" => "Customers",
            "contacts" => "Contacts",
            "packages" => "Packages",
            "catalogues" => "Catalogues",
            "catalogue-items" => "Catalogue Items",
            "new" => "New",
            "edit" => "Edit",
            "details" => "Details",
            "files" => "Files",
            "sharepoint" => "SharePoint",
            _ => CapitalizeFirst(resource.Replace("-", " "))
        };
    }

    /// <summary>
    /// Capitalizes the first letter of a string
    /// </summary>
    private string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return char.ToUpper(text[0]) + text.Substring(1);
    }

    /// <summary>
    /// Clears the entity name cache
    /// </summary>
    public void ClearCache()
    {
        _entityNameCache.Clear();
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
