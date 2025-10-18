using Microsoft.EntityFrameworkCore;
using static FabOS.WebServer.Components.Shared.Breadcrumb;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.BreadcrumbBuilders;

/// <summary>
/// Builds breadcrumb items for Customer entities
/// </summary>
public class CustomerBreadcrumbBuilder : IBreadcrumbBuilder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CustomerBreadcrumbBuilder> _logger;

    public string EntityType => "Customer";

    public CustomerBreadcrumbBuilder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CustomerBreadcrumbBuilder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<BreadcrumbItem> BuildBreadcrumbAsync(int entityId, string? url = null)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();
            var customer = await dbContext.Customers
                .AsNoTracking()
                .Where(c => c.Id == entityId)
                .Select(c => new { c.Id, c.Code, c.Name })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                _logger.LogWarning($"Customer with ID {entityId} not found for breadcrumb");
                return new BreadcrumbItem
                {
                    Label = $"Customer #{entityId}",
                    Url = url ?? $"/customers/{entityId}",
                    IsActive = false
                };
            }

            // Prefer customer name for display, fallback to code if name is empty
            var displayName = !string.IsNullOrEmpty(customer.Name)
                ? customer.Name
                : !string.IsNullOrEmpty(customer.Code)
                    ? customer.Code
                    : $"Customer #{entityId}";

            return new BreadcrumbItem
            {
                Label = displayName,
                Url = url ?? $"/customers/{entityId}",
                IsActive = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error building breadcrumb for Customer ID {entityId}");
            return new BreadcrumbItem
            {
                Label = $"Customer #{entityId}",
                Url = url ?? $"/customers/{entityId}",
                IsActive = false
            };
        }
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals(EntityType, StringComparison.OrdinalIgnoreCase);
    }
}
