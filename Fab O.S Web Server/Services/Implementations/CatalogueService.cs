using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations;

/// <summary>
/// Service implementation for managing catalogues and catalogue items
/// Supports multi-tenant catalogues with system (Database Catalogue) and custom catalogues
/// </summary>
public class CatalogueService : ICatalogueService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<CatalogueService> _logger;

    public CatalogueService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<CatalogueService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // ====================
    // CATALOGUE MANAGEMENT
    // ====================

    public async Task<List<Catalogue>> GetCataloguesAsync(int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Catalogues
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .OrderByDescending(c => c.IsSystemCatalogue) // System catalogue first
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Catalogue?> GetCatalogueByIdAsync(int catalogueId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Catalogues
            .Include(c => c.Company)
            .Include(c => c.Creator)
            .Include(c => c.Modifier)
            .FirstOrDefaultAsync(c => c.Id == catalogueId && c.CompanyId == companyId);
    }

    public async Task<Catalogue?> GetSystemCatalogueAsync(int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Catalogues
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.IsSystemCatalogue && c.IsActive);
    }

    public async Task<Catalogue> CreateCatalogueAsync(string name, string? description, int companyId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Validate name is unique for this company
        var nameExists = await context.Catalogues
            .AnyAsync(c => c.CompanyId == companyId && c.Name == name && c.IsActive);

        if (nameExists)
        {
            throw new InvalidOperationException($"A catalogue with the name '{name}' already exists for this company.");
        }

        var catalogue = new Catalogue
        {
            Name = name,
            Description = description,
            IsSystemCatalogue = false, // User-created catalogues are never system catalogues
            CompanyId = companyId,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userId
        };

        context.Catalogues.Add(catalogue);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created custom catalogue {CatalogueId} '{Name}' for company {CompanyId} by user {UserId}",
            catalogue.Id, catalogue.Name, companyId, userId);

        return catalogue;
    }

    public async Task<Catalogue?> UpdateCatalogueAsync(int catalogueId, string name, string? description, int companyId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var catalogue = await context.Catalogues
            .FirstOrDefaultAsync(c => c.Id == catalogueId && c.CompanyId == companyId);

        if (catalogue == null)
        {
            return null;
        }

        // Cannot modify system catalogues
        if (catalogue.IsSystemCatalogue)
        {
            throw new InvalidOperationException("Cannot modify the system Database Catalogue.");
        }

        // Check if new name conflicts with another catalogue
        var nameConflict = await context.Catalogues
            .AnyAsync(c => c.CompanyId == companyId && c.Name == name && c.Id != catalogueId && c.IsActive);

        if (nameConflict)
        {
            throw new InvalidOperationException($"A catalogue with the name '{name}' already exists for this company.");
        }

        catalogue.Name = name;
        catalogue.Description = description;
        catalogue.ModifiedDate = DateTime.UtcNow;
        catalogue.ModifiedBy = userId;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated catalogue {CatalogueId} for company {CompanyId} by user {UserId}",
            catalogueId, companyId, userId);

        return catalogue;
    }

    public async Task<bool> DeleteCatalogueAsync(int catalogueId, int companyId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var catalogue = await context.Catalogues
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == catalogueId && c.CompanyId == companyId);

        if (catalogue == null)
        {
            return false;
        }

        // Cannot delete system catalogues
        if (catalogue.IsSystemCatalogue)
        {
            throw new InvalidOperationException("Cannot delete the system Database Catalogue.");
        }

        // Cannot delete catalogues with items (safety check)
        if (catalogue.Items.Any())
        {
            throw new InvalidOperationException($"Cannot delete catalogue '{catalogue.Name}' because it contains {catalogue.Items.Count} items. Delete all items first.");
        }

        // Soft delete
        catalogue.IsActive = false;
        catalogue.ModifiedDate = DateTime.UtcNow;
        catalogue.ModifiedBy = userId;

        await context.SaveChangesAsync();

        _logger.LogInformation("Deleted catalogue {CatalogueId} '{Name}' for company {CompanyId} by user {UserId}",
            catalogueId, catalogue.Name, companyId, userId);

        return true;
    }

    public async Task<Catalogue> DuplicateCatalogueAsync(int sourceCatalogueId, string newName, int companyId, int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var sourceCatalogue = await context.Catalogues
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == sourceCatalogueId && c.CompanyId == companyId);

        if (sourceCatalogue == null)
        {
            throw new InvalidOperationException("Source catalogue not found.");
        }

        // Create new catalogue
        var newCatalogue = await CreateCatalogueAsync(newName, $"Copy of {sourceCatalogue.Name}", companyId, userId);

        // Copy all items
        foreach (var sourceItem in sourceCatalogue.Items)
        {
            var newItem = new CatalogueItem
            {
                CatalogueId = newCatalogue.Id,
                CompanyId = companyId,
                ItemCode = sourceItem.ItemCode,
                Description = sourceItem.Description,
                Category = sourceItem.Category,
                Material = sourceItem.Material,
                Profile = sourceItem.Profile,
                Length_mm = sourceItem.Length_mm,
                Width_mm = sourceItem.Width_mm,
                Height_mm = sourceItem.Height_mm,
                Depth_mm = sourceItem.Depth_mm,
                Thickness_mm = sourceItem.Thickness_mm,
                Diameter_mm = sourceItem.Diameter_mm,
                OD_mm = sourceItem.OD_mm,
                ID_mm = sourceItem.ID_mm,
                WallThickness_mm = sourceItem.WallThickness_mm,
                Mass_kg_m = sourceItem.Mass_kg_m,
                Mass_kg_m2 = sourceItem.Mass_kg_m2,
                Weight_kg = sourceItem.Weight_kg,
                SurfaceArea_m2 = sourceItem.SurfaceArea_m2,
                Standard = sourceItem.Standard,
                Grade = sourceItem.Grade,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            context.CatalogueItems.Add(newItem);
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Duplicated catalogue {SourceId} to new catalogue {NewId} with {ItemCount} items for company {CompanyId}",
            sourceCatalogueId, newCatalogue.Id, sourceCatalogue.Items.Count, companyId);

        return newCatalogue;
    }

    public async Task<int> GetCatalogueItemCountAsync(int catalogueId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.CatalogueItems
            .Where(i => i.CatalogueId == catalogueId && i.CompanyId == companyId && i.IsActive)
            .CountAsync();
    }

    public async Task<bool> CanModifyCatalogueAsync(int catalogueId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var catalogue = await context.Catalogues
            .FirstOrDefaultAsync(c => c.Id == catalogueId && c.CompanyId == companyId);

        return catalogue != null && !catalogue.IsSystemCatalogue;
    }

    // ==========================
    // CATALOGUE ITEM MANAGEMENT
    // ==========================

    public async Task<List<CatalogueItem>> GetItemsByCatalogueAsync(int catalogueId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.CatalogueItems
            .Where(i => i.CatalogueId == catalogueId && i.CompanyId == companyId && i.IsActive)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.ItemCode)
            .ToListAsync();
    }

    public async Task<List<CatalogueItem>> GetItemsByCatalogueAndCategoryAsync(int catalogueId, string category, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.CatalogueItems
            .Where(i => i.CatalogueId == catalogueId && i.Category == category && i.CompanyId == companyId && i.IsActive)
            .OrderBy(i => i.ItemCode)
            .ToListAsync();
    }

    public async Task<CatalogueItem?> GetCatalogueItemByIdAsync(int itemId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.CatalogueItems
            .Include(i => i.Catalogue)
            .Include(i => i.Company)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId);
    }

    public async Task<CatalogueItem> CreateCatalogueItemAsync(int catalogueId, CatalogueItem item, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Verify catalogue exists and is not a system catalogue
        var catalogue = await context.Catalogues
            .FirstOrDefaultAsync(c => c.Id == catalogueId && c.CompanyId == companyId);

        if (catalogue == null)
        {
            throw new InvalidOperationException("Catalogue not found.");
        }

        if (catalogue.IsSystemCatalogue)
        {
            throw new InvalidOperationException("Cannot add items to the system Database Catalogue.");
        }

        // Validate ItemCode is unique within the catalogue
        var codeExists = await context.CatalogueItems
            .AnyAsync(i => i.CatalogueId == catalogueId && i.ItemCode == item.ItemCode && i.IsActive);

        if (codeExists)
        {
            throw new InvalidOperationException($"Item code '{item.ItemCode}' already exists in this catalogue.");
        }

        item.CatalogueId = catalogueId;
        item.CompanyId = companyId;
        item.IsActive = true;
        item.CreatedDate = DateTime.UtcNow;

        context.CatalogueItems.Add(item);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created catalogue item {ItemId} '{ItemCode}' in catalogue {CatalogueId} for company {CompanyId}",
            item.Id, item.ItemCode, catalogueId, companyId);

        return item;
    }

    public async Task<CatalogueItem?> UpdateCatalogueItemAsync(int itemId, CatalogueItem updatedItem, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.CatalogueItems
            .Include(i => i.Catalogue)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId);

        if (item == null)
        {
            return null;
        }

        // Cannot modify items in system catalogues
        if (item.Catalogue.IsSystemCatalogue)
        {
            throw new InvalidOperationException("Cannot modify items in the system Database Catalogue.");
        }

        // Update all properties
        item.ItemCode = updatedItem.ItemCode;
        item.Description = updatedItem.Description;
        item.Category = updatedItem.Category;
        item.Material = updatedItem.Material;
        item.Profile = updatedItem.Profile;
        item.Length_mm = updatedItem.Length_mm;
        item.Width_mm = updatedItem.Width_mm;
        item.Height_mm = updatedItem.Height_mm;
        item.Thickness_mm = updatedItem.Thickness_mm;
        item.Mass_kg_m = updatedItem.Mass_kg_m;
        item.Weight_kg = updatedItem.Weight_kg;
        item.Standard = updatedItem.Standard;
        item.Grade = updatedItem.Grade;
        item.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Updated catalogue item {ItemId} in catalogue {CatalogueId} for company {CompanyId}",
            itemId, item.CatalogueId, companyId);

        return item;
    }

    public async Task<bool> DeleteCatalogueItemAsync(int itemId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.CatalogueItems
            .Include(i => i.Catalogue)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId);

        if (item == null)
        {
            return false;
        }

        // Cannot delete items from system catalogues
        if (item.Catalogue.IsSystemCatalogue)
        {
            throw new InvalidOperationException("Cannot delete items from the system Database Catalogue.");
        }

        // Soft delete
        item.IsActive = false;
        item.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _logger.LogInformation("Deleted catalogue item {ItemId} from catalogue {CatalogueId} for company {CompanyId}",
            itemId, item.CatalogueId, companyId);

        return true;
    }

    public async Task<List<CatalogueItem>> SearchCatalogueItemsAsync(string searchTerm, int companyId, int? catalogueId = null, int maxResults = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.CatalogueItems
            .Where(i => i.CompanyId == companyId && i.IsActive);

        // Filter by catalogue if specified
        if (catalogueId.HasValue)
        {
            query = query.Where(i => i.CatalogueId == catalogueId.Value);
        }

        // Search across multiple fields
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(i =>
                i.ItemCode.ToLower().Contains(term) ||
                i.Description.ToLower().Contains(term) ||
                i.Material.ToLower().Contains(term) ||
                (i.Profile != null && i.Profile.ToLower().Contains(term)) ||
                i.Category.ToLower().Contains(term)
            );
        }

        return await query
            .OrderBy(i => i.ItemCode)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<List<string>> GetAllCategoriesAsync(int companyId, int? catalogueId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.CatalogueItems
            .Where(i => i.CompanyId == companyId && i.IsActive);

        if (catalogueId.HasValue)
        {
            query = query.Where(i => i.CatalogueId == catalogueId.Value);
        }

        return await query
            .Select(i => i.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetAllMaterialsAsync(int companyId, int? catalogueId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.CatalogueItems
            .Where(i => i.CompanyId == companyId && i.IsActive);

        if (catalogueId.HasValue)
        {
            query = query.Where(i => i.CatalogueId == catalogueId.Value);
        }

        return await query
            .Select(i => i.Material)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    public async Task<bool> CanModifyItemAsync(int itemId, int companyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.CatalogueItems
            .Include(i => i.Catalogue)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId);

        return item != null && !item.Catalogue.IsSystemCatalogue;
    }

    // ================
    // IMPORT / EXPORT
    // ================

    public async Task<int> ImportItemsFromExcelAsync(int catalogueId, Stream fileStream, int companyId, int userId)
    {
        // TODO: Implement Excel import using ExcelImportService
        // This will be implemented in a future update
        throw new NotImplementedException("Excel import will be implemented in a future update.");
    }

    public async Task<byte[]> ExportCatalogueToExcelAsync(int catalogueId, int companyId)
    {
        // TODO: Implement Excel export
        // This will be implemented in a future update
        throw new NotImplementedException("Excel export will be implemented in a future update.");
    }
}
