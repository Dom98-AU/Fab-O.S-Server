using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for managing tenant locations
/// </summary>
public interface ILocationService
{
    #region CRUD Operations

    /// <summary>
    /// Get location by ID
    /// </summary>
    Task<Location?> GetByIdAsync(int id);

    /// <summary>
    /// Get location by code within a company
    /// </summary>
    Task<Location?> GetByCodeAsync(string locationCode, int companyId);

    /// <summary>
    /// Get all locations for a company
    /// </summary>
    Task<IEnumerable<Location>> GetAllAsync(int companyId, bool includeDeleted = false);

    /// <summary>
    /// Get paged locations with filtering
    /// </summary>
    Task<IEnumerable<Location>> GetPagedAsync(
        int companyId,
        int page,
        int pageSize,
        string? search = null,
        LocationType? type = null,
        bool? isActive = null);

    /// <summary>
    /// Get total count with filtering
    /// </summary>
    Task<int> GetCountAsync(
        int companyId,
        string? search = null,
        LocationType? type = null,
        bool? isActive = null);

    /// <summary>
    /// Create a new location
    /// </summary>
    Task<Location> CreateAsync(Location location, int companyId, int? createdByUserId = null);

    /// <summary>
    /// Update an existing location
    /// </summary>
    Task<Location> UpdateAsync(Location location, int? modifiedByUserId = null);

    /// <summary>
    /// Delete a location (soft or hard delete)
    /// </summary>
    Task<bool> DeleteAsync(int id, bool hardDelete = false);

    #endregion

    #region Queries

    /// <summary>
    /// Get locations by type
    /// </summary>
    Task<IEnumerable<Location>> GetByTypeAsync(int companyId, LocationType type);

    /// <summary>
    /// Get active locations only
    /// </summary>
    Task<IEnumerable<Location>> GetActiveLocationsAsync(int companyId);

    #endregion

    #region Equipment Assignment

    /// <summary>
    /// Get count of equipment at a location
    /// </summary>
    Task<int> GetEquipmentCountAsync(int locationId);

    /// <summary>
    /// Get count of kits at a location
    /// </summary>
    Task<int> GetKitCountAsync(int locationId);

    /// <summary>
    /// Get all equipment at a location
    /// </summary>
    Task<IEnumerable<Equipment>> GetEquipmentAtLocationAsync(int locationId);

    /// <summary>
    /// Get all kits at a location
    /// </summary>
    Task<IEnumerable<EquipmentKit>> GetKitsAtLocationAsync(int locationId);

    /// <summary>
    /// Assign multiple equipment items to a location
    /// </summary>
    Task AssignEquipmentToLocationAsync(int locationId, List<int> equipmentIds, int? modifiedByUserId = null);

    /// <summary>
    /// Assign multiple kits to a location
    /// </summary>
    Task AssignKitsToLocationAsync(int locationId, List<int> kitIds, int? modifiedByUserId = null);

    #endregion

    #region Activation

    /// <summary>
    /// Activate a location
    /// </summary>
    Task<Location> ActivateAsync(int id, int? modifiedByUserId = null);

    /// <summary>
    /// Deactivate a location
    /// </summary>
    Task<Location> DeactivateAsync(int id, int? modifiedByUserId = null);

    #endregion

    #region Dashboard

    /// <summary>
    /// Get dashboard statistics for locations
    /// </summary>
    Task<LocationDashboardData> GetDashboardAsync(int companyId);

    #endregion
}

/// <summary>
/// Dashboard data for locations
/// </summary>
public class LocationDashboardData
{
    public int TotalLocations { get; set; }
    public int ActiveLocations { get; set; }
    public int PhysicalSites { get; set; }
    public int JobSites { get; set; }
    public int Vehicles { get; set; }
    public int TotalEquipmentAllocated { get; set; }
    public int TotalKitsAllocated { get; set; }
}
