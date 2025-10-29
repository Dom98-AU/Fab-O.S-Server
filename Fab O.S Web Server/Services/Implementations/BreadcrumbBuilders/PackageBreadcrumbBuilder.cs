using Microsoft.EntityFrameworkCore;
using static FabOS.WebServer.Components.Shared.Breadcrumb;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.BreadcrumbBuilders;

/// <summary>
/// Builds breadcrumb items for Package entities
/// </summary>
public class PackageBreadcrumbBuilder : IBreadcrumbBuilder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<PackageBreadcrumbBuilder> _logger;
    private readonly ITenantService _tenantService;

    public string EntityType => "Package";

    public PackageBreadcrumbBuilder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<PackageBreadcrumbBuilder> logger,
        ITenantService tenantService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _tenantService = tenantService;
    }

    public async Task<BreadcrumbItem> BuildBreadcrumbAsync(int entityId, string? url = null)
    {
        // Get tenant slug for URL generation
        var tenantSlug = await _tenantService.GetCurrentTenantSlugAsync() ?? "default";

        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();
            var package = await dbContext.Packages
                .AsNoTracking()
                .Where(p => p.Id == entityId)
                .Select(p => new { p.Id, p.PackageNumber, p.PackageName })
                .FirstOrDefaultAsync();

            if (package == null)
            {
                _logger.LogWarning($"Package with ID {entityId} not found for breadcrumb");
                return new BreadcrumbItem
                {
                    Label = $"Package #{entityId}",
                    Url = url ?? $"/{tenantSlug}/trace/packages/{entityId}",
                    IsActive = false
                };
            }

            var displayName = !string.IsNullOrEmpty(package.PackageNumber)
                ? package.PackageNumber
                : !string.IsNullOrEmpty(package.PackageName)
                    ? package.PackageName
                    : $"Package #{entityId}";

            return new BreadcrumbItem
            {
                Label = displayName,
                Url = url ?? $"/{tenantSlug}/trace/packages/{entityId}",
                IsActive = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error building breadcrumb for Package ID {entityId}");
            return new BreadcrumbItem
            {
                Label = $"Package #{entityId}",
                Url = url ?? $"/{tenantSlug}/trace/packages/{entityId}",
                IsActive = false
            };
        }
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals(EntityType, StringComparison.OrdinalIgnoreCase);
    }
}
