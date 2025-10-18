using static FabOS.WebServer.Components.Shared.Breadcrumb;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Central service for building breadcrumb chains using registered breadcrumb builders
/// </summary>
public class BreadcrumbBuilderService
{
    private readonly IEnumerable<IBreadcrumbBuilder> _builders;
    private readonly ILogger<BreadcrumbBuilderService> _logger;

    public BreadcrumbBuilderService(
        IEnumerable<IBreadcrumbBuilder> builders,
        ILogger<BreadcrumbBuilderService> logger)
    {
        _builders = builders;
        _logger = logger;
    }

    /// <summary>
    /// Builds a complete breadcrumb chain from entity specifications
    /// </summary>
    /// <param name="breadcrumbSpecs">Array of tuples containing (entityType, entityId, url, customLabel)</param>
    /// <returns>Array of breadcrumb items ready for display</returns>
    public async Task<BreadcrumbItem[]> BuildBreadcrumbChainAsync(
        params (string entityType, int? entityId, string? url, string? customLabel)[] breadcrumbSpecs)
    {
        var breadcrumbItems = new List<BreadcrumbItem>();

        foreach (var spec in breadcrumbSpecs)
        {
            try
            {
                // If custom label is provided, use it directly without querying database
                if (!string.IsNullOrEmpty(spec.customLabel))
                {
                    breadcrumbItems.Add(new BreadcrumbItem
                    {
                        Label = spec.customLabel,
                        Url = spec.url,
                        IsActive = false
                    });
                    continue;
                }

                // If no entity ID, create a simple label breadcrumb
                if (!spec.entityId.HasValue)
                {
                    breadcrumbItems.Add(new BreadcrumbItem
                    {
                        Label = spec.entityType,
                        Url = spec.url,
                        IsActive = false
                    });
                    continue;
                }

                // Find the appropriate builder for this entity type
                var builder = _builders.FirstOrDefault(b => b.CanHandle(spec.entityType));

                if (builder == null)
                {
                    _logger.LogWarning($"No breadcrumb builder found for entity type '{spec.entityType}'");
                    breadcrumbItems.Add(new BreadcrumbItem
                    {
                        Label = $"{spec.entityType} #{spec.entityId}",
                        Url = spec.url,
                        IsActive = false
                    });
                    continue;
                }

                // Use the builder to create the breadcrumb item
                var item = await builder.BuildBreadcrumbAsync(spec.entityId.Value, spec.url);
                breadcrumbItems.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error building breadcrumb for {spec.entityType} ID {spec.entityId}");
                breadcrumbItems.Add(new BreadcrumbItem
                {
                    Label = spec.entityId.HasValue ? $"{spec.entityType} #{spec.entityId}" : spec.entityType,
                    Url = spec.url,
                    IsActive = false
                });
            }
        }

        // Mark the last item as active
        if (breadcrumbItems.Any())
        {
            breadcrumbItems.Last().IsActive = true;
            breadcrumbItems.Last().Url = null; // Active items shouldn't be clickable
        }

        return breadcrumbItems.ToArray();
    }

    /// <summary>
    /// Creates a simple two-level breadcrumb (List > Item)
    /// </summary>
    public async Task<BreadcrumbItem[]> BuildSimpleBreadcrumbAsync(
        string listLabel,
        string listUrl,
        string entityType,
        int? entityId = null,
        string? customLabel = null)
    {
        return await BuildBreadcrumbChainAsync(
            (listLabel, null, listUrl, null),
            (entityType, entityId, null, customLabel)
        );
    }
}
