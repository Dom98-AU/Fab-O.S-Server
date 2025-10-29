using Microsoft.EntityFrameworkCore;
using static FabOS.WebServer.Components.Shared.Breadcrumb;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.BreadcrumbBuilders;

/// <summary>
/// Builds breadcrumb items for Takeoff entities (Takeoff)
/// </summary>
public class TakeoffBreadcrumbBuilder : IBreadcrumbBuilder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<TakeoffBreadcrumbBuilder> _logger;

    public string EntityType => "Takeoff";

    public TakeoffBreadcrumbBuilder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<TakeoffBreadcrumbBuilder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<BreadcrumbItem> BuildBreadcrumbAsync(int entityId, string? url = null)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();
            var takeoff = await dbContext.TraceDrawings
                .AsNoTracking()
                .Where(t => t.Id == entityId)
                .Select(t => new { t.Id, t.TakeoffNumber })
                .FirstOrDefaultAsync();

            if (takeoff == null)
            {
                _logger.LogWarning($"Takeoff with ID {entityId} not found for breadcrumb");
                return new BreadcrumbItem
                {
                    Label = $"Takeoff #{entityId}",
                    Url = url ?? $"/takeoffs/{entityId}",
                    IsActive = false
                };
            }

            var displayName = !string.IsNullOrEmpty(takeoff.TakeoffNumber)
                ? takeoff.TakeoffNumber
                : $"Takeoff #{entityId}";

            return new BreadcrumbItem
            {
                Label = displayName,
                Url = url ?? $"/takeoffs/{entityId}",
                IsActive = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error building breadcrumb for Takeoff ID {entityId}");
            return new BreadcrumbItem
            {
                Label = $"Takeoff #{entityId}",
                Url = url ?? $"/takeoffs/{entityId}",
                IsActive = false
            };
        }
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals(EntityType, StringComparison.OrdinalIgnoreCase);
    }
}
