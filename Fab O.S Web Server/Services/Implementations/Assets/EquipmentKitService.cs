using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Equipment Kit management
/// </summary>
public class EquipmentKitService : IEquipmentKitService
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly IQRCodeService _qrCodeService;
    private readonly IUserManagementService _userService;
    private readonly ILogger<EquipmentKitService> _logger;

    public EquipmentKitService(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        IQRCodeService qrCodeService,
        IUserManagementService userService,
        ILogger<EquipmentKitService> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _qrCodeService = qrCodeService;
        _userService = userService;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<EquipmentKit?> GetByIdAsync(int id)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitTemplate)
            .Include(ek => ek.KitItems)
                .ThenInclude(ki => ki.Equipment)
            .FirstOrDefaultAsync(ek => ek.Id == id && !ek.IsDeleted);
    }

    public async Task<EquipmentKit?> GetByCodeAsync(string kitCode, int companyId)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitItems)
            .FirstOrDefaultAsync(ek => ek.KitCode == kitCode
                && ek.CompanyId == companyId
                && !ek.IsDeleted);
    }

    public async Task<EquipmentKit?> GetByQRCodeIdentifierAsync(string qrCodeIdentifier)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitItems)
                .ThenInclude(ki => ki.Equipment)
            .FirstOrDefaultAsync(ek => ek.QRCodeIdentifier == qrCodeIdentifier && !ek.IsDeleted);
    }

    public async Task<IEnumerable<EquipmentKit>> GetAllAsync(int companyId, bool includeDeleted = false)
    {
        var query = _context.EquipmentKits
            .Include(ek => ek.KitTemplate)
            .Include(ek => ek.KitItems)
            .Where(ek => ek.CompanyId == companyId);

        if (!includeDeleted)
            query = query.Where(ek => !ek.IsDeleted);

        return await query.OrderBy(ek => ek.Name).ToListAsync();
    }

    public async Task<IEnumerable<EquipmentKit>> GetPagedAsync(int companyId, int page, int pageSize,
        string? search = null, KitStatus? status = null, int? templateId = null, int? assignedToUserId = null)
    {
        var query = BuildQuery(companyId, search, status, templateId, assignedToUserId);

        return await query
            .Include(ek => ek.KitTemplate)
            .Include(ek => ek.KitItems)
            .OrderBy(ek => ek.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int companyId, string? search = null, KitStatus? status = null,
        int? templateId = null, int? assignedToUserId = null)
    {
        var query = BuildQuery(companyId, search, status, templateId, assignedToUserId);
        return await query.CountAsync();
    }

    private IQueryable<EquipmentKit> BuildQuery(int companyId, string? search, KitStatus? status,
        int? templateId, int? assignedToUserId)
    {
        var query = _context.EquipmentKits
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(ek =>
                ek.Name.ToLower().Contains(searchLower) ||
                ek.KitCode.ToLower().Contains(searchLower) ||
                (ek.Description != null && ek.Description.ToLower().Contains(searchLower)));
        }

        if (status.HasValue)
            query = query.Where(ek => ek.Status == status.Value);

        if (templateId.HasValue)
            query = query.Where(ek => ek.KitTemplateId == templateId.Value);

        if (assignedToUserId.HasValue)
            query = query.Where(ek => ek.AssignedToUserId == assignedToUserId.Value);

        return query;
    }

    public async Task<EquipmentKit> UpdateAsync(EquipmentKit kit, int? modifiedByUserId = null)
    {
        var existing = await GetByIdAsync(kit.Id);
        if (existing == null)
            throw new InvalidOperationException($"Equipment kit with ID {kit.Id} not found");

        existing.Name = kit.Name;
        existing.Description = kit.Description;
        existing.Location = kit.Location;
        existing.LastModified = DateTime.UtcNow;
        existing.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated equipment kit {KitId}: {KitName}", kit.Id, kit.Name);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, bool hardDelete = false)
    {
        var kit = await GetByIdAsync(id);
        if (kit == null)
            return false;

        // Check if kit is currently checked out
        if (kit.Status == KitStatus.CheckedOut)
            throw new InvalidOperationException("Cannot delete a kit that is currently checked out");

        if (hardDelete)
        {
            // Remove all kit items first
            var items = await _context.EquipmentKitItems.Where(ki => ki.KitId == id).ToListAsync();
            _context.EquipmentKitItems.RemoveRange(items);
            _context.EquipmentKits.Remove(kit);
        }
        else
        {
            kit.IsDeleted = true;
            kit.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("{DeleteType} equipment kit {KitId}", hardDelete ? "Hard deleted" : "Soft deleted", id);

        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.EquipmentKits.AnyAsync(ek => ek.Id == id && !ek.IsDeleted);
    }

    #endregion

    #region Kit Creation

    public async Task<EquipmentKit> CreateFromTemplateAsync(int templateId, int companyId, List<int> equipmentIds,
        string? name = null, string? description = null, string? location = null, int? createdByUserId = null)
    {
        var template = await _context.KitTemplates
            .Include(kt => kt.TemplateItems)
            .FirstOrDefaultAsync(kt => kt.Id == templateId && !kt.IsDeleted);

        if (template == null)
            throw new InvalidOperationException($"Kit template with ID {templateId} not found");

        if (!template.IsActive)
            throw new InvalidOperationException($"Kit template {template.Name} is not active");

        // Validate equipment exists and is available
        var equipment = await _context.Equipment
            .Where(e => equipmentIds.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();

        if (equipment.Count != equipmentIds.Count)
            throw new InvalidOperationException("Some equipment items were not found");

        // Check if any equipment is already in another kit
        var inOtherKits = await _context.EquipmentKitItems
            .Include(eki => eki.Kit)
            .Where(eki => equipmentIds.Contains(eki.EquipmentId) && !eki.Kit!.IsDeleted)
            .Select(eki => eki.EquipmentId)
            .ToListAsync();

        if (inOtherKits.Any())
            throw new InvalidOperationException($"Equipment items {string.Join(", ", inOtherKits)} are already in other kits");

        var kit = new EquipmentKit
        {
            CompanyId = companyId,
            KitTemplateId = templateId,
            KitCode = await _numberSeriesService.GetNextNumberAsync("EquipmentKit", companyId),
            Name = name ?? $"{template.Name} Kit",
            Description = description ?? template.Description,
            LocationLegacy = location,
            Status = KitStatus.Available,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.EquipmentKits.Add(kit);
        await _context.SaveChangesAsync();

        // Add kit items
        for (int i = 0; i < equipmentIds.Count; i++)
        {
            var kitItem = new EquipmentKitItem
            {
                KitId = kit.Id,
                EquipmentId = equipmentIds[i],
                DisplayOrder = i + 1,
                AddedDate = DateTime.UtcNow,
                AddedByUserId = createdByUserId
            };
            _context.EquipmentKitItems.Add(kitItem);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created kit {KitId} from template {TemplateId} with {ItemCount} items",
            kit.Id, templateId, equipmentIds.Count);

        return kit;
    }

    public async Task<EquipmentKit> CreateAdHocAsync(string name, int companyId, List<int> equipmentIds,
        string? description = null, string? location = null, int? createdByUserId = null)
    {
        // Validate equipment exists
        var equipment = await _context.Equipment
            .Where(e => equipmentIds.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();

        if (equipment.Count != equipmentIds.Count)
            throw new InvalidOperationException("Some equipment items were not found");

        // Check if any equipment is already in another kit
        var inOtherKits = await _context.EquipmentKitItems
            .Include(eki => eki.Kit)
            .Where(eki => equipmentIds.Contains(eki.EquipmentId) && !eki.Kit!.IsDeleted)
            .Select(eki => eki.EquipmentId)
            .ToListAsync();

        if (inOtherKits.Any())
            throw new InvalidOperationException($"Equipment items {string.Join(", ", inOtherKits)} are already in other kits");

        var kit = new EquipmentKit
        {
            CompanyId = companyId,
            KitTemplateId = null, // Ad-hoc kit
            KitCode = await _numberSeriesService.GetNextNumberAsync("EquipmentKit", companyId),
            Name = name,
            Description = description,
            LocationLegacy = location,
            Status = KitStatus.Available,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.EquipmentKits.Add(kit);
        await _context.SaveChangesAsync();

        // Add kit items
        for (int i = 0; i < equipmentIds.Count; i++)
        {
            var kitItem = new EquipmentKitItem
            {
                KitId = kit.Id,
                EquipmentId = equipmentIds[i],
                DisplayOrder = i + 1,
                AddedDate = DateTime.UtcNow,
                AddedByUserId = createdByUserId
            };
            _context.EquipmentKitItems.Add(kitItem);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ad-hoc kit {KitId} with {ItemCount} items", kit.Id, equipmentIds.Count);

        return kit;
    }

    #endregion

    #region Kit Items

    public async Task<EquipmentKit?> GetWithItemsAsync(int id)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitTemplate)
            .Include(ek => ek.KitItems.OrderBy(ki => ki.DisplayOrder))
                .ThenInclude(ki => ki.Equipment)
                    .ThenInclude(e => e!.EquipmentType)
                        .ThenInclude(et => et!.EquipmentCategory)
            .FirstOrDefaultAsync(ek => ek.Id == id && !ek.IsDeleted);
    }

    public async Task<IEnumerable<EquipmentKitItem>> GetKitItemsAsync(int kitId)
    {
        return await _context.EquipmentKitItems
            .Include(ki => ki.Equipment)
                .ThenInclude(e => e!.EquipmentType)
                    .ThenInclude(et => et!.EquipmentCategory)
            .Where(ki => ki.KitId == kitId)
            .OrderBy(ki => ki.DisplayOrder)
            .ToListAsync();
    }

    public async Task<EquipmentKitItem> AddItemToKitAsync(int kitId, int equipmentId, int? templateItemId = null,
        int displayOrder = 0, string? notes = null, int? addedByUserId = null)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            throw new InvalidOperationException($"Kit with ID {kitId} not found");

        if (kit.Status == KitStatus.CheckedOut)
            throw new InvalidOperationException("Cannot add items to a checked-out kit");

        // Check if equipment exists
        var equipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == equipmentId && !e.IsDeleted);
        if (equipment == null)
            throw new InvalidOperationException($"Equipment with ID {equipmentId} not found");

        // Check if equipment is already in a kit
        var existingKitItem = await _context.EquipmentKitItems
            .Include(eki => eki.Kit)
            .FirstOrDefaultAsync(eki => eki.EquipmentId == equipmentId && !eki.Kit!.IsDeleted);

        if (existingKitItem != null)
            throw new InvalidOperationException($"Equipment {equipmentId} is already in kit {existingKitItem.Kit!.Name}");

        if (displayOrder == 0)
        {
            var maxOrder = await _context.EquipmentKitItems
                .Where(ki => ki.KitId == kitId)
                .MaxAsync(ki => (int?)ki.DisplayOrder) ?? 0;
            displayOrder = maxOrder + 1;
        }

        var kitItem = new EquipmentKitItem
        {
            KitId = kitId,
            EquipmentId = equipmentId,
            TemplateItemId = templateItemId,
            DisplayOrder = displayOrder,
            Notes = notes,
            AddedDate = DateTime.UtcNow,
            AddedByUserId = addedByUserId
        };

        _context.EquipmentKitItems.Add(kitItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added equipment {EquipmentId} to kit {KitId}", equipmentId, kitId);

        return kitItem;
    }

    public async Task<bool> RemoveItemFromKitAsync(int kitId, int equipmentId)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            return false;

        if (kit.Status == KitStatus.CheckedOut)
            throw new InvalidOperationException("Cannot remove items from a checked-out kit");

        var kitItem = await _context.EquipmentKitItems
            .FirstOrDefaultAsync(ki => ki.KitId == kitId && ki.EquipmentId == equipmentId);

        if (kitItem == null)
            return false;

        _context.EquipmentKitItems.Remove(kitItem);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Removed equipment {EquipmentId} from kit {KitId}", equipmentId, kitId);

        return true;
    }

    public async Task<bool> SwapItemAsync(int kitId, int oldEquipmentId, int newEquipmentId, int? modifiedByUserId = null)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            throw new InvalidOperationException($"Kit with ID {kitId} not found");

        if (kit.Status == KitStatus.CheckedOut)
            throw new InvalidOperationException("Cannot swap items in a checked-out kit");

        var kitItem = await _context.EquipmentKitItems
            .FirstOrDefaultAsync(ki => ki.KitId == kitId && ki.EquipmentId == oldEquipmentId);

        if (kitItem == null)
            throw new InvalidOperationException($"Equipment {oldEquipmentId} is not in kit {kitId}");

        // Check new equipment exists and is available
        var newEquipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == newEquipmentId && !e.IsDeleted);
        if (newEquipment == null)
            throw new InvalidOperationException($"Equipment with ID {newEquipmentId} not found");

        var existingKitItem = await _context.EquipmentKitItems
            .Include(eki => eki.Kit)
            .FirstOrDefaultAsync(eki => eki.EquipmentId == newEquipmentId && !eki.Kit!.IsDeleted);

        if (existingKitItem != null)
            throw new InvalidOperationException($"Equipment {newEquipmentId} is already in kit {existingKitItem.Kit!.Name}");

        kitItem.EquipmentId = newEquipmentId;
        kitItem.AddedDate = DateTime.UtcNow;
        kitItem.AddedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Swapped equipment {OldId} with {NewId} in kit {KitId}",
            oldEquipmentId, newEquipmentId, kitId);

        return true;
    }

    public async Task<bool> ReorderKitItemsAsync(int kitId, List<int> equipmentIds)
    {
        var items = await _context.EquipmentKitItems
            .Where(ki => ki.KitId == kitId && equipmentIds.Contains(ki.EquipmentId))
            .ToListAsync();

        for (int i = 0; i < equipmentIds.Count; i++)
        {
            var item = items.FirstOrDefault(ki => ki.EquipmentId == equipmentIds[i]);
            if (item != null)
                item.DisplayOrder = i + 1;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Available Equipment

    public async Task<IEnumerable<Equipment>> GetAvailableEquipmentForKitAsync(int companyId, int? templateId = null)
    {
        // Get equipment not in any kit
        var equipmentInKits = await _context.EquipmentKitItems
            .Include(eki => eki.Kit)
            .Where(eki => !eki.Kit!.IsDeleted)
            .Select(eki => eki.EquipmentId)
            .ToListAsync();

        var query = _context.Equipment
            .Include(e => e.EquipmentType)
                .ThenInclude(et => et!.EquipmentCategory)
            .Where(e => e.CompanyId == companyId
                && !e.IsDeleted
                && e.Status == EquipmentStatus.Active
                && !equipmentInKits.Contains(e.Id));

        // If template specified, filter to matching equipment types
        if (templateId.HasValue)
        {
            var templateTypeIds = await _context.KitTemplateItems
                .Where(ti => ti.KitTemplateId == templateId.Value)
                .Select(ti => ti.EquipmentTypeId)
                .ToListAsync();

            query = query.Where(e => templateTypeIds.Contains(e.TypeId));
        }

        return await query.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<bool> ValidateKitCompletenessAsync(int kitId)
    {
        var kit = await _context.EquipmentKits
            .Include(ek => ek.KitItems)
                .ThenInclude(ki => ki.Equipment)
            .FirstOrDefaultAsync(ek => ek.Id == kitId && !ek.IsDeleted);

        if (kit == null || !kit.KitTemplateId.HasValue)
            return true; // Ad-hoc kits are always "complete"

        var templateItems = await _context.KitTemplateItems
            .Where(ti => ti.KitTemplateId == kit.KitTemplateId.Value && ti.IsMandatory)
            .ToListAsync();

        foreach (var templateItem in templateItems)
        {
            var matchingItems = kit.KitItems
                .Count(ki => ki.Equipment?.TypeId == templateItem.EquipmentTypeId);

            if (matchingItems < templateItem.Quantity)
                return false;
        }

        return true;
    }

    public async Task<IEnumerable<KitTemplateItem>> GetMissingTemplateItemsAsync(int kitId)
    {
        var kit = await _context.EquipmentKits
            .Include(ek => ek.KitItems)
                .ThenInclude(ki => ki.Equipment)
            .FirstOrDefaultAsync(ek => ek.Id == kitId && !ek.IsDeleted);

        if (kit == null || !kit.KitTemplateId.HasValue)
            return Enumerable.Empty<KitTemplateItem>();

        var templateItems = await _context.KitTemplateItems
            .Include(ti => ti.EquipmentType)
            .Where(ti => ti.KitTemplateId == kit.KitTemplateId.Value && ti.IsMandatory)
            .ToListAsync();

        var missingItems = new List<KitTemplateItem>();

        foreach (var templateItem in templateItems)
        {
            var matchingItems = kit.KitItems
                .Count(ki => ki.Equipment?.TypeId == templateItem.EquipmentTypeId);

            if (matchingItems < templateItem.Quantity)
                missingItems.Add(templateItem);
        }

        return missingItems;
    }

    #endregion

    #region Status Operations

    public async Task<EquipmentKit> UpdateStatusAsync(int id, KitStatus status, int? modifiedByUserId = null)
    {
        var kit = await GetByIdAsync(id);
        if (kit == null)
            throw new InvalidOperationException($"Kit with ID {id} not found");

        kit.Status = status;
        kit.LastModified = DateTime.UtcNow;
        kit.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated kit {KitId} status to {Status}", id, status);

        return kit;
    }

    public async Task<IEnumerable<EquipmentKit>> GetAvailableKitsAsync(int companyId)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitItems)
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.Status == KitStatus.Available)
            .OrderBy(ek => ek.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentKit>> GetCheckedOutKitsAsync(int companyId, int? userId = null)
    {
        var query = _context.EquipmentKits
            .Include(ek => ek.KitItems)
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.Status == KitStatus.CheckedOut);

        if (userId.HasValue)
            query = query.Where(ek => ek.AssignedToUserId == userId.Value);

        return await query.OrderBy(ek => ek.Name).ToListAsync();
    }

    public async Task<IEnumerable<EquipmentKit>> GetOverdueKitsAsync(int companyId)
    {
        var now = DateTime.UtcNow;

        return await _context.EquipmentKits
            .Include(ek => ek.Checkouts.Where(c => c.Status == CheckoutStatus.CheckedOut))
            .Where(ek => ek.CompanyId == companyId
                && !ek.IsDeleted
                && ek.Status == KitStatus.CheckedOut
                && ek.Checkouts.Any(c => c.Status == CheckoutStatus.CheckedOut && c.ExpectedReturnDate < now))
            .OrderBy(ek => ek.Name)
            .ToListAsync();
    }

    public async Task<Dictionary<KitStatus, int>> GetStatusCountsAsync(int companyId)
    {
        var kits = await _context.EquipmentKits
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted)
            .ToListAsync();

        return kits
            .GroupBy(ek => ek.Status)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion

    #region Maintenance Flagging

    public async Task<EquipmentKit> FlagItemForMaintenanceAsync(int kitId, int equipmentId, string? notes = null, int? flaggedByUserId = null)
    {
        var kitItem = await _context.EquipmentKitItems
            .Include(ki => ki.Kit)
            .FirstOrDefaultAsync(ki => ki.KitId == kitId && ki.EquipmentId == equipmentId);

        if (kitItem == null)
            throw new InvalidOperationException($"Equipment {equipmentId} is not in kit {kitId}");

        kitItem.NeedsMaintenance = true;
        kitItem.Notes = notes;

        var kit = kitItem.Kit!;
        kit.HasMaintenanceFlag = true;
        kit.MaintenanceFlagNotes = notes;
        kit.LastModified = DateTime.UtcNow;
        kit.LastModifiedByUserId = flaggedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Flagged equipment {EquipmentId} in kit {KitId} for maintenance", equipmentId, kitId);

        return kit;
    }

    public async Task<EquipmentKit> ClearMaintenanceFlagAsync(int kitId, int equipmentId, int? clearedByUserId = null)
    {
        var kitItem = await _context.EquipmentKitItems
            .Include(ki => ki.Kit)
            .FirstOrDefaultAsync(ki => ki.KitId == kitId && ki.EquipmentId == equipmentId);

        if (kitItem == null)
            throw new InvalidOperationException($"Equipment {equipmentId} is not in kit {kitId}");

        kitItem.NeedsMaintenance = false;

        var kit = kitItem.Kit!;

        // Check if any other items need maintenance
        var otherItemsNeedMaintenance = await _context.EquipmentKitItems
            .AnyAsync(ki => ki.KitId == kitId && ki.EquipmentId != equipmentId && ki.NeedsMaintenance);

        if (!otherItemsNeedMaintenance)
        {
            kit.HasMaintenanceFlag = false;
            kit.MaintenanceFlagNotes = null;
        }

        kit.LastModified = DateTime.UtcNow;
        kit.LastModifiedByUserId = clearedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleared maintenance flag for equipment {EquipmentId} in kit {KitId}", equipmentId, kitId);

        return kit;
    }

    public async Task<IEnumerable<EquipmentKit>> GetKitsWithMaintenanceFlagsAsync(int companyId)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitItems.Where(ki => ki.NeedsMaintenance))
                .ThenInclude(ki => ki.Equipment)
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.HasMaintenanceFlag)
            .OrderBy(ek => ek.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentKitItem>> GetItemsNeedingMaintenanceAsync(int companyId)
    {
        return await _context.EquipmentKitItems
            .Include(ki => ki.Kit)
            .Include(ki => ki.Equipment)
            .Where(ki => ki.Kit!.CompanyId == companyId && !ki.Kit.IsDeleted && ki.NeedsMaintenance)
            .ToListAsync();
    }

    #endregion

    #region QR Code Operations

    public async Task<QRCodeGenerationResult> GenerateKitQRCodeAsync(int kitId)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            return new QRCodeGenerationResult { Success = false, ErrorMessage = $"Kit with ID {kitId} not found" };

        if (!string.IsNullOrEmpty(kit.QRCodeData))
            return new QRCodeGenerationResult { Success = false, ErrorMessage = "Kit already has a QR code. Use regenerate instead." };

        var qrData = new EquipmentQRData
        {
            Id = kit.Id,
            Code = kit.KitCode,
            Name = kit.Name,
            Type = "EquipmentKit",
            Category = kit.KitTemplate?.Category,
            GeneratedAt = DateTime.UtcNow.ToString("O")
        };

        var qrResult = _qrCodeService.GenerateQRCode(qrData);

        if (qrResult.Success)
        {
            kit.QRCodeData = qrResult.QRCodeDataUrl;
            kit.QRCodeIdentifier = qrResult.QRCodeIdentifier;
            kit.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return qrResult;
    }

    public async Task<QRCodeGenerationResult> RegenerateKitQRCodeAsync(int kitId)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            return new QRCodeGenerationResult { Success = false, ErrorMessage = $"Kit with ID {kitId} not found" };

        var qrData = new EquipmentQRData
        {
            Id = kit.Id,
            Code = kit.KitCode,
            Name = kit.Name,
            Type = "EquipmentKit",
            Category = kit.KitTemplate?.Category,
            GeneratedAt = DateTime.UtcNow.ToString("O")
        };

        var qrResult = _qrCodeService.GenerateQRCode(qrData);

        if (qrResult.Success)
        {
            kit.QRCodeData = qrResult.QRCodeDataUrl;
            kit.QRCodeIdentifier = qrResult.QRCodeIdentifier;
            kit.LastModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return qrResult;
    }

    #endregion

    #region Assignment Operations

    public async Task<EquipmentKit> AssignToUserAsync(int kitId, int userId, int? modifiedByUserId = null)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            throw new InvalidOperationException($"Kit with ID {kitId} not found");

        // Fetch user from global user management system
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found in system");

        var userName = !string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName)
            ? $"{user.FirstName} {user.LastName}".Trim()
            : user.Email;

        kit.AssignedToUserId = userId;
        kit.AssignedToUserName = userName;
        kit.LastModified = DateTime.UtcNow;
        kit.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned kit {KitId} to user {UserId} ({UserName})", kitId, userId, userName);

        return kit;
    }

    public async Task<EquipmentKit> UnassignAsync(int kitId, int? modifiedByUserId = null)
    {
        var kit = await GetByIdAsync(kitId);
        if (kit == null)
            throw new InvalidOperationException($"Kit with ID {kitId} not found");

        if (kit.Status == KitStatus.CheckedOut)
            throw new InvalidOperationException("Cannot unassign a checked-out kit");

        kit.AssignedToUserId = null;
        kit.AssignedToUserName = null;
        kit.LastModified = DateTime.UtcNow;
        kit.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Unassigned kit {KitId}", kitId);

        return kit;
    }

    public async Task<IEnumerable<EquipmentKit>> GetByAssignedUserAsync(int userId, int companyId)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitItems)
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.AssignedToUserId == userId)
            .OrderBy(ek => ek.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentKit>> GetUnassignedAsync(int companyId)
    {
        return await _context.EquipmentKits
            .Include(ek => ek.KitItems)
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.AssignedToUserId == null)
            .OrderBy(ek => ek.Name)
            .ToListAsync();
    }

    #endregion

    #region Dashboard

    public async Task<KitDashboardDto> GetDashboardAsync(int companyId)
    {
        var kits = await _context.EquipmentKits
            .Include(ek => ek.KitTemplate)
            .Where(ek => ek.CompanyId == companyId && !ek.IsDeleted)
            .ToListAsync();

        var overdueCount = await _context.KitCheckouts
            .Where(kc => kc.CompanyId == companyId
                && kc.Status == CheckoutStatus.CheckedOut
                && kc.ExpectedReturnDate < DateTime.UtcNow)
            .CountAsync();

        return new KitDashboardDto
        {
            TotalKits = kits.Count,
            AvailableKits = kits.Count(ek => ek.Status == KitStatus.Available),
            CheckedOutKits = kits.Count(ek => ek.Status == KitStatus.CheckedOut),
            MaintenanceFlaggedKits = kits.Count(ek => ek.HasMaintenanceFlag),
            OverdueCheckouts = overdueCount,
            KitsByTemplate = kits
                .Where(ek => ek.KitTemplate != null)
                .GroupBy(ek => ek.KitTemplate!.Name)
                .ToDictionary(g => g.Key, g => g.Count()),
            KitsByStatus = kits
                .GroupBy(ek => ek.Status)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<int> GetTotalCountAsync(int companyId)
    {
        return await _context.EquipmentKits.CountAsync(ek => ek.CompanyId == companyId && !ek.IsDeleted);
    }

    public async Task<int> GetAvailableCountAsync(int companyId)
    {
        return await _context.EquipmentKits.CountAsync(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.Status == KitStatus.Available);
    }

    public async Task<int> GetCheckedOutCountAsync(int companyId)
    {
        return await _context.EquipmentKits.CountAsync(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.Status == KitStatus.CheckedOut);
    }

    public async Task<int> GetMaintenanceFlaggedCountAsync(int companyId)
    {
        return await _context.EquipmentKits.CountAsync(ek => ek.CompanyId == companyId && !ek.IsDeleted && ek.HasMaintenanceFlag);
    }

    #endregion
}
