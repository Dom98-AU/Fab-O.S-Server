using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Equipment management
/// </summary>
public interface IEquipmentService
{
    #region CRUD Operations

    Task<Equipment?> GetByIdAsync(int id);
    Task<Equipment?> GetByCodeAsync(string equipmentCode, int companyId);
    Task<Equipment?> GetByQRCodeIdentifierAsync(string qrCodeIdentifier);
    Task<IEnumerable<Equipment>> GetAllAsync(int companyId, bool includeDeleted = false);
    Task<IEnumerable<Equipment>> GetPagedAsync(int companyId, int page, int pageSize, string? search = null, int? categoryId = null, int? typeId = null, EquipmentStatus? status = null, string? location = null);
    Task<int> GetCountAsync(int companyId, string? search = null, int? categoryId = null, int? typeId = null, EquipmentStatus? status = null, string? location = null);
    Task<Equipment> CreateAsync(Equipment equipment, int companyId, string? createdBy = null);
    Task<Equipment> UpdateAsync(Equipment equipment, string? modifiedBy = null);
    Task<bool> DeleteAsync(int id, bool hardDelete = false);
    Task<bool> ExistsAsync(int id);

    #endregion

    #region Status Operations

    Task<Equipment> UpdateStatusAsync(int id, EquipmentStatus status, string? modifiedBy = null);
    Task<IEnumerable<Equipment>> GetByStatusAsync(int companyId, EquipmentStatus status);
    Task<Dictionary<EquipmentStatus, int>> GetStatusCountsAsync(int companyId);

    #endregion

    #region Assignment Operations

    Task<Equipment> AssignToUserAsync(int id, int userId, string userName, string? modifiedBy = null);
    Task<Equipment> UnassignAsync(int id, string? modifiedBy = null);
    Task<IEnumerable<Equipment>> GetByAssignedUserAsync(int userId, int companyId);
    Task<IEnumerable<Equipment>> GetUnassignedAsync(int companyId);

    #endregion

    #region Location Operations

    Task<Equipment> UpdateLocationAsync(int id, string location, string? modifiedBy = null);
    Task<IEnumerable<Equipment>> GetByLocationAsync(int companyId, string location);
    Task<IEnumerable<string>> GetLocationsAsync(int companyId);
    Task<Dictionary<string, int>> GetEquipmentCountsByLocationAsync(int companyId);

    #endregion

    #region Maintenance Scheduling

    Task<Equipment> UpdateMaintenanceDatesAsync(int id, DateTime? lastMaintenance, DateTime? nextMaintenance, string? modifiedBy = null);
    Task<IEnumerable<Equipment>> GetDueForMaintenanceAsync(int companyId, int daysAhead = 7);
    Task<IEnumerable<Equipment>> GetOverdueMaintenanceAsync(int companyId);
    Task<IEnumerable<Equipment>> GetByMaintenanceIntervalAsync(int companyId, int intervalDays);

    #endregion

    #region Category and Type Operations

    Task<IEnumerable<Equipment>> GetByCategoryAsync(int companyId, int categoryId);
    Task<IEnumerable<Equipment>> GetByTypeAsync(int companyId, int typeId);
    Task<Dictionary<int, int>> GetEquipmentCountsByCategoryAsync(int companyId);
    Task<Dictionary<int, int>> GetEquipmentCountsByTypeAsync(int companyId);

    #endregion

    #region Warranty Operations

    Task<IEnumerable<Equipment>> GetWithExpiringWarrantyAsync(int companyId, int daysAhead = 30);
    Task<IEnumerable<Equipment>> GetUnderWarrantyAsync(int companyId);
    Task<IEnumerable<Equipment>> GetWarrantyExpiredAsync(int companyId);

    #endregion

    #region Search Operations

    Task<IEnumerable<Equipment>> SearchAsync(int companyId, string searchTerm);
    Task<IEnumerable<Equipment>> SearchBySerialNumberAsync(int companyId, string serialNumber);
    Task<IEnumerable<Equipment>> SearchByManufacturerAsync(int companyId, string manufacturer);

    #endregion

    #region Analytics

    Task<decimal> GetTotalAssetValueAsync(int companyId);
    Task<decimal> GetAverageAssetValueAsync(int companyId);
    Task<IEnumerable<Equipment>> GetRecentlyAddedAsync(int companyId, int count = 10);
    Task<IEnumerable<Equipment>> GetRecentlyModifiedAsync(int companyId, int count = 10);

    #endregion

    #region Dashboard

    Task<int> GetTotalCountAsync(int companyId);
    Task<Dictionary<int, int>> GetCategoryCountsAsync(int companyId);
    Task<int> GetMaintenanceDueCountAsync(int companyId, int daysAhead = 30);
    Task<decimal> GetTotalValueAsync(int companyId);

    #endregion
}
