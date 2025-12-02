using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Equipment Manual management
/// </summary>
public interface IEquipmentManualService
{
    #region CRUD Operations

    Task<EquipmentManual?> GetManualByIdAsync(int id);
    Task<IEnumerable<EquipmentManual>> GetManualsByEquipmentAsync(int equipmentId);
    Task<IEnumerable<EquipmentManual>> GetManualsPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, string? type = null);
    Task<int> GetManualsCountAsync(int companyId, int? equipmentId = null, string? type = null);
    Task<EquipmentManual> CreateManualAsync(EquipmentManual manual);
    Task<EquipmentManual> UpdateManualAsync(EquipmentManual manual);
    Task<bool> DeleteManualAsync(int id);

    #endregion

    #region Type Operations

    Task<IEnumerable<string>> GetManualTypesAsync(int companyId);
    Task<IEnumerable<string>> GetManualTypesForEquipmentAsync(int equipmentId);
    Task<IEnumerable<EquipmentManual>> GetManualsByTypeAsync(int equipmentId, string manualType);

    #endregion

    #region Search Operations

    Task<IEnumerable<EquipmentManual>> SearchManualsAsync(int companyId, string searchTerm);
    Task<IEnumerable<EquipmentManual>> SearchManualsByTitleAsync(int companyId, string title);

    #endregion

    #region Bulk Operations

    Task<bool> CopyManualsToEquipmentAsync(int sourceEquipmentId, int targetEquipmentId);
    Task<bool> DeleteAllManualsForEquipmentAsync(int equipmentId);

    #endregion

    #region Document Operations

    Task<bool> UpdateManualDocumentAsync(int id, string documentUrl, string? fileName = null, long? fileSize = null, string? contentType = null);
    Task<string?> GetManualDocumentUrlAsync(int id);
    Task<bool> UpdateManualVersionAsync(int id, string version);

    #endregion

    #region Analytics

    Task<Dictionary<int, int>> GetManualCountsByEquipmentAsync(int companyId);
    Task<Dictionary<string, int>> GetManualCountsByTypeAsync(int companyId);
    Task<IEnumerable<EquipmentManual>> GetRecentManualsAsync(int companyId, int count = 10);
    Task<IEnumerable<Equipment>> GetEquipmentWithoutManualsAsync(int companyId);
    Task<long> GetTotalManualStorageSizeAsync(int companyId);

    #endregion
}
