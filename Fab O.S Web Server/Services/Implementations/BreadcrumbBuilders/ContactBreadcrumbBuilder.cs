using Microsoft.EntityFrameworkCore;
using static FabOS.WebServer.Components.Shared.Breadcrumb;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.BreadcrumbBuilders;

/// <summary>
/// Builds breadcrumb items for CustomerContact entities
/// </summary>
public class ContactBreadcrumbBuilder : IBreadcrumbBuilder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ContactBreadcrumbBuilder> _logger;

    public string EntityType => "Contact";

    public ContactBreadcrumbBuilder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<ContactBreadcrumbBuilder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<BreadcrumbItem> BuildBreadcrumbAsync(int entityId, string? url = null)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync();
            var contact = await dbContext.CustomerContacts
                .AsNoTracking()
                .Where(c => c.Id == entityId)
                .Select(c => new { c.Id, c.FirstName, c.LastName, c.ContactNumber })
                .FirstOrDefaultAsync();

            if (contact == null)
            {
                _logger.LogWarning($"Contact with ID {entityId} not found for breadcrumb");
                return new BreadcrumbItem
                {
                    Label = $"Contact #{entityId}",
                    Url = url ?? $"/contacts/{entityId}",
                    IsActive = false
                };
            }

            // Build display name: "FirstName LastName" or fallback to contact number
            var displayName = !string.IsNullOrEmpty(contact.FirstName) || !string.IsNullOrEmpty(contact.LastName)
                ? $"{contact.FirstName} {contact.LastName}".Trim()
                : !string.IsNullOrEmpty(contact.ContactNumber)
                    ? contact.ContactNumber
                    : $"Contact #{entityId}";

            return new BreadcrumbItem
            {
                Label = displayName,
                Url = url ?? $"/contacts/{entityId}",
                IsActive = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error building breadcrumb for Contact ID {entityId}");
            return new BreadcrumbItem
            {
                Label = $"Contact #{entityId}",
                Url = url ?? $"/contacts/{entityId}",
                IsActive = false
            };
        }
    }

    public bool CanHandle(string entityType)
    {
        return entityType.Equals(EntityType, StringComparison.OrdinalIgnoreCase);
    }
}
