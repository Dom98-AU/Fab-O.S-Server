using static FabOS.WebServer.Components.Shared.Breadcrumb;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Interface for building breadcrumb items from entity IDs
/// </summary>
public interface IBreadcrumbBuilder
{
    /// <summary>
    /// Gets the entity type this builder handles (e.g., "Takeoff", "Package", "Customer")
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Builds a breadcrumb item by loading the entity from the database
    /// </summary>
    /// <param name="entityId">The ID of the entity to load</param>
    /// <param name="url">Optional URL to use for the breadcrumb item. If null, will auto-generate</param>
    /// <returns>A breadcrumb item with the entity's display name</returns>
    Task<BreadcrumbItem> BuildBreadcrumbAsync(int entityId, string? url = null);

    /// <summary>
    /// Checks if this builder can handle the specified entity type
    /// </summary>
    /// <param name="entityType">The entity type to check</param>
    /// <returns>True if this builder handles the entity type</returns>
    bool CanHandle(string entityType);
}
