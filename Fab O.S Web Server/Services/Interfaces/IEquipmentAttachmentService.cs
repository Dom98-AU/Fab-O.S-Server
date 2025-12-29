using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using Microsoft.AspNetCore.Http;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for managing equipment attachments (photos, documents, certificates)
/// Supports dual storage: Azure Blob for photos, SharePoint for documents/certificates
/// </summary>
public interface IEquipmentAttachmentService
{
    #region Upload Operations

    /// <summary>
    /// Upload a single attachment
    /// Routes to Azure Blob for photos, SharePoint for documents/certificates
    /// </summary>
    Task<AttachmentUploadResult> UploadAsync(
        IFormFile file,
        UploadAttachmentRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Upload multiple attachments
    /// </summary>
    Task<MultipleAttachmentUploadResult> UploadMultipleAsync(
        IEnumerable<IFormFile> files,
        UploadAttachmentRequest request,
        int companyId,
        int userId);

    #endregion

    #region Get Operations

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    Task<EquipmentAttachmentDto?> GetByIdAsync(int id, int companyId);

    /// <summary>
    /// Get all attachments for an equipment
    /// </summary>
    Task<AttachmentListResponse> GetForEquipmentAsync(
        int equipmentId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get all attachments for an equipment kit
    /// </summary>
    Task<AttachmentListResponse> GetForKitAsync(
        int kitId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get all attachments for a location
    /// </summary>
    Task<AttachmentListResponse> GetForLocationAsync(
        int locationId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get all attachments for a maintenance record
    /// </summary>
    Task<AttachmentListResponse> GetForMaintenanceRecordAsync(
        int maintenanceRecordId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Get the primary photo for an equipment
    /// </summary>
    Task<EquipmentAttachmentDto?> GetPrimaryPhotoAsync(int equipmentId, int companyId);

    /// <summary>
    /// Get photo gallery for an equipment
    /// </summary>
    Task<PhotoGalleryDto> GetPhotoGalleryAsync(int equipmentId, int companyId);

    /// <summary>
    /// Get photos only for an equipment
    /// </summary>
    Task<List<EquipmentAttachmentDto>> GetPhotosAsync(int equipmentId, int companyId);

    /// <summary>
    /// Get documents only for an equipment
    /// </summary>
    Task<List<EquipmentAttachmentDto>> GetDocumentsAsync(int equipmentId, int companyId);

    /// <summary>
    /// Get certificates only for an equipment
    /// </summary>
    Task<List<EquipmentAttachmentDto>> GetCertificatesAsync(int equipmentId, int companyId);

    /// <summary>
    /// Get certificate summary with expiry tracking
    /// </summary>
    Task<CertificateAttachmentSummaryDto> GetCertificateSummaryAsync(int companyId, int? equipmentId = null);

    #endregion

    #region Photo Operations

    /// <summary>
    /// Set an attachment as the primary photo for equipment
    /// </summary>
    Task<bool> SetPrimaryPhotoAsync(int attachmentId, int equipmentId, int companyId);

    /// <summary>
    /// Clear primary photo flag (no primary photo)
    /// </summary>
    Task<bool> ClearPrimaryPhotoAsync(int equipmentId, int companyId);

    #endregion

    #region Download Operations

    /// <summary>
    /// Download attachment file as stream
    /// </summary>
    Task<Stream?> DownloadAsync(int id, int companyId);

    /// <summary>
    /// Get a secure download URL (with SAS token for Azure Blob)
    /// </summary>
    Task<AttachmentDownloadResponse?> GetDownloadUrlAsync(int id, int companyId, TimeSpan? expiration = null);

    /// <summary>
    /// Get thumbnail URL for a photo
    /// </summary>
    Task<string?> GetThumbnailUrlAsync(int id, int companyId);

    #endregion

    #region Update/Delete Operations

    /// <summary>
    /// Update attachment metadata (title, description, category, etc.)
    /// </summary>
    Task<EquipmentAttachmentDto?> UpdateMetadataAsync(int id, UpdateAttachmentRequest request, int companyId, int userId);

    /// <summary>
    /// Soft delete an attachment
    /// </summary>
    Task<bool> DeleteAsync(int id, int companyId);

    /// <summary>
    /// Permanently delete an attachment and its file from storage
    /// </summary>
    Task<bool> PermanentDeleteAsync(int id, int companyId);

    /// <summary>
    /// Delete all attachments for an equipment (used when deleting equipment)
    /// </summary>
    Task<int> DeleteAllForEquipmentAsync(int equipmentId, int companyId);

    #endregion

    #region Validation

    /// <summary>
    /// Validate a file before upload
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage)> ValidateFileAsync(IFormFile file, AttachmentType type);

    /// <summary>
    /// Check if file extension is allowed for attachment type
    /// </summary>
    bool IsAllowedExtension(string fileName, AttachmentType type);

    /// <summary>
    /// Check if file size is within limits
    /// </summary>
    bool IsFileSizeAllowed(long fileSizeBytes);

    #endregion
}
