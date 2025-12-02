using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service for managing tenant locations
/// </summary>
public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        ApplicationDbContext context,
        NumberSeriesService numberSeriesService,
        ILogger<LocationService> logger)
    {
        _context = context;
        _numberSeriesService = numberSeriesService;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<Location?> GetByIdAsync(int id)
    {
        return await _context.Locations
            .Include(l => l.Equipment.Where(e => !e.IsDeleted))
            .Include(l => l.Kits.Where(k => !k.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);
    }

    public async Task<Location?> GetByCodeAsync(string locationCode, int companyId)
    {
        return await _context.Locations
            .Include(l => l.Equipment.Where(e => !e.IsDeleted))
            .Include(l => l.Kits.Where(k => !k.IsDeleted))
            .FirstOrDefaultAsync(l => l.LocationCode == locationCode && l.CompanyId == companyId && !l.IsDeleted);
    }

    public async Task<IEnumerable<Location>> GetAllAsync(int companyId, bool includeDeleted = false)
    {
        var query = _context.Locations
            .Include(l => l.Equipment.Where(e => !e.IsDeleted))
            .Include(l => l.Kits.Where(k => !k.IsDeleted))
            .Where(l => l.CompanyId == companyId);

        if (!includeDeleted)
            query = query.Where(l => !l.IsDeleted);

        return await query.OrderBy(l => l.Name).ToListAsync();
    }

    public async Task<IEnumerable<Location>> GetPagedAsync(
        int companyId,
        int page,
        int pageSize,
        string? search = null,
        LocationType? type = null,
        bool? isActive = null)
    {
        var query = BuildFilteredQuery(companyId, search, type, isActive);

        return await query
            .Include(l => l.Equipment.Where(e => !e.IsDeleted))
            .Include(l => l.Kits.Where(k => !k.IsDeleted))
            .OrderBy(l => l.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(
        int companyId,
        string? search = null,
        LocationType? type = null,
        bool? isActive = null)
    {
        var query = BuildFilteredQuery(companyId, search, type, isActive);
        return await query.CountAsync();
    }

    private IQueryable<Location> BuildFilteredQuery(
        int companyId,
        string? search,
        LocationType? type,
        bool? isActive)
    {
        var query = _context.Locations
            .Where(l => l.CompanyId == companyId && !l.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(searchLower) ||
                l.LocationCode.ToLower().Contains(searchLower) ||
                (l.Description != null && l.Description.ToLower().Contains(searchLower)) ||
                (l.Address != null && l.Address.ToLower().Contains(searchLower)));
        }

        if (type.HasValue)
            query = query.Where(l => l.Type == type.Value);

        if (isActive.HasValue)
            query = query.Where(l => l.IsActive == isActive.Value);

        return query;
    }

    public async Task<Location> CreateAsync(Location location, int companyId, int? createdByUserId = null)
    {
        location.CompanyId = companyId;
        location.LocationCode = await GenerateLocationCodeAsync(companyId, location.Type);
        location.CreatedDate = DateTime.UtcNow;
        location.CreatedByUserId = createdByUserId;

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created location {LocationCode} ({Name}) for company {CompanyId}",
            location.LocationCode, location.Name, companyId);

        return location;
    }

    private async Task<string> GenerateLocationCodeAsync(int companyId, LocationType type)
    {
        // Use a single number series for all locations to ensure unique codes
        // The code format will be LOC-YYYY-NNNN regardless of type
        var number = await _numberSeriesService.GetNextNumberAsync("Location", companyId);
        return number;
    }

    public async Task<Location> UpdateAsync(Location location, int? modifiedByUserId = null)
    {
        location.LastModified = DateTime.UtcNow;
        location.LastModifiedByUserId = modifiedByUserId;

        _context.Locations.Update(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated location {Id} ({LocationCode})", location.Id, location.LocationCode);

        return location;
    }

    public async Task<bool> DeleteAsync(int id, bool hardDelete = false)
    {
        var location = await _context.Locations
            .Include(l => l.Equipment)
            .Include(l => l.Kits)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (location == null)
            return false;

        // Check if location has assigned equipment or kits
        if (location.Equipment.Any(e => !e.IsDeleted) || location.Kits.Any(k => !k.IsDeleted))
        {
            throw new InvalidOperationException(
                "Cannot delete location with assigned equipment or kits. Please reassign them first.");
        }

        if (hardDelete)
        {
            _context.Locations.Remove(location);
            _logger.LogInformation("Hard deleted location {Id} ({LocationCode})", id, location.LocationCode);
        }
        else
        {
            location.IsDeleted = true;
            location.LastModified = DateTime.UtcNow;
            _logger.LogInformation("Soft deleted location {Id} ({LocationCode})", id, location.LocationCode);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Queries

    public async Task<IEnumerable<Location>> GetByTypeAsync(int companyId, LocationType type)
    {
        return await _context.Locations
            .Include(l => l.Equipment.Where(e => !e.IsDeleted))
            .Include(l => l.Kits.Where(k => !k.IsDeleted))
            .Where(l => l.CompanyId == companyId && l.Type == type && !l.IsDeleted)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Location>> GetActiveLocationsAsync(int companyId)
    {
        return await _context.Locations
            .Include(l => l.Equipment.Where(e => !e.IsDeleted))
            .Include(l => l.Kits.Where(k => !k.IsDeleted))
            .Where(l => l.CompanyId == companyId && l.IsActive && !l.IsDeleted)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    #endregion

    #region Equipment Assignment

    public async Task<int> GetEquipmentCountAsync(int locationId)
    {
        return await _context.Equipment
            .CountAsync(e => e.LocationId == locationId && !e.IsDeleted);
    }

    public async Task<int> GetKitCountAsync(int locationId)
    {
        return await _context.EquipmentKits
            .CountAsync(k => k.LocationId == locationId && !k.IsDeleted);
    }

    public async Task<IEnumerable<Equipment>> GetEquipmentAtLocationAsync(int locationId)
    {
        return await _context.Equipment
            .Include(e => e.EquipmentCategory)
            .Include(e => e.EquipmentType)
            .Where(e => e.LocationId == locationId && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<EquipmentKit>> GetKitsAtLocationAsync(int locationId)
    {
        return await _context.EquipmentKits
            .Include(k => k.KitTemplate)
            .Include(k => k.KitItems)
            .ThenInclude(ki => ki.Equipment)
            .Where(k => k.LocationId == locationId && !k.IsDeleted)
            .OrderBy(k => k.Name)
            .ToListAsync();
    }

    public async Task AssignEquipmentToLocationAsync(int locationId, List<int> equipmentIds, int? modifiedByUserId = null)
    {
        var location = await GetByIdAsync(locationId);
        if (location == null)
            throw new InvalidOperationException($"Location with ID {locationId} not found");

        var equipmentList = await _context.Equipment
            .Where(e => equipmentIds.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();

        if (equipmentList.Count != equipmentIds.Count)
            throw new InvalidOperationException("Some equipment items were not found");

        foreach (var equipment in equipmentList)
        {
            equipment.LocationId = locationId;
            equipment.LastModified = DateTime.UtcNow;
            equipment.LastModifiedBy = modifiedByUserId?.ToString();
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned {Count} equipment items to location {LocationId}",
            equipmentIds.Count, locationId);
    }

    public async Task AssignKitsToLocationAsync(int locationId, List<int> kitIds, int? modifiedByUserId = null)
    {
        var location = await GetByIdAsync(locationId);
        if (location == null)
            throw new InvalidOperationException($"Location with ID {locationId} not found");

        var kitList = await _context.EquipmentKits
            .Where(k => kitIds.Contains(k.Id) && !k.IsDeleted)
            .ToListAsync();

        if (kitList.Count != kitIds.Count)
            throw new InvalidOperationException("Some kits were not found");

        foreach (var kit in kitList)
        {
            kit.LocationId = locationId;
            kit.LastModified = DateTime.UtcNow;
            kit.LastModifiedByUserId = modifiedByUserId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned {Count} kits to location {LocationId}",
            kitIds.Count, locationId);
    }

    #endregion

    #region Activation

    public async Task<Location> ActivateAsync(int id, int? modifiedByUserId = null)
    {
        var location = await GetByIdAsync(id);
        if (location == null)
            throw new InvalidOperationException($"Location with ID {id} not found");

        location.IsActive = true;
        location.LastModified = DateTime.UtcNow;
        location.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated location {Id} ({LocationCode})", id, location.LocationCode);

        return location;
    }

    public async Task<Location> DeactivateAsync(int id, int? modifiedByUserId = null)
    {
        var location = await GetByIdAsync(id);
        if (location == null)
            throw new InvalidOperationException($"Location with ID {id} not found");

        location.IsActive = false;
        location.LastModified = DateTime.UtcNow;
        location.LastModifiedByUserId = modifiedByUserId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated location {Id} ({LocationCode})", id, location.LocationCode);

        return location;
    }

    #endregion

    #region Dashboard

    public async Task<LocationDashboardData> GetDashboardAsync(int companyId)
    {
        var locations = await _context.Locations
            .Where(l => l.CompanyId == companyId && !l.IsDeleted)
            .ToListAsync();

        var equipmentAllocated = await _context.Equipment
            .CountAsync(e => e.CompanyId == companyId && e.LocationId != null && !e.IsDeleted);

        var kitsAllocated = await _context.EquipmentKits
            .CountAsync(k => k.CompanyId == companyId && k.LocationId != null && !k.IsDeleted);

        return new LocationDashboardData
        {
            TotalLocations = locations.Count,
            ActiveLocations = locations.Count(l => l.IsActive),
            PhysicalSites = locations.Count(l => l.Type == LocationType.PhysicalSite),
            JobSites = locations.Count(l => l.Type == LocationType.JobSite),
            Vehicles = locations.Count(l => l.Type == LocationType.Vehicle),
            TotalEquipmentAllocated = equipmentAllocated,
            TotalKitsAllocated = kitsAllocated
        };
    }

    #endregion
}
