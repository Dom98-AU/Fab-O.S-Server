using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Implementations.CloudStorage;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for managing equipment attachments
/// Routes photos to Azure Blob Storage, documents/certificates to SharePoint
/// </summary>
public class EquipmentAttachmentService : IEquipmentAttachmentService
{
    private readonly ApplicationDbContext _context;
    private readonly AzureBlobStorageProvider _blobStorage;
    private readonly CloudStorageProviderFactory _cloudStorageFactory;
    private readonly AzureBlobStorageSettings _blobSettings;
    private readonly ILogger<EquipmentAttachmentService> _logger;

    // Allowed extensions by type
    private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
    };

    public EquipmentAttachmentService(
        ApplicationDbContext context,
        AzureBlobStorageProvider blobStorage,
        CloudStorageProviderFactory cloudStorageFactory,
        IOptions<AzureBlobStorageSettings> blobSettings,
        ILogger<EquipmentAttachmentService> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _cloudStorageFactory = cloudStorageFactory;
        _blobSettings = blobSettings.Value;
        _logger = logger;
    }

    #region Upload Operations

    public async Task<AttachmentUploadResult> UploadAsync(
        IFormFile file,
        UploadAttachmentRequest request,
        int companyId,
        int userId)
    {
        try
        {
            // Validate file
            var (isValid, errorMessage) = await ValidateFileAsync(file, request.Type);
            if (!isValid)
            {
                return new AttachmentUploadResult { Success = false, ErrorMessage = errorMessage };
            }

            // Generate storage file name
            var extension = Path.GetExtension(file.FileName);
            var storageFileName = $"{Guid.NewGuid()}{extension}";

            // Build storage path based on parent entity
            var storagePath = BuildStoragePath(companyId, request);

            // Upload to appropriate storage
            CloudFileUploadResult uploadResult;
            string storageProvider;
            string? thumbnailPath = null;

            if (request.Type == AttachmentType.Photo)
            {
                // Photos go to Azure Blob Storage
                storageProvider = "AzureBlob";

                using var stream = file.OpenReadStream();
                var uploadRequest = new CloudFileUploadRequest
                {
                    FolderPath = storagePath,
                    FileName = storageFileName,
                    Content = stream,
                    ContentType = file.ContentType,
                    Metadata = new Dictionary<string, string>
                    {
                        ["OriginalFileName"] = file.FileName,
                        ["CompanyId"] = companyId.ToString(),
                        ["AttachmentType"] = request.Type.ToString()
                    }
                };

                uploadResult = await _blobStorage.UploadFileAsync(uploadRequest);

                // Generate thumbnail for photos
                if (_blobSettings.GenerateThumbnails)
                {
                    thumbnailPath = await GenerateThumbnailAsync(file, storagePath, storageFileName, companyId);
                }
            }
            else
            {
                // Documents and certificates go to SharePoint
                storageProvider = "SharePoint";

                using var stream = file.OpenReadStream();
                var uploadRequest = new CloudFileUploadRequest
                {
                    FolderPath = storagePath,
                    FileName = storageFileName,
                    Content = stream,
                    ContentType = file.ContentType
                };

                // Use SharePoint provider via factory
                var sharePointProvider = _cloudStorageFactory.GetProvider("SharePoint");
                uploadResult = await sharePointProvider.UploadFileAsync(uploadRequest);
            }

            // Get image dimensions if photo
            int? width = null, height = null;
            if (request.Type == AttachmentType.Photo)
            {
                using var imageStream = file.OpenReadStream();
                try
                {
                    using var image = await Image.LoadAsync(imageStream);
                    width = image.Width;
                    height = image.Height;
                }
                catch
                {
                    // Ignore dimension detection errors
                }
            }

            // Check if this should be the primary photo
            var isPrimary = request.Type == AttachmentType.Photo &&
                (request.SetAsPrimaryPhoto || !await HasPrimaryPhotoAsync(request.EquipmentId ?? 0, companyId));

            // Create attachment entity
            var attachment = new EquipmentAttachment
            {
                CompanyId = companyId,
                EquipmentId = request.EquipmentId,
                EquipmentKitId = request.EquipmentKitId,
                LocationId = request.LocationId,
                MaintenanceRecordId = request.MaintenanceRecordId,
                Type = request.Type,
                Category = request.Category,
                FileName = storageFileName,
                OriginalFileName = file.FileName,
                Title = request.Title ?? file.FileName,
                Description = request.Description,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                StorageProvider = storageProvider,
                StoragePath = storagePath,
                StorageFileId = uploadResult.FileId,
                StorageUrl = uploadResult.WebUrl,
                ContainerName = storageProvider == "AzureBlob" ? _blobSettings.ContainerName : null,
                Width = width,
                Height = height,
                IsPrimaryPhoto = isPrimary,
                ThumbnailPath = thumbnailPath,
                ExpiryDate = request.ExpiryDate,
                CertificateNumber = request.CertificateNumber,
                IssuingAuthority = request.IssuingAuthority,
                UploadedDate = DateTime.UtcNow,
                UploadedByUserId = userId
            };

            // If setting as primary, clear any existing primary
            if (isPrimary && request.EquipmentId.HasValue)
            {
                await ClearPrimaryPhotoInternalAsync(request.EquipmentId.Value, companyId);
            }

            _context.EquipmentAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[EquipmentAttachmentService] Uploaded {Type} attachment {Id} for company {CompanyId}",
                request.Type, attachment.Id, companyId);

            // Map to DTO
            var dto = await MapToDto(attachment);
            var downloadUrl = await GetDownloadUrlInternalAsync(attachment);

            return new AttachmentUploadResult
            {
                Success = true,
                Attachment = dto,
                DownloadUrl = downloadUrl,
                ThumbnailUrl = thumbnailPath != null ? await GetThumbnailUrlInternalAsync(attachment) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EquipmentAttachmentService] Failed to upload attachment");
            return new AttachmentUploadResult
            {
                Success = false,
                ErrorMessage = $"Failed to upload file: {ex.Message}"
            };
        }
    }

    public async Task<MultipleAttachmentUploadResult> UploadMultipleAsync(
        IEnumerable<IFormFile> files,
        UploadAttachmentRequest request,
        int companyId,
        int userId)
    {
        var result = new MultipleAttachmentUploadResult();
        var fileList = files.ToList();
        result.TotalCount = fileList.Count;

        var isFirstPhoto = request.Type == AttachmentType.Photo &&
            !await HasPrimaryPhotoAsync(request.EquipmentId ?? 0, companyId);

        for (int i = 0; i < fileList.Count; i++)
        {
            var file = fileList[i];

            // Only set first photo as primary if no existing primary
            var uploadRequest = new UploadAttachmentRequest
            {
                EquipmentId = request.EquipmentId,
                EquipmentKitId = request.EquipmentKitId,
                LocationId = request.LocationId,
                MaintenanceRecordId = request.MaintenanceRecordId,
                Type = request.Type,
                Category = request.Category,
                Title = request.Title,
                Description = request.Description,
                ExpiryDate = request.ExpiryDate,
                CertificateNumber = request.CertificateNumber,
                IssuingAuthority = request.IssuingAuthority,
                SetAsPrimaryPhoto = isFirstPhoto && i == 0
            };

            var uploadResult = await UploadAsync(file, uploadRequest, companyId, userId);

            if (uploadResult.Success && uploadResult.Attachment != null)
            {
                result.SuccessCount++;
                result.UploadedAttachments.Add(uploadResult.Attachment);
            }
            else
            {
                result.FailedCount++;
                result.Errors.Add($"{file.FileName}: {uploadResult.ErrorMessage}");
            }
        }

        result.Success = result.FailedCount == 0;
        return result;
    }

    #endregion

    #region Get Operations

    public async Task<EquipmentAttachmentDto?> GetByIdAsync(int id, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .Include(a => a.Equipment)
            .Include(a => a.EquipmentKit)
            .Include(a => a.Location)
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted);

        return attachment != null ? await MapToDto(attachment) : null;
    }

    public async Task<AttachmentListResponse> GetForEquipmentAsync(
        int equipmentId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.EquipmentAttachments
            .Where(a => a.EquipmentId == equipmentId && a.CompanyId == companyId && !a.IsDeleted);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.IsPrimaryPhoto)
            .ThenByDescending(a => a.UploadedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AttachmentListResponse
        {
            Items = (await Task.WhenAll(items.Select(MapToDto))).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AttachmentListResponse> GetForKitAsync(
        int kitId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.EquipmentAttachments
            .Where(a => a.EquipmentKitId == kitId && a.CompanyId == companyId && !a.IsDeleted);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.UploadedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AttachmentListResponse
        {
            Items = (await Task.WhenAll(items.Select(MapToDto))).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AttachmentListResponse> GetForLocationAsync(
        int locationId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.EquipmentAttachments
            .Where(a => a.LocationId == locationId && a.CompanyId == companyId && !a.IsDeleted);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.UploadedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AttachmentListResponse
        {
            Items = (await Task.WhenAll(items.Select(MapToDto))).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AttachmentListResponse> GetForMaintenanceRecordAsync(
        int maintenanceRecordId,
        int companyId,
        AttachmentType? type = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.EquipmentAttachments
            .Where(a => a.MaintenanceRecordId == maintenanceRecordId && a.CompanyId == companyId && !a.IsDeleted);

        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.UploadedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AttachmentListResponse
        {
            Items = (await Task.WhenAll(items.Select(MapToDto))).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EquipmentAttachmentDto?> GetPrimaryPhotoAsync(int equipmentId, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .Include(a => a.Equipment)
            .FirstOrDefaultAsync(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Photo &&
                a.IsPrimaryPhoto &&
                !a.IsDeleted);

        return attachment != null ? await MapToDto(attachment) : null;
    }

    public async Task<PhotoGalleryDto> GetPhotoGalleryAsync(int equipmentId, int companyId)
    {
        var photos = await _context.EquipmentAttachments
            .Include(a => a.Equipment)
            .Where(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Photo &&
                !a.IsDeleted)
            .OrderByDescending(a => a.IsPrimaryPhoto)
            .ThenByDescending(a => a.UploadedDate)
            .ToListAsync();

        var photoDtos = await Task.WhenAll(photos.Select(MapToDto));

        return new PhotoGalleryDto
        {
            PrimaryPhoto = photoDtos.FirstOrDefault(p => p.IsPrimaryPhoto),
            Photos = photoDtos.ToList(),
            TotalCount = photos.Count
        };
    }

    public async Task<List<EquipmentAttachmentDto>> GetPhotosAsync(int equipmentId, int companyId)
    {
        var photos = await _context.EquipmentAttachments
            .Where(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Photo &&
                !a.IsDeleted)
            .OrderByDescending(a => a.IsPrimaryPhoto)
            .ThenByDescending(a => a.UploadedDate)
            .ToListAsync();

        return (await Task.WhenAll(photos.Select(MapToDto))).ToList();
    }

    public async Task<List<EquipmentAttachmentDto>> GetDocumentsAsync(int equipmentId, int companyId)
    {
        var documents = await _context.EquipmentAttachments
            .Where(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Document &&
                !a.IsDeleted)
            .OrderByDescending(a => a.UploadedDate)
            .ToListAsync();

        return (await Task.WhenAll(documents.Select(MapToDto))).ToList();
    }

    public async Task<List<EquipmentAttachmentDto>> GetCertificatesAsync(int equipmentId, int companyId)
    {
        var certificates = await _context.EquipmentAttachments
            .Where(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Certificate &&
                !a.IsDeleted)
            .OrderBy(a => a.ExpiryDate)
            .ToListAsync();

        return (await Task.WhenAll(certificates.Select(MapToDto))).ToList();
    }

    public async Task<CertificateAttachmentSummaryDto> GetCertificateSummaryAsync(int companyId, int? equipmentId = null)
    {
        var query = _context.EquipmentAttachments
            .Where(a => a.CompanyId == companyId && a.Type == AttachmentType.Certificate && !a.IsDeleted);

        if (equipmentId.HasValue)
            query = query.Where(a => a.EquipmentId == equipmentId.Value);

        var certificates = await query.ToListAsync();
        var now = DateTime.UtcNow;
        var thirtyDaysFromNow = now.AddDays(30);

        var expired = certificates.Where(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < now).ToList();
        var expiringSoon = certificates.Where(c =>
            c.ExpiryDate.HasValue &&
            c.ExpiryDate.Value >= now &&
            c.ExpiryDate.Value <= thirtyDaysFromNow).ToList();
        var valid = certificates.Where(c =>
            !c.ExpiryDate.HasValue || c.ExpiryDate.Value > thirtyDaysFromNow).ToList();

        return new CertificateAttachmentSummaryDto
        {
            TotalCertificates = certificates.Count,
            ValidCertificates = valid.Count,
            ExpiredCertificateCount = expired.Count,
            ExpiringSoonCertificates = expiringSoon.Count,
            ExpiringCertificatesList = (await Task.WhenAll(expiringSoon.Select(MapToDto))).ToList(),
            ExpiredCertificatesList = (await Task.WhenAll(expired.Select(MapToDto))).ToList()
        };
    }

    #endregion

    #region Photo Operations

    public async Task<bool> SetPrimaryPhotoAsync(int attachmentId, int equipmentId, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .FirstOrDefaultAsync(a =>
                a.Id == attachmentId &&
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Photo &&
                !a.IsDeleted);

        if (attachment == null)
            return false;

        // Clear existing primary
        await ClearPrimaryPhotoInternalAsync(equipmentId, companyId);

        // Set new primary
        attachment.IsPrimaryPhoto = true;
        attachment.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[EquipmentAttachmentService] Set attachment {AttachmentId} as primary photo for equipment {EquipmentId}",
            attachmentId, equipmentId);

        return true;
    }

    public async Task<bool> ClearPrimaryPhotoAsync(int equipmentId, int companyId)
    {
        return await ClearPrimaryPhotoInternalAsync(equipmentId, companyId);
    }

    private async Task<bool> ClearPrimaryPhotoInternalAsync(int equipmentId, int companyId)
    {
        var existingPrimary = await _context.EquipmentAttachments
            .Where(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Photo &&
                a.IsPrimaryPhoto &&
                !a.IsDeleted)
            .ToListAsync();

        foreach (var photo in existingPrimary)
        {
            photo.IsPrimaryPhoto = false;
        }

        if (existingPrimary.Any())
            await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> HasPrimaryPhotoAsync(int equipmentId, int companyId)
    {
        if (equipmentId == 0) return false;

        return await _context.EquipmentAttachments
            .AnyAsync(a =>
                a.EquipmentId == equipmentId &&
                a.CompanyId == companyId &&
                a.Type == AttachmentType.Photo &&
                a.IsPrimaryPhoto &&
                !a.IsDeleted);
    }

    #endregion

    #region Download Operations

    public async Task<Stream?> DownloadAsync(int id, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted);

        if (attachment == null)
            return null;

        if (attachment.StorageProvider == "AzureBlob")
        {
            return await _blobStorage.DownloadFileAsync(attachment.StorageFileId!);
        }
        else
        {
            var sharePointProvider = _cloudStorageFactory.GetProvider("SharePoint");
            return await sharePointProvider.DownloadFileAsync(attachment.StorageFileId!);
        }
    }

    public async Task<AttachmentDownloadResponse?> GetDownloadUrlAsync(int id, int companyId, TimeSpan? expiration = null)
    {
        var attachment = await _context.EquipmentAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted);

        if (attachment == null)
            return null;

        var downloadUrl = await GetDownloadUrlInternalAsync(attachment, expiration);

        return new AttachmentDownloadResponse
        {
            DownloadUrl = downloadUrl,
            ExpiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(_blobSettings.SasTokenExpirationMinutes)),
            FileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes
        };
    }

    public async Task<string?> GetThumbnailUrlAsync(int id, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted);

        if (attachment == null || string.IsNullOrEmpty(attachment.ThumbnailPath))
            return null;

        return await GetThumbnailUrlInternalAsync(attachment);
    }

    private async Task<string> GetDownloadUrlInternalAsync(EquipmentAttachment attachment, TimeSpan? expiration = null)
    {
        if (attachment.StorageProvider == "AzureBlob")
        {
            return await _blobStorage.GetPresignedDownloadUrlAsync(
                attachment.StorageFileId!,
                expiration);
        }
        else
        {
            var sharePointProvider = _cloudStorageFactory.GetProvider("SharePoint");
            return await sharePointProvider.GetFileWebUrlAsync(attachment.StorageFileId!);
        }
    }

    private async Task<string?> GetThumbnailUrlInternalAsync(EquipmentAttachment attachment)
    {
        if (string.IsNullOrEmpty(attachment.ThumbnailPath))
            return null;

        if (attachment.StorageProvider == "AzureBlob")
        {
            return await _blobStorage.GetPresignedDownloadUrlAsync(attachment.ThumbnailPath);
        }

        return null;
    }

    #endregion

    #region Update/Delete Operations

    public async Task<EquipmentAttachmentDto?> UpdateMetadataAsync(
        int id,
        UpdateAttachmentRequest request,
        int companyId,
        int userId)
    {
        var attachment = await _context.EquipmentAttachments
            .Include(a => a.Equipment)
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted);

        if (attachment == null)
            return null;

        // Update metadata
        if (request.Title != null)
            attachment.Title = request.Title;

        if (request.Description != null)
            attachment.Description = request.Description;

        if (request.Category.HasValue)
            attachment.Category = request.Category.Value;

        if (request.ExpiryDate.HasValue)
            attachment.ExpiryDate = request.ExpiryDate;

        if (request.CertificateNumber != null)
            attachment.CertificateNumber = request.CertificateNumber;

        if (request.IssuingAuthority != null)
            attachment.IssuingAuthority = request.IssuingAuthority;

        attachment.LastModified = DateTime.UtcNow;
        attachment.LastModifiedByUserId = userId;

        await _context.SaveChangesAsync();

        return await MapToDto(attachment);
    }

    public async Task<bool> DeleteAsync(int id, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId && !a.IsDeleted);

        if (attachment == null)
            return false;

        // Soft delete
        attachment.IsDeleted = true;
        attachment.LastModified = DateTime.UtcNow;

        // If this was the primary photo, we need to clear that flag
        // (The next photo won't automatically become primary - user must set it)

        await _context.SaveChangesAsync();

        _logger.LogInformation("[EquipmentAttachmentService] Soft deleted attachment {Id}", id);
        return true;
    }

    public async Task<bool> PermanentDeleteAsync(int id, int companyId)
    {
        var attachment = await _context.EquipmentAttachments
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId);

        if (attachment == null)
            return false;

        try
        {
            // Delete from storage
            if (attachment.StorageProvider == "AzureBlob")
            {
                await _blobStorage.DeleteFileAsync(attachment.StorageFileId!);

                // Also delete thumbnail if exists
                if (!string.IsNullOrEmpty(attachment.ThumbnailPath))
                {
                    await _blobStorage.DeleteFileAsync(attachment.ThumbnailPath);
                }
            }
            else
            {
                var sharePointProvider = _cloudStorageFactory.GetProvider("SharePoint");
                await sharePointProvider.DeleteFileAsync(attachment.StorageFileId!);
            }

            // Remove from database
            _context.EquipmentAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[EquipmentAttachmentService] Permanently deleted attachment {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EquipmentAttachmentService] Failed to permanently delete attachment {Id}", id);
            return false;
        }
    }

    public async Task<int> DeleteAllForEquipmentAsync(int equipmentId, int companyId)
    {
        var attachments = await _context.EquipmentAttachments
            .Where(a => a.EquipmentId == equipmentId && a.CompanyId == companyId && !a.IsDeleted)
            .ToListAsync();

        foreach (var attachment in attachments)
        {
            attachment.IsDeleted = true;
            attachment.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return attachments.Count;
    }

    #endregion

    #region Validation

    public Task<(bool IsValid, string? ErrorMessage)> ValidateFileAsync(IFormFile file, AttachmentType type)
    {
        // Check file size
        if (!IsFileSizeAllowed(file.Length))
        {
            return Task.FromResult((false, $"File size exceeds maximum allowed size of {_blobSettings.MaxFileSizeMB}MB"));
        }

        // Check extension
        if (!IsAllowedExtension(file.FileName, type))
        {
            var allowedExtensions = type == AttachmentType.Photo
                ? _blobSettings.AllowedPhotoExtensions
                : _blobSettings.AllowedDocumentExtensions;
            return Task.FromResult((false, $"File extension not allowed. Allowed extensions: {allowedExtensions}"));
        }

        return Task.FromResult<(bool, string?)>((true, null));
    }

    public bool IsAllowedExtension(string fileName, AttachmentType type)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
            return false;

        return type == AttachmentType.Photo
            ? PhotoExtensions.Contains(extension)
            : DocumentExtensions.Contains(extension);
    }

    public bool IsFileSizeAllowed(long fileSizeBytes)
    {
        var maxBytes = _blobSettings.MaxFileSizeMB * 1024 * 1024;
        return fileSizeBytes <= maxBytes;
    }

    #endregion

    #region Private Helpers

    private string BuildStoragePath(int companyId, UploadAttachmentRequest request)
    {
        var basePath = $"{companyId}";

        if (request.EquipmentId.HasValue)
            basePath += $"/equipment/{request.EquipmentId}";
        else if (request.EquipmentKitId.HasValue)
            basePath += $"/kits/{request.EquipmentKitId}";
        else if (request.LocationId.HasValue)
            basePath += $"/locations/{request.LocationId}";
        else if (request.MaintenanceRecordId.HasValue)
            basePath += $"/maintenance/{request.MaintenanceRecordId}";

        // Add type folder
        basePath += request.Type switch
        {
            AttachmentType.Photo => "/photos",
            AttachmentType.Document => "/documents",
            AttachmentType.Certificate => "/certificates",
            _ => "/other"
        };

        return basePath;
    }

    private async Task<string?> GenerateThumbnailAsync(
        IFormFile file,
        string storagePath,
        string originalFileName,
        int companyId)
    {
        try
        {
            using var imageStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(imageStream);

            // Calculate thumbnail dimensions maintaining aspect ratio
            var ratio = Math.Min(
                (double)_blobSettings.ThumbnailMaxWidth / image.Width,
                (double)_blobSettings.ThumbnailMaxHeight / image.Height);

            if (ratio >= 1) // Image is already small enough
                return null;

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(newWidth, newHeight));

            // Generate thumbnail filename
            var extension = Path.GetExtension(originalFileName);
            var thumbnailFileName = Path.GetFileNameWithoutExtension(originalFileName) + "-thumb" + extension;
            var thumbnailPath = $"{storagePath}/{thumbnailFileName}";

            // Save to memory stream
            using var thumbnailStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Position = 0;

            // Upload thumbnail
            var uploadRequest = new CloudFileUploadRequest
            {
                FolderPath = storagePath,
                FileName = thumbnailFileName,
                Content = thumbnailStream,
                ContentType = "image/jpeg",
                Metadata = new Dictionary<string, string>
                {
                    ["IsThumbnail"] = "true",
                    ["OriginalFile"] = originalFileName
                }
            };

            var result = await _blobStorage.UploadFileAsync(uploadRequest);
            return result.FileId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[EquipmentAttachmentService] Failed to generate thumbnail for {FileName}", file.FileName);
            return null;
        }
    }

    private async Task<EquipmentAttachmentDto> MapToDto(EquipmentAttachment attachment)
    {
        var dto = new EquipmentAttachmentDto
        {
            Id = attachment.Id,
            CompanyId = attachment.CompanyId,
            EquipmentId = attachment.EquipmentId,
            EquipmentCode = attachment.Equipment?.EquipmentCode,
            EquipmentName = attachment.Equipment?.Name,
            EquipmentKitId = attachment.EquipmentKitId,
            KitCode = attachment.EquipmentKit?.KitCode,
            KitName = attachment.EquipmentKit?.Name,
            LocationId = attachment.LocationId,
            LocationName = attachment.Location?.Name,
            MaintenanceRecordId = attachment.MaintenanceRecordId,
            Type = attachment.Type,
            Category = attachment.Category,
            FileName = attachment.FileName,
            OriginalFileName = attachment.OriginalFileName,
            Title = attachment.Title,
            Description = attachment.Description,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            StorageProvider = attachment.StorageProvider,
            StorageUrl = attachment.StorageUrl,
            Width = attachment.Width,
            Height = attachment.Height,
            IsPrimaryPhoto = attachment.IsPrimaryPhoto,
            ExpiryDate = attachment.ExpiryDate,
            CertificateNumber = attachment.CertificateNumber,
            IssuingAuthority = attachment.IssuingAuthority,
            UploadedDate = attachment.UploadedDate,
            UploadedBy = attachment.UploadedBy,
            LastModified = attachment.LastModified
        };

        // Get URLs if needed
        try
        {
            dto.StorageUrl = await GetDownloadUrlInternalAsync(attachment);
            dto.ThumbnailUrl = await GetThumbnailUrlInternalAsync(attachment);
        }
        catch
        {
            // Ignore URL generation errors in mapping
        }

        return dto;
    }

    #endregion
}
