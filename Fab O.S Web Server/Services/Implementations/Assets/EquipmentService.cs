using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Equipment management
/// </summary>
public class EquipmentService : IEquipmentService
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly IQRCodeService _qrCodeService;
    private readonly IUserManagementService _userService;
    private readonly ILogger<EquipmentService> _logger;

    public EquipmentService(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        IQRCodeService qrCodeService,
        IUserManagementService userService,
        ILogger<EquipmentService> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _qrCodeService = qrCodeService;
        _userService = userService;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<Equipment?> GetByIdAsync(int id)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public async Task<Equipment?> GetByCodeAsync(string equipmentCode, int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .FirstOrDefaultAsync(e => e.EquipmentCode == equipmentCode && e.CompanyId == companyId && !e.IsDeleted);
    }

    public async Task<Equipment?> GetByQRCodeIdentifierAsync(string qrCodeIdentifier)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .FirstOrDefaultAsync(e => e.QRCodeIdentifier == qrCodeIdentifier && !e.IsDeleted);
    }

    public async Task<IEnumerable<Equipment>> GetAllAsync(int companyId, bool includeDeleted = false)
    {
        var query = _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId);

        if (!includeDeleted)
            query = query.Where(e => !e.IsDeleted);

        return await query.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetPagedAsync(int companyId, int page, int pageSize, string? search = null, int? categoryId = null, int? typeId = null, EquipmentStatus? status = null, string? location = null)
    {
        var query = BuildFilteredQuery(companyId, search, categoryId, typeId, status, location);

        return await query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int companyId, string? search = null, int? categoryId = null, int? typeId = null, EquipmentStatus? status = null, string? location = null)
    {
        var query = BuildFilteredQuery(companyId, search, categoryId, typeId, status, location);
        return await query.CountAsync();
    }

    private IQueryable<Equipment> BuildFilteredQuery(int companyId, string? search, int? categoryId, int? typeId, EquipmentStatus? status, string? location)
    {
        var query = _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(e =>
                e.Name.ToLower().Contains(search) ||
                e.EquipmentCode.ToLower().Contains(search) ||
                (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(search)) ||
                (e.Description != null && e.Description.ToLower().Contains(search)));
        }

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);

        if (typeId.HasValue)
            query = query.Where(e => e.TypeId == typeId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.LocationLegacy == location);

        return query;
    }

    public async Task<Equipment> CreateAsync(Equipment equipment, int companyId, string? createdBy = null)
    {
        equipment.CompanyId = companyId;
        equipment.EquipmentCode = await _numberSeriesService.GetNextNumberAsync("Equipment", companyId);
        equipment.CreatedDate = DateTime.UtcNow;
        equipment.CreatedBy = createdBy;

        // Generate QR code
        equipment.QRCodeIdentifier = _qrCodeService.GenerateQRCodeIdentifier(0, equipment.EquipmentCode);

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        // Now generate QR code with actual ID
        equipment.QRCodeIdentifier = _qrCodeService.GenerateQRCodeIdentifier(equipment.Id, equipment.EquipmentCode);
        var qrResult = await _qrCodeService.GenerateEquipmentQRCodeAsync(equipment.Id);
        if (qrResult.Success)
        {
            equipment.QRCodeData = qrResult.QRCodeDataUrl;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created equipment {EquipmentCode} for company {CompanyId}", equipment.EquipmentCode, companyId);
        return equipment;
    }

    public async Task<Equipment> UpdateAsync(Equipment equipment, string? modifiedBy = null)
    {
        equipment.LastModified = DateTime.UtcNow;
        equipment.LastModifiedBy = modifiedBy;

        _context.Equipment.Update(equipment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated equipment {Id}", equipment.Id);
        return equipment;
    }

    public async Task<bool> DeleteAsync(int id, bool hardDelete = false)
    {
        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null)
            return false;

        if (hardDelete)
        {
            _context.Equipment.Remove(equipment);
        }
        else
        {
            equipment.IsDeleted = true;
            equipment.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("{DeleteType} equipment {Id}", hardDelete ? "Hard deleted" : "Soft deleted", id);
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Equipment.AnyAsync(e => e.Id == id && !e.IsDeleted);
    }

    #endregion

    #region Status Operations

    public async Task<Equipment> UpdateStatusAsync(int id, EquipmentStatus status, string? modifiedBy = null)
    {
        var equipment = await GetByIdAsync(id);
        if (equipment == null)
            throw new InvalidOperationException($"Equipment with ID {id} not found");

        equipment.Status = status;
        equipment.LastModified = DateTime.UtcNow;
        equipment.LastModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        return equipment;
    }

    public async Task<IEnumerable<Equipment>> GetByStatusAsync(int companyId, EquipmentStatus status)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.Status == status && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Dictionary<EquipmentStatus, int>> GetStatusCountsAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    #endregion

    #region Assignment Operations

    public async Task<Equipment> AssignToUserAsync(int id, int userId, string userName, string? modifiedBy = null)
    {
        var equipment = await GetByIdAsync(id);
        if (equipment == null)
            throw new InvalidOperationException($"Equipment with ID {id} not found");

        // Fetch user from global user management system to validate and get real name
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found in system");

        // Use the actual user name from the system, falling back to provided name if user name fields are empty
        var actualUserName = !string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName)
            ? $"{user.FirstName} {user.LastName}".Trim()
            : userName;

        equipment.AssignedToUserId = userId;
        equipment.AssignedTo = actualUserName;
        equipment.LastModified = DateTime.UtcNow;
        equipment.LastModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Assigned equipment {EquipmentId} to user {UserId} ({UserName})", id, userId, actualUserName);
        return equipment;
    }

    public async Task<Equipment> UnassignAsync(int id, string? modifiedBy = null)
    {
        var equipment = await GetByIdAsync(id);
        if (equipment == null)
            throw new InvalidOperationException($"Equipment with ID {id} not found");

        equipment.AssignedToUserId = null;
        equipment.AssignedTo = null;
        equipment.LastModified = DateTime.UtcNow;
        equipment.LastModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        return equipment;
    }

    public async Task<IEnumerable<Equipment>> GetByAssignedUserAsync(int userId, int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.AssignedToUserId == userId && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetUnassignedAsync(int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.AssignedToUserId == null && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    #endregion

    #region Location Operations

    public async Task<Equipment> UpdateLocationAsync(int id, string location, string? modifiedBy = null)
    {
        var equipment = await GetByIdAsync(id);
        if (equipment == null)
            throw new InvalidOperationException($"Equipment with ID {id} not found");

        equipment.LocationLegacy = location;
        equipment.LastModified = DateTime.UtcNow;
        equipment.LastModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        return equipment;
    }

    public async Task<IEnumerable<Equipment>> GetByLocationAsync(int companyId, string location)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.LocationLegacy == location && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetLocationsAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.LocationLegacy != null && !e.IsDeleted)
            .Select(e => e.LocationLegacy!)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetEquipmentCountsByLocationAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.LocationLegacy != null && !e.IsDeleted)
            .GroupBy(e => e.LocationLegacy!)
            .Select(g => new { Location = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Location, x => x.Count);
    }

    #endregion

    #region Maintenance Scheduling

    public async Task<Equipment> UpdateMaintenanceDatesAsync(int id, DateTime? lastMaintenance, DateTime? nextMaintenance, string? modifiedBy = null)
    {
        var equipment = await GetByIdAsync(id);
        if (equipment == null)
            throw new InvalidOperationException($"Equipment with ID {id} not found");

        equipment.LastMaintenanceDate = lastMaintenance;
        equipment.NextMaintenanceDate = nextMaintenance;
        equipment.LastModified = DateTime.UtcNow;
        equipment.LastModifiedBy = modifiedBy;

        await _context.SaveChangesAsync();
        return equipment;
    }

    public async Task<IEnumerable<Equipment>> GetDueForMaintenanceAsync(int companyId, int daysAhead = 7)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.NextMaintenanceDate != null &&
                        e.NextMaintenanceDate <= futureDate &&
                        e.NextMaintenanceDate >= DateTime.UtcNow &&
                        e.Status == EquipmentStatus.Active &&
                        !e.IsDeleted)
            .OrderBy(e => e.NextMaintenanceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetOverdueMaintenanceAsync(int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.NextMaintenanceDate != null &&
                        e.NextMaintenanceDate < DateTime.UtcNow &&
                        e.Status == EquipmentStatus.Active &&
                        !e.IsDeleted)
            .OrderBy(e => e.NextMaintenanceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetByMaintenanceIntervalAsync(int companyId, int intervalDays)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.MaintenanceIntervalDays == intervalDays &&
                        !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    #endregion

    #region Category and Type Operations

    public async Task<IEnumerable<Equipment>> GetByCategoryAsync(int companyId, int categoryId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.CategoryId == categoryId && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetByTypeAsync(int companyId, int typeId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.TypeId == typeId && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetEquipmentCountsByCategoryAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
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

    #region Warranty Operations

    public async Task<IEnumerable<Equipment>> GetWithExpiringWarrantyAsync(int companyId, int daysAhead = 30)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.WarrantyExpiry != null &&
                        e.WarrantyExpiry <= futureDate &&
                        e.WarrantyExpiry >= DateTime.UtcNow &&
                        !e.IsDeleted)
            .OrderBy(e => e.WarrantyExpiry)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetUnderWarrantyAsync(int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.WarrantyExpiry != null &&
                        e.WarrantyExpiry > DateTime.UtcNow &&
                        !e.IsDeleted)
            .OrderBy(e => e.WarrantyExpiry)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetWarrantyExpiredAsync(int companyId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.WarrantyExpiry != null &&
                        e.WarrantyExpiry < DateTime.UtcNow &&
                        !e.IsDeleted)
            .OrderBy(e => e.WarrantyExpiry)
            .ToListAsync();
    }

    #endregion

    #region Search Operations

    public async Task<IEnumerable<Equipment>> SearchAsync(int companyId, string searchTerm)
    {
        searchTerm = searchTerm.ToLower();
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        !e.IsDeleted &&
                        (e.Name.ToLower().Contains(searchTerm) ||
                         e.EquipmentCode.ToLower().Contains(searchTerm) ||
                         (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(searchTerm)) ||
                         (e.Description != null && e.Description.ToLower().Contains(searchTerm)) ||
                         (e.Manufacturer != null && e.Manufacturer.ToLower().Contains(searchTerm)) ||
                         (e.Model != null && e.Model.ToLower().Contains(searchTerm))))
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> SearchBySerialNumberAsync(int companyId, string serialNumber)
    {
        serialNumber = serialNumber.ToLower();
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.SerialNumber != null &&
                        e.SerialNumber.ToLower().Contains(serialNumber) &&
                        !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> SearchByManufacturerAsync(int companyId, string manufacturer)
    {
        manufacturer = manufacturer.ToLower();
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId &&
                        e.Manufacturer != null &&
                        e.Manufacturer.ToLower().Contains(manufacturer) &&
                        !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    #endregion

    #region Analytics

    public async Task<decimal> GetTotalAssetValueAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.CurrentValue != null && !e.IsDeleted)
            .SumAsync(e => e.CurrentValue ?? 0);
    }

    public async Task<decimal> GetAverageAssetValueAsync(int companyId)
    {
        var values = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.CurrentValue != null && !e.IsDeleted)
            .Select(e => e.CurrentValue ?? 0)
            .ToListAsync();

        return values.Any() ? values.Average() : 0;
    }

    public async Task<IEnumerable<Equipment>> GetRecentlyAddedAsync(int companyId, int count = 10)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Equipment>> GetRecentlyModifiedAsync(int companyId, int count = 10)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.CompanyId == companyId && e.LastModified != null && !e.IsDeleted)
            .OrderByDescending(e => e.LastModified)
            .Take(count)
            .ToListAsync();
    }

    #endregion

    #region Dashboard

    public async Task<int> GetTotalCountAsync(int companyId)
    {
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && !e.IsDeleted)
            .CountAsync();
    }

    public async Task<Dictionary<int, int>> GetCategoryCountsAsync(int companyId)
    {
        return await GetEquipmentCountsByCategoryAsync(companyId);
    }

    public async Task<int> GetMaintenanceDueCountAsync(int companyId, int daysAhead = 30)
    {
        var futureDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Equipment
            .Where(e => e.CompanyId == companyId && !e.IsDeleted &&
                   e.NextMaintenanceDate != null && e.NextMaintenanceDate <= futureDate)
            .CountAsync();
    }

    public async Task<decimal> GetTotalValueAsync(int companyId)
    {
        return await GetTotalAssetValueAsync(companyId);
    }

    #endregion
}
