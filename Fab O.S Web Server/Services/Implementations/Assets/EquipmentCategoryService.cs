using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Equipment Category and Type management
/// </summary>
public class EquipmentCategoryService : IEquipmentCategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EquipmentCategoryService> _logger;

    public EquipmentCategoryService(ApplicationDbContext context, ILogger<EquipmentCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Category CRUD Operations

    public async Task<EquipmentCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.EquipmentCategories
            .Include(c => c.EquipmentTypes)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<EquipmentCategory?> GetCategoryByNameAsync(string name, int companyId)
    {
        return await _context.EquipmentCategories
            .Include(c => c.EquipmentTypes)
            .FirstOrDefaultAsync(c => c.Name == name && c.CompanyId == companyId);
    }

    public async Task<IEnumerable<EquipmentCategory>> GetAllCategoriesAsync(int companyId, bool includeInactive = false)
    {
        var query = _context.EquipmentCategories
            .Where(c => c.CompanyId == companyId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCategory>> GetCategoriesWithTypesAsync(int companyId, bool includeInactive = false)
    {
        var query = _context.EquipmentCategories
            .Include(c => c.EquipmentTypes.Where(t => includeInactive || t.IsActive))
            .Where(c => c.CompanyId == companyId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<EquipmentCategory> CreateCategoryAsync(EquipmentCategory category, int companyId)
    {
        category.CompanyId = companyId;
        category.CreatedDate = DateTime.UtcNow;

        // Get max display order for company
        var maxOrder = await _context.EquipmentCategories
            .Where(c => c.CompanyId == companyId)
            .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
        category.DisplayOrder = maxOrder + 1;

        _context.EquipmentCategories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created equipment category {Name} for company {CompanyId}", category.Name, companyId);
        return category;
    }

    public async Task<EquipmentCategory> UpdateCategoryAsync(EquipmentCategory category)
    {
        category.LastModified = DateTime.UtcNow;
        _context.EquipmentCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _context.EquipmentCategories
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            return false;

        if (category.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be deleted");

        if (category.Equipment.Any())
            throw new InvalidOperationException("Cannot delete category with associated equipment");

        _context.EquipmentCategories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CategoryExistsAsync(int id)
    {
        return await _context.EquipmentCategories.AnyAsync(c => c.Id == id);
    }

    public async Task<bool> CategoryNameExistsAsync(string name, int companyId, int? excludeId = null)
    {
        var query = _context.EquipmentCategories
            .Where(c => c.Name == name && c.CompanyId == companyId);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    #endregion

    #region Category Status Operations

    public async Task<EquipmentCategory> ActivateCategoryAsync(int id)
    {
        var category = await GetCategoryByIdAsync(id);
        if (category == null)
            throw new InvalidOperationException($"Category with ID {id} not found");

        category.IsActive = true;
        category.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<EquipmentCategory> DeactivateCategoryAsync(int id)
    {
        var category = await GetCategoryByIdAsync(id);
        if (category == null)
            throw new InvalidOperationException($"Category with ID {id} not found");

        if (category.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be deactivated");

        category.IsActive = false;
        category.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<IEnumerable<EquipmentCategory>> GetActiveCategoriesAsync(int companyId)
    {
        return await GetAllCategoriesAsync(companyId, includeInactive: false);
    }

    public async Task<IEnumerable<EquipmentCategory>> GetSystemCategoriesAsync(int companyId)
    {
        return await _context.EquipmentCategories
            .Where(c => c.CompanyId == companyId && c.IsSystemCategory)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentCategory>> GetCustomCategoriesAsync(int companyId)
    {
        return await _context.EquipmentCategories
            .Where(c => c.CompanyId == companyId && !c.IsSystemCategory)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    #endregion

    #region Category Statistics

    public async Task<int> GetEquipmentCountForCategoryAsync(int categoryId)
    {
        return await _context.Equipment
            .CountAsync(e => e.CategoryId == categoryId && !e.IsDeleted);
    }

    public async Task<int> GetTypeCountForCategoryAsync(int categoryId)
    {
        return await _context.EquipmentTypes
            .CountAsync(t => t.CategoryId == categoryId);
    }

    public async Task<Dictionary<int, int>> GetEquipmentCountsByCategoryAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
    }

    #endregion

    #region Type CRUD Operations

    public async Task<EquipmentType?> GetTypeByIdAsync(int id)
    {
        return await _context.EquipmentTypes
            .Include(t => t.EquipmentCategory)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<EquipmentType?> GetTypeByNameAsync(string name, int categoryId)
    {
        return await _context.EquipmentTypes
            .Include(t => t.EquipmentCategory)
            .FirstOrDefaultAsync(t => t.Name == name && t.CategoryId == categoryId);
    }

    public async Task<IEnumerable<EquipmentType>> GetAllTypesAsync(int companyId, bool includeInactive = false)
    {
        var query = _context.EquipmentTypes
            .Include(t => t.EquipmentCategory)
            .Where(t => t.EquipmentCategory!.CompanyId == companyId);

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        return await query
            .OrderBy(t => t.EquipmentCategory!.Name)
            .ThenBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentType>> GetTypesByCategoryAsync(int categoryId, bool includeInactive = false)
    {
        var query = _context.EquipmentTypes
            .Include(t => t.EquipmentCategory)
            .Where(t => t.CategoryId == categoryId);

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        return await query
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<EquipmentType> CreateTypeAsync(EquipmentType type)
    {
        type.CreatedDate = DateTime.UtcNow;

        // Get max display order for category
        var maxOrder = await _context.EquipmentTypes
            .Where(t => t.CategoryId == type.CategoryId)
            .MaxAsync(t => (int?)t.DisplayOrder) ?? 0;
        type.DisplayOrder = maxOrder + 1;

        _context.EquipmentTypes.Add(type);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created equipment type {Name} for category {CategoryId}", type.Name, type.CategoryId);
        return type;
    }

    public async Task<EquipmentType> UpdateTypeAsync(EquipmentType type)
    {
        type.LastModified = DateTime.UtcNow;
        _context.EquipmentTypes.Update(type);
        await _context.SaveChangesAsync();
        return type;
    }

    public async Task<bool> DeleteTypeAsync(int id)
    {
        var type = await _context.EquipmentTypes
            .Include(t => t.Equipment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (type == null)
            return false;

        if (type.IsSystemType)
            throw new InvalidOperationException("System types cannot be deleted");

        if (type.Equipment.Any())
            throw new InvalidOperationException("Cannot delete type with associated equipment");

        _context.EquipmentTypes.Remove(type);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TypeExistsAsync(int id)
    {
        return await _context.EquipmentTypes.AnyAsync(t => t.Id == id);
    }

    public async Task<bool> TypeNameExistsAsync(string name, int categoryId, int? excludeId = null)
    {
        var query = _context.EquipmentTypes
            .Where(t => t.Name == name && t.CategoryId == categoryId);

        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    #endregion

    #region Type Status Operations

    public async Task<EquipmentType> ActivateTypeAsync(int id)
    {
        var type = await GetTypeByIdAsync(id);
        if (type == null)
            throw new InvalidOperationException($"Type with ID {id} not found");

        type.IsActive = true;
        type.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return type;
    }

    public async Task<EquipmentType> DeactivateTypeAsync(int id)
    {
        var type = await GetTypeByIdAsync(id);
        if (type == null)
            throw new InvalidOperationException($"Type with ID {id} not found");

        if (type.IsSystemType)
            throw new InvalidOperationException("System types cannot be deactivated");

        type.IsActive = false;
        type.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return type;
    }

    public async Task<IEnumerable<EquipmentType>> GetActiveTypesAsync(int companyId)
    {
        return await GetAllTypesAsync(companyId, includeInactive: false);
    }

    public async Task<IEnumerable<EquipmentType>> GetActiveTypesByCategoryAsync(int categoryId)
    {
        return await GetTypesByCategoryAsync(categoryId, includeInactive: false);
    }

    public async Task<IEnumerable<EquipmentType>> GetSystemTypesAsync(int companyId)
    {
        return await _context.EquipmentTypes
            .Include(t => t.EquipmentCategory)
            .Where(t => t.EquipmentCategory!.CompanyId == companyId && t.IsSystemType)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentType>> GetCustomTypesAsync(int companyId)
    {
        return await _context.EquipmentTypes
            .Include(t => t.EquipmentCategory)
            .Where(t => t.EquipmentCategory!.CompanyId == companyId && !t.IsSystemType)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();
    }

    #endregion

    #region Type Statistics

    public async Task<int> GetEquipmentCountForTypeAsync(int typeId)
    {
        return await _context.Equipment
            .CountAsync(e => e.TypeId == typeId && !e.IsDeleted);
    }

    public async Task<Dictionary<int, int>> GetEquipmentCountsByTypeAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .GroupBy(e => e.TypeId)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Count);
    }

    #endregion

    #region Display Order Operations

    public async Task UpdateCategoryDisplayOrderAsync(int categoryId, int newOrder)
    {
        var category = await GetCategoryByIdAsync(categoryId);
        if (category == null)
            throw new InvalidOperationException($"Category with ID {categoryId} not found");

        category.DisplayOrder = newOrder;
        category.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTypeDisplayOrderAsync(int typeId, int newOrder)
    {
        var type = await GetTypeByIdAsync(typeId);
        if (type == null)
            throw new InvalidOperationException($"Type with ID {typeId} not found");

        type.DisplayOrder = newOrder;
        type.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task ReorderCategoriesAsync(int companyId, IEnumerable<(int Id, int NewOrder)> newOrders)
    {
        foreach (var (id, newOrder) in newOrders)
        {
            var category = await _context.EquipmentCategories.FindAsync(id);
            if (category != null && category.CompanyId == companyId)
            {
                category.DisplayOrder = newOrder;
                category.LastModified = DateTime.UtcNow;
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task ReorderTypesAsync(int categoryId, IEnumerable<(int Id, int NewOrder)> newOrders)
    {
        foreach (var (id, newOrder) in newOrders)
        {
            var type = await _context.EquipmentTypes.FindAsync(id);
            if (type != null && type.CategoryId == categoryId)
            {
                type.DisplayOrder = newOrder;
                type.LastModified = DateTime.UtcNow;
            }
        }
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Seed Data

    public async Task SeedDefaultCategoriesAsync(int companyId)
    {
        if (await HasDefaultCategoriesAsync(companyId))
            return;

        var categories = new List<EquipmentCategory>
        {
            new() { CompanyId = companyId, Name = "Fabrication Equipment", Description = "CNC machines, welders, plasma cutters, press brakes", IconClass = "fas fa-industry", DisplayOrder = 1, IsSystemCategory = true },
            new() { CompanyId = companyId, Name = "Workshop Tools", Description = "Power tools, hand tools, measuring equipment", IconClass = "fas fa-tools", DisplayOrder = 2, IsSystemCategory = true },
            new() { CompanyId = companyId, Name = "Lifting Equipment", Description = "Cranes, hoists, forklifts, lifting gear", IconClass = "fas fa-arrow-up", DisplayOrder = 3, IsSystemCategory = true },
            new() { CompanyId = companyId, Name = "Vehicles", Description = "Trucks, vans, trailers, site vehicles", IconClass = "fas fa-truck", DisplayOrder = 4, IsSystemCategory = true },
            new() { CompanyId = companyId, Name = "Safety Equipment", Description = "PPE, fire extinguishers, first aid", IconClass = "fas fa-hard-hat", DisplayOrder = 5, IsSystemCategory = true },
            new() { CompanyId = companyId, Name = "IT Equipment", Description = "Computers, servers, networking equipment", IconClass = "fas fa-laptop", DisplayOrder = 6, IsSystemCategory = true }
        };

        foreach (var category in categories)
        {
            category.CreatedDate = DateTime.UtcNow;
        }

        _context.EquipmentCategories.AddRange(categories);
        await _context.SaveChangesAsync();

        // Seed types for each category
        var fabricationTypes = new List<EquipmentType>
        {
            new() { CategoryId = categories[0].Id, Name = "CNC Machine", DefaultMaintenanceIntervalDays = 90, IsSystemType = true, DisplayOrder = 1 },
            new() { CategoryId = categories[0].Id, Name = "MIG/TIG Welder", DefaultMaintenanceIntervalDays = 30, IsSystemType = true, DisplayOrder = 2 },
            new() { CategoryId = categories[0].Id, Name = "Plasma Cutter", DefaultMaintenanceIntervalDays = 60, IsSystemType = true, DisplayOrder = 3 },
            new() { CategoryId = categories[0].Id, Name = "Press Brake", DefaultMaintenanceIntervalDays = 90, IsSystemType = true, DisplayOrder = 4 },
            new() { CategoryId = categories[0].Id, Name = "Laser Cutter", DefaultMaintenanceIntervalDays = 30, IsSystemType = true, DisplayOrder = 5 }
        };

        var workshopTypes = new List<EquipmentType>
        {
            new() { CategoryId = categories[1].Id, Name = "Angle Grinder", DefaultMaintenanceIntervalDays = 30, IsSystemType = true, DisplayOrder = 1 },
            new() { CategoryId = categories[1].Id, Name = "Drill Press", DefaultMaintenanceIntervalDays = 90, IsSystemType = true, DisplayOrder = 2 },
            new() { CategoryId = categories[1].Id, Name = "Band Saw", DefaultMaintenanceIntervalDays = 60, IsSystemType = true, DisplayOrder = 3 },
            new() { CategoryId = categories[1].Id, Name = "Hand Tools", DefaultMaintenanceIntervalDays = 180, IsSystemType = true, DisplayOrder = 4 }
        };

        var liftingTypes = new List<EquipmentType>
        {
            new() { CategoryId = categories[2].Id, Name = "Overhead Crane", RequiredCertifications = "Load Test,NDT", DefaultMaintenanceIntervalDays = 365, IsSystemType = true, DisplayOrder = 1 },
            new() { CategoryId = categories[2].Id, Name = "Forklift", RequiredCertifications = "Load Test", DefaultMaintenanceIntervalDays = 365, IsSystemType = true, DisplayOrder = 2 },
            new() { CategoryId = categories[2].Id, Name = "Chain Hoist", RequiredCertifications = "Load Test", DefaultMaintenanceIntervalDays = 180, IsSystemType = true, DisplayOrder = 3 },
            new() { CategoryId = categories[2].Id, Name = "Slings/Chains", RequiredCertifications = "Load Test", DefaultMaintenanceIntervalDays = 90, IsSystemType = true, DisplayOrder = 4 }
        };

        var allTypes = fabricationTypes.Concat(workshopTypes).Concat(liftingTypes).ToList();
        foreach (var type in allTypes)
        {
            type.CreatedDate = DateTime.UtcNow;
        }

        _context.EquipmentTypes.AddRange(allTypes);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded default equipment categories and types for company {CompanyId}", companyId);
    }

    public async Task<bool> HasDefaultCategoriesAsync(int companyId)
    {
        return await _context.EquipmentCategories
            .AnyAsync(c => c.CompanyId == companyId && c.IsSystemCategory);
    }

    #endregion
}
