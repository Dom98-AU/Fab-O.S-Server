using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Implementations.CloudStorage;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for Equipment Attachments (photos, documents, certificates)
/// Photos are stored in Azure Blob Storage, documents/certificates in SharePoint
/// </summary>
[ApiController]
[Route("api/assets/attachments")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class AssetAttachmentsController : ControllerBase
{
    private readonly IEquipmentAttachmentService _attachmentService;
    private readonly AzureBlobStorageSettings _blobSettings;
    private readonly ILogger<AssetAttachmentsController> _logger;

    public AssetAttachmentsController(
        IEquipmentAttachmentService attachmentService,
        IOptions<AzureBlobStorageSettings> blobSettings,
        ILogger<AssetAttachmentsController> logger)
    {
        _attachmentService = attachmentService;
        _blobSettings = blobSettings.Value;
        _logger = logger;
    }

    #region Upload Endpoints

    /// <summary>
    /// Upload a single attachment
    /// </summary>
    /// <remarks>
    /// Upload a photo, document, or certificate for an equipment, kit, location, or maintenance record.
    /// Photos are stored in Azure Blob Storage with automatic thumbnail generation.
    /// Documents and certificates are stored in SharePoint.
    /// </remarks>
    [HttpPost("upload")]
    [RequestSizeLimit(104857600)] // 100MB
    public async Task<ActionResult<AttachmentUploadResult>> UploadAttachment(
        [FromQuery] int companyId,
        [FromQuery] int userId,
        [FromForm] IFormFile file,
        [FromForm] int? equipmentId,
        [FromForm] int? equipmentKitId,
        [FromForm] int? locationId,
        [FromForm] int? maintenanceRecordId,
        [FromForm] AttachmentType type,
        [FromForm] AttachmentCategory category = AttachmentCategory.Other,
        [FromForm] string? title = null,
        [FromForm] string? description = null,
        [FromForm] DateTime? expiryDate = null,
        [FromForm] string? certificateNumber = null,
        [FromForm] string? issuingAuthority = null,
        [FromForm] bool setAsPrimaryPhoto = false)
    {
        try
        {
            // Validate at least one parent entity is specified
            if (!equipmentId.HasValue && !equipmentKitId.HasValue &&
                !locationId.HasValue && !maintenanceRecordId.HasValue)
            {
                return BadRequest(new { message = "At least one parent entity (equipmentId, equipmentKitId, locationId, or maintenanceRecordId) must be specified" });
            }

            var request = new UploadAttachmentRequest
            {
                EquipmentId = equipmentId,
                EquipmentKitId = equipmentKitId,
                LocationId = locationId,
                MaintenanceRecordId = maintenanceRecordId,
                Type = type,
                Category = category,
                Title = title,
                Description = description,
                ExpiryDate = expiryDate,
                CertificateNumber = certificateNumber,
                IssuingAuthority = issuingAuthority,
                SetAsPrimaryPhoto = setAsPrimaryPhoto
            };

            var result = await _attachmentService.UploadAsync(file, request, companyId, userId);

            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment for company {CompanyId}", companyId);
            return StatusCode(500, new { message = "Error uploading attachment", error = ex.Message });
        }
    }

    /// <summary>
    /// Upload multiple attachments at once
    /// </summary>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(524288000)] // 500MB total
    public async Task<ActionResult<MultipleAttachmentUploadResult>> UploadMultipleAttachments(
        [FromQuery] int companyId,
        [FromQuery] int userId,
        [FromForm] List<IFormFile> files,
        [FromForm] int? equipmentId,
        [FromForm] int? equipmentKitId,
        [FromForm] int? locationId,
        [FromForm] int? maintenanceRecordId,
        [FromForm] AttachmentType type,
        [FromForm] AttachmentCategory category = AttachmentCategory.Other,
        [FromForm] string? title = null,
        [FromForm] string? description = null)
    {
        try
        {
            if (files == null || !files.Any())
                return BadRequest(new { message = "No files provided" });

            if (!equipmentId.HasValue && !equipmentKitId.HasValue &&
                !locationId.HasValue && !maintenanceRecordId.HasValue)
            {
                return BadRequest(new { message = "At least one parent entity must be specified" });
            }

            var request = new UploadAttachmentRequest
            {
                EquipmentId = equipmentId,
                EquipmentKitId = equipmentKitId,
                LocationId = locationId,
                MaintenanceRecordId = maintenanceRecordId,
                Type = type,
                Category = category,
                Title = title,
                Description = description
            };

            var result = await _attachmentService.UploadMultipleAsync(files, request, companyId, userId);

            if (result.FailedCount > 0 && result.SuccessCount == 0)
                return BadRequest(result);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple attachments for company {CompanyId}", companyId);
            return StatusCode(500, new { message = "Error uploading attachments", error = ex.Message });
        }
    }

    #endregion

    #region Get Endpoints

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentAttachmentDto>> GetAttachment(int id, [FromQuery] int companyId)
    {
        try
        {
            var attachment = await _attachmentService.GetByIdAsync(id, companyId);
            if (attachment == null)
                return NotFound(new { message = $"Attachment with ID {id} not found" });

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachment {Id}", id);
            return StatusCode(500, new { message = "Error retrieving attachment" });
        }
    }

    /// <summary>
    /// Get all attachments for an equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}")]
    public async Task<ActionResult<AttachmentListResponse>> GetEquipmentAttachments(
        int equipmentId,
        [FromQuery] int companyId,
        [FromQuery] AttachmentType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _attachmentService.GetForEquipmentAsync(equipmentId, companyId, type, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving attachments" });
        }
    }

    /// <summary>
    /// Get photos only for an equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/photos")]
    public async Task<ActionResult<List<EquipmentAttachmentDto>>> GetEquipmentPhotos(
        int equipmentId,
        [FromQuery] int companyId)
    {
        try
        {
            var photos = await _attachmentService.GetPhotosAsync(equipmentId, companyId);
            return Ok(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photos for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving photos" });
        }
    }

    /// <summary>
    /// Get primary photo for an equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/photos/primary")]
    public async Task<ActionResult<EquipmentAttachmentDto>> GetEquipmentPrimaryPhoto(
        int equipmentId,
        [FromQuery] int companyId)
    {
        try
        {
            var photo = await _attachmentService.GetPrimaryPhotoAsync(equipmentId, companyId);
            if (photo == null)
                return NotFound(new { message = "No primary photo set for this equipment" });

            return Ok(photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting primary photo for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving primary photo" });
        }
    }

    /// <summary>
    /// Get photo gallery for an equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/gallery")]
    public async Task<ActionResult<PhotoGalleryDto>> GetEquipmentPhotoGallery(
        int equipmentId,
        [FromQuery] int companyId)
    {
        try
        {
            var gallery = await _attachmentService.GetPhotoGalleryAsync(equipmentId, companyId);
            return Ok(gallery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photo gallery for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving photo gallery" });
        }
    }

    /// <summary>
    /// Get documents only for an equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/documents")]
    public async Task<ActionResult<List<EquipmentAttachmentDto>>> GetEquipmentDocuments(
        int equipmentId,
        [FromQuery] int companyId)
    {
        try
        {
            var documents = await _attachmentService.GetDocumentsAsync(equipmentId, companyId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving documents" });
        }
    }

    /// <summary>
    /// Get certificates only for an equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/certificates")]
    public async Task<ActionResult<List<EquipmentAttachmentDto>>> GetEquipmentCertificates(
        int equipmentId,
        [FromQuery] int companyId)
    {
        try
        {
            var certificates = await _attachmentService.GetCertificatesAsync(equipmentId, companyId);
            return Ok(certificates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificates for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving certificates" });
        }
    }

    /// <summary>
    /// Get all attachments for an equipment kit
    /// </summary>
    [HttpGet("kit/{kitId:int}")]
    public async Task<ActionResult<AttachmentListResponse>> GetKitAttachments(
        int kitId,
        [FromQuery] int companyId,
        [FromQuery] AttachmentType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _attachmentService.GetForKitAsync(kitId, companyId, type, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for kit {KitId}", kitId);
            return StatusCode(500, new { message = "Error retrieving attachments" });
        }
    }

    /// <summary>
    /// Get all attachments for a location
    /// </summary>
    [HttpGet("location/{locationId:int}")]
    public async Task<ActionResult<AttachmentListResponse>> GetLocationAttachments(
        int locationId,
        [FromQuery] int companyId,
        [FromQuery] AttachmentType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _attachmentService.GetForLocationAsync(locationId, companyId, type, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for location {LocationId}", locationId);
            return StatusCode(500, new { message = "Error retrieving attachments" });
        }
    }

    /// <summary>
    /// Get all attachments for a maintenance record
    /// </summary>
    [HttpGet("maintenance/{maintenanceRecordId:int}")]
    public async Task<ActionResult<AttachmentListResponse>> GetMaintenanceRecordAttachments(
        int maintenanceRecordId,
        [FromQuery] int companyId,
        [FromQuery] AttachmentType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _attachmentService.GetForMaintenanceRecordAsync(
                maintenanceRecordId, companyId, type, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attachments for maintenance record {MaintenanceRecordId}",
                maintenanceRecordId);
            return StatusCode(500, new { message = "Error retrieving attachments" });
        }
    }

    /// <summary>
    /// Get certificate summary with expiry tracking
    /// </summary>
    [HttpGet("certificates/summary")]
    public async Task<ActionResult<CertificateAttachmentSummaryDto>> GetCertificateSummary(
        [FromQuery] int companyId,
        [FromQuery] int? equipmentId = null)
    {
        try
        {
            var summary = await _attachmentService.GetCertificateSummaryAsync(companyId, equipmentId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate summary for company {CompanyId}", companyId);
            return StatusCode(500, new { message = "Error retrieving certificate summary" });
        }
    }

    #endregion

    #region Download Endpoints

    /// <summary>
    /// Download attachment file
    /// </summary>
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int id, [FromQuery] int companyId)
    {
        try
        {
            var attachment = await _attachmentService.GetByIdAsync(id, companyId);
            if (attachment == null)
                return NotFound(new { message = $"Attachment with ID {id} not found" });

            var stream = await _attachmentService.DownloadAsync(id, companyId);
            if (stream == null)
                return NotFound(new { message = "File not found in storage" });

            return File(stream, attachment.ContentType, attachment.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {Id}", id);
            return StatusCode(500, new { message = "Error downloading attachment" });
        }
    }

    /// <summary>
    /// Get secure download URL (with SAS token for Azure Blob)
    /// </summary>
    [HttpGet("{id:int}/download-url")]
    public async Task<ActionResult<AttachmentDownloadResponse>> GetDownloadUrl(
        int id,
        [FromQuery] int companyId,
        [FromQuery] int? expirationMinutes = null)
    {
        try
        {
            TimeSpan? expiration = expirationMinutes.HasValue
                ? TimeSpan.FromMinutes(expirationMinutes.Value)
                : null;

            var response = await _attachmentService.GetDownloadUrlAsync(id, companyId, expiration);
            if (response == null)
                return NotFound(new { message = $"Attachment with ID {id} not found" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download URL for attachment {Id}", id);
            return StatusCode(500, new { message = "Error getting download URL" });
        }
    }

    /// <summary>
    /// Get thumbnail URL for a photo
    /// </summary>
    [HttpGet("{id:int}/thumbnail-url")]
    public async Task<ActionResult<string>> GetThumbnailUrl(int id, [FromQuery] int companyId)
    {
        try
        {
            var url = await _attachmentService.GetThumbnailUrlAsync(id, companyId);
            if (url == null)
                return NotFound(new { message = "Thumbnail not available for this attachment" });

            return Ok(new { thumbnailUrl = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail URL for attachment {Id}", id);
            return StatusCode(500, new { message = "Error getting thumbnail URL" });
        }
    }

    #endregion

    #region Photo Operations

    /// <summary>
    /// Set an attachment as the primary photo for equipment
    /// </summary>
    [HttpPost("{id:int}/set-primary")]
    public async Task<ActionResult> SetPrimaryPhoto(
        int id,
        [FromQuery] int equipmentId,
        [FromQuery] int companyId)
    {
        try
        {
            var result = await _attachmentService.SetPrimaryPhotoAsync(id, equipmentId, companyId);
            if (!result)
                return NotFound(new { message = "Attachment not found or is not a photo" });

            return Ok(new { message = "Primary photo set successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary photo for attachment {Id}", id);
            return StatusCode(500, new { message = "Error setting primary photo" });
        }
    }

    /// <summary>
    /// Clear primary photo for equipment
    /// </summary>
    [HttpPost("equipment/{equipmentId:int}/clear-primary")]
    public async Task<ActionResult> ClearPrimaryPhoto(int equipmentId, [FromQuery] int companyId)
    {
        try
        {
            await _attachmentService.ClearPrimaryPhotoAsync(equipmentId, companyId);
            return Ok(new { message = "Primary photo cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing primary photo for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error clearing primary photo" });
        }
    }

    #endregion

    #region Update/Delete Endpoints

    /// <summary>
    /// Update attachment metadata
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentAttachmentDto>> UpdateAttachment(
        int id,
        [FromQuery] int companyId,
        [FromQuery] int userId,
        [FromBody] UpdateAttachmentRequest request)
    {
        try
        {
            var attachment = await _attachmentService.UpdateMetadataAsync(id, request, companyId, userId);
            if (attachment == null)
                return NotFound(new { message = $"Attachment with ID {id} not found" });

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attachment {Id}", id);
            return StatusCode(500, new { message = "Error updating attachment" });
        }
    }

    /// <summary>
    /// Delete attachment (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAttachment(int id, [FromQuery] int companyId)
    {
        try
        {
            var result = await _attachmentService.DeleteAsync(id, companyId);
            if (!result)
                return NotFound(new { message = $"Attachment with ID {id} not found" });

            return Ok(new { message = "Attachment deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {Id}", id);
            return StatusCode(500, new { message = "Error deleting attachment" });
        }
    }

    /// <summary>
    /// Permanently delete attachment and file from storage
    /// </summary>
    [HttpDelete("{id:int}/permanent")]
    public async Task<ActionResult> PermanentDeleteAttachment(int id, [FromQuery] int companyId)
    {
        try
        {
            var result = await _attachmentService.PermanentDeleteAsync(id, companyId);
            if (!result)
                return NotFound(new { message = $"Attachment with ID {id} not found or could not be deleted" });

            return Ok(new { message = "Attachment permanently deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting attachment {Id}", id);
            return StatusCode(500, new { message = "Error permanently deleting attachment" });
        }
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Validate a file before upload
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult> ValidateFile(
        [FromForm] IFormFile file,
        [FromForm] AttachmentType type)
    {
        try
        {
            var (isValid, errorMessage) = await _attachmentService.ValidateFileAsync(file, type);

            if (!isValid)
                return BadRequest(new { valid = false, message = errorMessage });

            return Ok(new
            {
                valid = true,
                fileName = file.FileName,
                size = file.Length,
                contentType = file.ContentType,
                type = type.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file");
            return StatusCode(500, new { message = "Error validating file" });
        }
    }

    /// <summary>
    /// Get allowed file extensions for each attachment type
    /// </summary>
    [HttpGet("allowed-extensions")]
    public ActionResult GetAllowedExtensions()
    {
        return Ok(new
        {
            photo = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic" },
            document = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" },
            certificate = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" }
        });
    }

    /// <summary>
    /// Get maximum file size allowed
    /// </summary>
    [HttpGet("max-file-size")]
    public ActionResult GetMaxFileSize()
    {
        var maxSizeMB = _blobSettings.MaxFileSizeMB;
        return Ok(new
        {
            maxFileSizeBytes = maxSizeMB * 1024 * 1024,
            maxFileSizeMB = maxSizeMB
        });
    }

    #endregion
}
