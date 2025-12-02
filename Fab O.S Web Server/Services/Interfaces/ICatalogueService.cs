using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for managing catalogues and catalogue items
/// Supports multi-tenant catalogues with system (Database Catalogue) and custom catalogues
/// </summary>
public interface ICatalogueService
{
    // ====================
    // CATALOGUE MANAGEMENT
    // ====================

    /// <summary>
    /// Get all catalogues for a company
    /// </summary>
    Task<List<Catalogue>> GetCataloguesAsync(int companyId);

    /// <summary>
    /// Get a specific catalogue by ID
    /// </summary>
    Task<Catalogue?> GetCatalogueByIdAsync(int catalogueId, int companyId);

    /// <summary>
    /// Get the system (Database) catalogue for a company
    /// </summary>
    Task<Catalogue?> GetSystemCatalogueAsync(int companyId);

    /// <summary>
    /// Create a new custom catalogue
    /// </summary>
    Task<Catalogue> CreateCatalogueAsync(string name, string? description, int companyId, int userId);

    /// <summary>
    /// Update an existing catalogue (only if it's not a system catalogue)
    /// </summary>
    Task<Catalogue?> UpdateCatalogueAsync(int catalogueId, string name, string? description, int companyId, int userId);

    /// <summary>
    /// Delete a catalogue (only if it's not a system catalogue and has no items)
    /// </summary>
    Task<bool> DeleteCatalogueAsync(int catalogueId, int companyId, int userId);

    /// <summary>
    /// Duplicate a catalogue (creates a custom copy with all items)
    /// </summary>
    Task<Catalogue> DuplicateCatalogueAsync(int sourceCatalogueId, string newName, int companyId, int userId);

    /// <summary>
    /// Get the number of items in a catalogue
    /// </summary>
    Task<int> GetCatalogueItemCountAsync(int catalogueId, int companyId);

    /// <summary>
    /// Check if a catalogue can be modified (returns false for system catalogues)
    /// </summary>
    Task<bool> CanModifyCatalogueAsync(int catalogueId, int companyId);

    // ==========================
    // CATALOGUE ITEM MANAGEMENT
    // ==========================

    /// <summary>
    /// Get all items in a specific catalogue
    /// </summary>
    Task<List<CatalogueItem>> GetItemsByCatalogueAsync(int catalogueId, int companyId);

    /// <summary>
    /// Get items in a catalogue filtered by category
    /// </summary>
    Task<List<CatalogueItem>> GetItemsByCatalogueAndCategoryAsync(int catalogueId, string category, int companyId);

    /// <summary>
    /// Get a specific catalogue item by ID
    /// </summary>
    Task<CatalogueItem?> GetCatalogueItemByIdAsync(int itemId, int companyId);

    /// <summary>
    /// Create a new catalogue item (only in custom catalogues)
    /// </summary>
    Task<CatalogueItem> CreateCatalogueItemAsync(int catalogueId, CatalogueItem item, int companyId);

    /// <summary>
    /// Update an existing catalogue item (only in custom catalogues)
    /// </summary>
    Task<CatalogueItem?> UpdateCatalogueItemAsync(int itemId, CatalogueItem item, int companyId);

    /// <summary>
    /// Delete a catalogue item (only in custom catalogues)
    /// </summary>
    Task<bool> DeleteCatalogueItemAsync(int itemId, int companyId);

    /// <summary>
    /// Search for catalogue items across all catalogues for a company
    /// </summary>
    Task<List<CatalogueItem>> SearchCatalogueItemsAsync(string searchTerm, int companyId, int? catalogueId = null, int maxResults = 50);

    /// <summary>
    /// Get all unique categories across all catalogues for a company
    /// </summary>
    Task<List<string>> GetAllCategoriesAsync(int companyId, int? catalogueId = null);

    /// <summary>
    /// Get all unique materials across all catalogues for a company
    /// </summary>
    Task<List<string>> GetAllMaterialsAsync(int companyId, int? catalogueId = null);

    /// <summary>
    /// Check if a catalogue item can be modified (returns false for items in system catalogues)
    /// </summary>
    Task<bool> CanModifyItemAsync(int itemId, int companyId);

    // ================
    // IMPORT / EXPORT
    // ================

    /// <summary>
    /// Import catalogue items from Excel file (only to custom catalogues)
    /// </summary>
    Task<int> ImportItemsFromExcelAsync(int catalogueId, Stream fileStream, int companyId, int userId);

    /// <summary>
    /// Export catalogue items to Excel
    /// </summary>
    Task<byte[]> ExportCatalogueToExcelAsync(int catalogueId, int companyId);
}
