using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.DTOs.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Equipment Kit management
/// </summary>
public interface IEquipmentKitService
{
    #region CRUD Operations

    Task<EquipmentKit?> GetByIdAsync(int id);
    Task<EquipmentKit?> GetByCodeAsync(string kitCode, int companyId);
    Task<EquipmentKit?> GetByQRCodeIdentifierAsync(string qrCodeIdentifier);
    Task<IEnumerable<EquipmentKit>> GetAllAsync(int companyId, bool includeDeleted = false);
    Task<IEnumerable<EquipmentKit>> GetPagedAsync(int companyId, int page, int pageSize,
        string? search = null, KitStatus? status = null, int? templateId = null, int? assignedToUserId = null);
    Task<int> GetCountAsync(int companyId, string? search = null, KitStatus? status = null, int? templateId = null, int? assignedToUserId = null);
    Task<EquipmentKit> UpdateAsync(EquipmentKit kit, int? modifiedByUserId = null);
    Task<bool> DeleteAsync(int id, bool hardDelete = false);
    Task<bool> ExistsAsync(int id);

    #endregion

    #region Kit Creation

    /// <summary>
    /// Create a kit from a predefined template
    /// </summary>
    Task<EquipmentKit> CreateFromTemplateAsync(int templateId, int companyId, List<int> equipmentIds,
        string? name = null, string? description = null, string? location = null, int? createdByUserId = null);

    /// <summary>
    /// Create an ad-hoc kit (not based on a template)
    /// </summary>
    Task<EquipmentKit> CreateAdHocAsync(string name, int companyId, List<int> equipmentIds,
        string? description = null, string? location = null, int? createdByUserId = null);

    #endregion

    #region Kit Items

    Task<EquipmentKit?> GetWithItemsAsync(int id);
    Task<IEnumerable<EquipmentKitItem>> GetKitItemsAsync(int kitId);
    Task<EquipmentKitItem> AddItemToKitAsync(int kitId, int equipmentId, int? templateItemId = null,
        int displayOrder = 0, string? notes = null, int? addedByUserId = null);
    Task<bool> RemoveItemFromKitAsync(int kitId, int equipmentId);
    Task<bool> SwapItemAsync(int kitId, int oldEquipmentId, int newEquipmentId, int? modifiedByUserId = null);
    Task<bool> ReorderKitItemsAsync(int kitId, List<int> equipmentIds);

    #endregion

    #region Available Equipment

    /// <summary>
    /// Get equipment available to be added to kits (not currently in any kit)
    /// </summary>
    Task<IEnumerable<Equipment>> GetAvailableEquipmentForKitAsync(int companyId, int? templateId = null);

    /// <summary>
    /// Validate that a kit based on a template is complete (all mandatory items present)
    /// </summary>
    Task<bool> ValidateKitCompletenessAsync(int kitId);

    /// <summary>
    /// Get missing items for a template-based kit
    /// </summary>
    Task<IEnumerable<KitTemplateItem>> GetMissingTemplateItemsAsync(int kitId);

    #endregion

    #region Status Operations

    Task<EquipmentKit> UpdateStatusAsync(int id, KitStatus status, int? modifiedByUserId = null);
    Task<IEnumerable<EquipmentKit>> GetAvailableKitsAsync(int companyId);
    Task<IEnumerable<EquipmentKit>> GetCheckedOutKitsAsync(int companyId, int? userId = null);
    Task<IEnumerable<EquipmentKit>> GetOverdueKitsAsync(int companyId);
    Task<Dictionary<KitStatus, int>> GetStatusCountsAsync(int companyId);

    #endregion

    #region Maintenance Flagging

    Task<EquipmentKit> FlagItemForMaintenanceAsync(int kitId, int equipmentId, string? notes = null, int? flaggedByUserId = null);
    Task<EquipmentKit> ClearMaintenanceFlagAsync(int kitId, int equipmentId, int? clearedByUserId = null);
    Task<IEnumerable<EquipmentKit>> GetKitsWithMaintenanceFlagsAsync(int companyId);
    Task<IEnumerable<EquipmentKitItem>> GetItemsNeedingMaintenanceAsync(int companyId);

    #endregion

    #region QR Code Operations

    Task<QRCodeGenerationResult> GenerateKitQRCodeAsync(int kitId);
    Task<QRCodeGenerationResult> RegenerateKitQRCodeAsync(int kitId);

    #endregion

    #region Assignment Operations

    Task<EquipmentKit> AssignToUserAsync(int kitId, int userId, int? modifiedByUserId = null);
    Task<EquipmentKit> UnassignAsync(int kitId, int? modifiedByUserId = null);
    Task<IEnumerable<EquipmentKit>> GetByAssignedUserAsync(int userId, int companyId);
    Task<IEnumerable<EquipmentKit>> GetUnassignedAsync(int companyId);

    #endregion

    #region Dashboard

    Task<KitDashboardDto> GetDashboardAsync(int companyId);
    Task<int> GetTotalCountAsync(int companyId);
    Task<int> GetAvailableCountAsync(int companyId);
    Task<int> GetCheckedOutCountAsync(int companyId);
    Task<int> GetMaintenanceFlaggedCountAsync(int companyId);

    #endregion
}
