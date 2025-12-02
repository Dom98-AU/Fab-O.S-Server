using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Equipment Certification management
/// </summary>
public interface ICertificationService
{
    #region CRUD Operations

    Task<EquipmentCertification?> GetCertificationByIdAsync(int id);
    Task<IEnumerable<EquipmentCertification>> GetCertificationsByEquipmentAsync(int equipmentId);
    Task<IEnumerable<EquipmentCertification>> GetCertificationsPagedAsync(int companyId, int page, int pageSize, int? equipmentId = null, string? status = null, string? type = null);
    Task<int> GetCertificationsCountAsync(int companyId, int? equipmentId = null, string? status = null, string? type = null);
    Task<EquipmentCertification> CreateCertificationAsync(EquipmentCertification certification);
    Task<EquipmentCertification> UpdateCertificationAsync(EquipmentCertification certification);
    Task<bool> DeleteCertificationAsync(int id);

    #endregion

    #region Status Operations

    Task<IEnumerable<EquipmentCertification>> GetValidCertificationsAsync(int companyId);
    Task<IEnumerable<EquipmentCertification>> GetExpiredCertificationsAsync(int companyId);
    Task<IEnumerable<EquipmentCertification>> GetExpiringCertificationsAsync(int companyId, int daysAhead = 30);
    Task<IEnumerable<EquipmentCertification>> GetCertificationsByStatusAsync(int companyId, string status);
    Task UpdateCertificationStatusesAsync(int companyId);

    #endregion

    #region Renewal and Revocation

    Task<EquipmentCertification> RenewCertificationAsync(int id, DateTime newIssueDate, DateTime? newExpiryDate, string? newCertificateNumber = null, string? documentUrl = null);
    Task<bool> RevokeCertificationAsync(int id, string reason);

    #endregion

    #region Type Operations

    Task<IEnumerable<string>> GetCertificationTypesAsync(int companyId);
    Task<IEnumerable<EquipmentCertification>> GetCertificationsByTypeAsync(int companyId, string certificationType);

    #endregion

    #region Compliance

    Task<bool> IsEquipmentFullyCompliantAsync(int equipmentId);
    Task<IEnumerable<Equipment>> GetNonCompliantEquipmentAsync(int companyId);
    Task<IEnumerable<string>> GetMissingCertificationTypesAsync(int equipmentId);

    #endregion

    #region Analytics

    Task<Dictionary<string, int>> GetCertificationCountByStatusAsync(int companyId);
    Task<Dictionary<string, int>> GetCertificationCountByTypeAsync(int companyId);
    Task<IEnumerable<EquipmentCertification>> GetRecentCertificationsAsync(int companyId, int count = 10);

    #endregion
}
