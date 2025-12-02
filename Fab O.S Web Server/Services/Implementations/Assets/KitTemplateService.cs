using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Kit Template management
/// </summary>
public class KitTemplateService : IKitTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly ILogger<KitTemplateService> _logger;

    public KitTemplateService(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        ILogger<KitTemplateService> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<KitTemplate?> GetByIdAsync(int id)
    {
        return await _context.KitTemplates
            .Include(kt => kt.TemplateItems)
                .ThenInclude(ti => ti.EquipmentType)
            .FirstOrDefaultAsync(kt => kt.Id == id && !kt.IsDeleted);
    }

    public async Task<KitTemplate?> GetByCodeAsync(string templateCode, int companyId)
    {
        return await _context.KitTemplates
            .Include(kt => kt.TemplateItems)
            .FirstOrDefaultAsync(kt => kt.TemplateCode == templateCode
                && kt.CompanyId == companyId
                && !kt.IsDeleted);
    }

    public async Task<IEnumerable<KitTemplate>> GetAllAsync(int companyId, bool includeDeleted = false)
    {
        var query = _context.KitTemplates
            .Include(kt => kt.TemplateItems)
            .Where(kt => kt.CompanyId == companyId);

        if (!includeDeleted)
            query = query.Where(kt => !kt.IsDeleted);

        return await query.OrderBy(kt => kt.Name).ToListAsync();
    }

    public async Task<IEnumerable<KitTemplate>> GetPagedAsync(int companyId, int page, int pageSize,
        string? search = null, string? category = null, bool? isActive = null)
    {
        var query = BuildQuery(companyId, search, category, isActive);

        return await query
            .OrderBy(kt => kt.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int companyId, string? search = null, string? category = null, bool? isActive = null)
    {
        var query = BuildQuery(companyId, search, category, isActive);
        return await query.CountAsync();
    }

    private IQueryable<KitTemplate> BuildQuery(int companyId, string? search, string? category, bool? isActive)
    {
        var query = _context.KitTemplates
            .Where(kt => kt.CompanyId == companyId && !kt.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(kt =>
                kt.Name.ToLower().Contains(searchLower) ||
                kt.TemplateCode.ToLower().Contains(searchLower) ||
                (kt.Description != null && kt.Description.ToLower().Contains(searchLower)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(kt => kt.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(kt => kt.IsActive == isActive.Value);
        }

        return query;
    }

    public async Task<KitTemplate> CreateAsync(KitTemplate template, int companyId, int? createdByUserId = null)
    {
        template.CompanyId = companyId;
        template.CreatedDate = DateTime.UtcNow;
        template.CreatedByUserId = createdByUserId;

        // Generate template code if not provided
        if (string.IsNullOrEmpty(template.TemplateCode))
        {
            template.TemplateCode = await _numberSeriesService.GetNextNumberAsync("KitTemplate", companyId);
        }

        _context.KitTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created kit template {TemplateId}: {TemplateName} for company {CompanyId}",
            template.Id, template.Name, companyId);

        return template;
    }

    public async Task<KitTemplate> UpdateAsync(KitTemplate template, int? modifiedByUserId = null)
    {
        var existing = await GetByIdAsync(template.Id);
        if (existing == null)
            throw new InvalidOperationException($"Kit template with ID {template.Id} not found");

        existing.Name = template.Name;
        existing.Description = template.Description;
        existing.Category = template.Category;
        existing.IconClass = template.IconClass;
        existing.DefaultCheckoutDays = template.DefaultCheckoutDays;
        existing.RequiresSignature = template.RequiresSignature;
        existing.RequiresConditionCheck = template.RequiresConditionCheck;
        existing.IsActive = template.IsActive;
        existing.LastModified = DateTime.UtcNow;
        existing.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated kit template {TemplateId}: {TemplateName}",
            template.Id, template.Name);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, bool hardDelete = false)
    {
        var template = await GetByIdAsync(id);
        if (template == null)
            return false;

        // Check if template is in use
        var hasKits = await _context.EquipmentKits.AnyAsync(ek => ek.KitTemplateId == id && !ek.IsDeleted);
        if (hasKits && !hardDelete)
        {
            // Soft delete only if template is in use
            template.IsDeleted = true;
            template.LastModified = DateTime.UtcNow;
        }
        else if (hardDelete)
        {
            _context.KitTemplates.Remove(template);
        }
        else
        {
            template.IsDeleted = true;
            template.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("{DeleteType} kit template {TemplateId}",
            hardDelete ? "Hard deleted" : "Soft deleted", id);

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.KitTemplates.AnyAsync(kt => kt.Id == id && !kt.IsDeleted);
    }

    #endregion

    #region Template Items

    public async Task<KitTemplate?> GetWithItemsAsync(int id)
    {
        return await _context.KitTemplates
            .Include(kt => kt.TemplateItems.OrderBy(ti => ti.DisplayOrder))
                .ThenInclude(ti => ti.EquipmentType)
                    .ThenInclude(et => et!.EquipmentCategory)
            .FirstOrDefaultAsync(kt => kt.Id == id && !kt.IsDeleted);
    }

    public async Task<IEnumerable<KitTemplateItem>> GetTemplateItemsAsync(int templateId)
    {
        return await _context.KitTemplateItems
            .Include(ti => ti.EquipmentType)
                .ThenInclude(et => et!.EquipmentCategory)
            .Where(ti => ti.KitTemplateId == templateId)
            .OrderBy(ti => ti.DisplayOrder)
            .ToListAsync();
    }

    public async Task<KitTemplateItem> AddTemplateItemAsync(int templateId, KitTemplateItem item)
    {
        var template = await GetByIdAsync(templateId);
        if (template == null)
            throw new InvalidOperationException($"Kit template with ID {templateId} not found");

        // Check if equipment type already exists in template
        var existingItem = await _context.KitTemplateItems
            .FirstOrDefaultAsync(ti => ti.KitTemplateId == templateId && ti.EquipmentTypeId == item.EquipmentTypeId);

        if (existingItem != null)
            throw new InvalidOperationException($"Equipment type {item.EquipmentTypeId} already exists in this template");

        item.KitTemplateId = templateId;

        // Auto-assign display order if not set
        if (item.DisplayOrder == 0)
        {
            var maxOrder = await _context.KitTemplateItems
                .Where(ti => ti.KitTemplateId == templateId)
                .MaxAsync(ti => (int?)ti.DisplayOrder) ?? 0;
            item.DisplayOrder = maxOrder + 1;
        }

        _context.KitTemplateItems.Add(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added template item {ItemId} to template {TemplateId}",
            item.Id, templateId);

        return item;
    }

    public async Task<bool> RemoveTemplateItemAsync(int templateId, int itemId)
    {
        var item = await _context.KitTemplateItems
            .FirstOrDefaultAsync(ti => ti.Id == itemId && ti.KitTemplateId == templateId);

        if (item == null)
            return false;

        _context.KitTemplateItems.Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed template item {ItemId} from template {TemplateId}",
            itemId, templateId);

        return true;
    }

    public async Task<KitTemplateItem> UpdateTemplateItemAsync(KitTemplateItem item)
    {
        var existing = await _context.KitTemplateItems
            .FirstOrDefaultAsync(ti => ti.Id == item.Id);

        if (existing == null)
            throw new InvalidOperationException($"Template item with ID {item.Id} not found");

        existing.Quantity = item.Quantity;
        existing.IsMandatory = item.IsMandatory;
        existing.DisplayOrder = item.DisplayOrder;
        existing.Notes = item.Notes;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated template item {ItemId}", item.Id);

        return existing;
    }

    public async Task<bool> ReorderTemplateItemsAsync(int templateId, List<int> itemIds)
    {
        var items = await _context.KitTemplateItems
            .Where(ti => ti.KitTemplateId == templateId && itemIds.Contains(ti.Id))
            .ToListAsync();

        for (int i = 0; i < itemIds.Count; i++)
        {
            var item = items.FirstOrDefault(ti => ti.Id == itemIds[i]);
            if (item != null)
                item.DisplayOrder = i + 1;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Categories

    public async Task<IEnumerable<string>> GetCategoriesAsync(int companyId)
    {
        return await _context.KitTemplates
            .Where(kt => kt.CompanyId == companyId && !kt.IsDeleted && kt.Category != null)
            .Select(kt => kt.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetTemplateCategoryCountsAsync(int companyId)
    {
        return await _context.KitTemplates
            .Where(kt => kt.CompanyId == companyId && !kt.IsDeleted && kt.Category != null)
            .GroupBy(kt => kt.Category!)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    #endregion

    #region Activation

    public async Task<KitTemplate> ActivateAsync(int id, int? modifiedByUserId = null)
    {
        var template = await GetByIdAsync(id);
        if (template == null)
            throw new InvalidOperationException($"Kit template with ID {id} not found");

        template.IsActive = true;
        template.LastModified = DateTime.UtcNow;
        template.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated kit template {TemplateId}", id);

        return template;
    }

    public async Task<KitTemplate> DeactivateAsync(int id, int? modifiedByUserId = null)
    {
        var template = await GetByIdAsync(id);
        if (template == null)
            throw new InvalidOperationException($"Kit template with ID {id} not found");

        template.IsActive = false;
        template.LastModified = DateTime.UtcNow;
        template.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated kit template {TemplateId}", id);

        return template;
    }

    public async Task<IEnumerable<KitTemplate>> GetActiveTemplatesAsync(int companyId)
    {
        return await _context.KitTemplates
            .Include(kt => kt.TemplateItems)
            .Where(kt => kt.CompanyId == companyId && !kt.IsDeleted && kt.IsActive)
            .OrderBy(kt => kt.Name)
            .ToListAsync();
    }

    #endregion

    #region Dashboard

    public async Task<int> GetTotalCountAsync(int companyId)
    {
        return await _context.KitTemplates
            .CountAsync(kt => kt.CompanyId == companyId && !kt.IsDeleted);
    }

    public async Task<int> GetActiveCountAsync(int companyId)
    {
        return await _context.KitTemplates
            .CountAsync(kt => kt.CompanyId == companyId && !kt.IsDeleted && kt.IsActive);
    }

    public async Task<Dictionary<string, int>> GetTemplatesByCategory(int companyId)
    {
        var templates = await _context.KitTemplates
            .Where(kt => kt.CompanyId == companyId && !kt.IsDeleted)
            .ToListAsync();

        return templates
            .GroupBy(kt => kt.Category ?? "Uncategorized")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion
}
