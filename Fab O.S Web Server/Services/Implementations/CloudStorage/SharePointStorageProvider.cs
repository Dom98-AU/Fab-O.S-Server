using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.CloudStorage;

/// <summary>
/// SharePoint implementation of ICloudStorageProvider
/// Wraps the existing SharePointService to use configured SharePoint credentials
/// </summary>
public class SharePointStorageProvider : ICloudStorageProvider
{
    private readonly ISharePointService _sharePointService;
    private readonly ILogger<SharePointStorageProvider> _logger;

    public SharePointStorageProvider(
        ISharePointService sharePointService,
        ILogger<SharePointStorageProvider> logger)
    {
        _sharePointService = sharePointService;
        _logger = logger;
    }

    public string ProviderName => "SharePoint";

    public async Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request)
    {
        try
        {
            _logger.LogInformation(
                "[SharePointStorageProvider] Uploading file {FileName} to {FolderPath}",
                request.FileName,
                request.FolderPath
            );

            // Use existing SharePointService to upload file
            var files = await _sharePointService.UploadMultipleFilesAsync(
                request.FolderPath,
                new List<(Stream stream, string fileName, string contentType)>
                {
                    (request.Content, request.FileName, request.ContentType)
                }
            );

            if (files == null || !files.Any())
            {
                throw new InvalidOperationException("SharePoint file upload returned no results");
            }

            var file = files.First();

            var result = new CloudFileUploadResult
            {
                FileId = file.Id,
                WebUrl = file.WebUrl,
                Size = file.Size,
                ETag = file.ETag,
                ProviderMetadata = new Dictionary<string, string>
                {
                    { "SharePointItemId", file.Id },
                    { "SharePointUrl", file.WebUrl },
                    { "SharePointETag", file.ETag ?? "" }
                }
            };

            _logger.LogInformation(
                "[SharePointStorageProvider] Successfully uploaded file {FileName}, FileId: {FileId}",
                request.FileName,
                result.FileId
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SharePointStorageProvider] Error uploading file {FileName} to {FolderPath}",
                request.FileName,
                request.FolderPath
            );
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        try
        {
            _logger.LogInformation(
                "[SharePointStorageProvider] Downloading file {FileId}",
                fileId
            );

            var stream = await _sharePointService.DownloadFileAsync(fileId);

            _logger.LogInformation(
                "[SharePointStorageProvider] Successfully downloaded file {FileId}",
                fileId
            );

            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SharePointStorageProvider] Error downloading file {FileId}",
                fileId
            );
            throw;
        }
    }

    public async Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType)
    {
        try
        {
            _logger.LogInformation(
                "[SharePointStorageProvider] Updating file {FileId} (ContentType: {ContentType})",
                fileId,
                contentType
            );

            // Use existing SharePointService to update file
            var updatedFile = await _sharePointService.UpdateFileAsync(fileId, content, contentType);

            var result = new CloudFileUploadResult
            {
                FileId = updatedFile.Id,
                WebUrl = updatedFile.WebUrl,
                Size = updatedFile.Size,
                ETag = updatedFile.ETag,
                ProviderMetadata = new Dictionary<string, string>
                {
                    { "SharePointItemId", updatedFile.Id },
                    { "SharePointUrl", updatedFile.WebUrl },
                    { "SharePointETag", updatedFile.ETag ?? "" }
                }
            };

            _logger.LogInformation(
                "[SharePointStorageProvider] Successfully updated file {FileId}, Size: {Size} bytes",
                result.FileId,
                result.Size
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SharePointStorageProvider] Error updating file {FileId}",
                fileId
            );
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            _logger.LogInformation(
                "[SharePointStorageProvider] Deleting file {FileId}",
                fileId
            );

            await _sharePointService.DeleteFileAsync(fileId);

            _logger.LogInformation(
                "[SharePointStorageProvider] Successfully deleted file {FileId}",
                fileId
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SharePointStorageProvider] Error deleting file {FileId}",
                fileId
            );
            return false;
        }
    }

    public Task<CloudFileMetadata> GetFileMetadataAsync(string fileId)
    {
        // TODO: Implement when ISharePointService adds GetFileMetadataAsync method
        _logger.LogWarning(
            "[SharePointStorageProvider] GetFileMetadataAsync not yet implemented in ISharePointService"
        );
        throw new NotImplementedException(
            "File metadata retrieval is not yet available in the SharePoint provider. " +
            "This will be implemented in a future version."
        );
    }

    public async Task<string> GetFileWebUrlAsync(string fileId)
    {
        try
        {
            _logger.LogInformation(
                "[SharePointStorageProvider] Getting web URL for file {FileId}",
                fileId
            );

            var url = await _sharePointService.GetFileWebUrlAsync(fileId);

            _logger.LogInformation(
                "[SharePointStorageProvider] Successfully retrieved web URL for file {FileId}",
                fileId
            );

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SharePointStorageProvider] Error getting web URL for file {FileId}",
                fileId
            );
            throw;
        }
    }

    public Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request)
    {
        // TODO: Implement when ISharePointService adds CreateUploadSessionAsync method
        _logger.LogWarning(
            "[SharePointStorageProvider] CreateUploadSessionAsync not yet implemented in ISharePointService"
        );
        throw new NotImplementedException(
            "Upload session functionality is not yet available in the SharePoint provider. " +
            "This will be implemented in a future version for large file uploads."
        );
    }

    public Task<string> GetPresignedUploadUrlAsync(string folderPath, string fileName, TimeSpan expiration)
    {
        // SharePoint doesn't support presigned URLs like S3
        // For future cloud direct upload, we would use upload sessions instead
        _logger.LogWarning(
            "[SharePointStorageProvider] Presigned upload URLs not supported for SharePoint. " +
            "Use CreateUploadSessionAsync for large file uploads instead."
        );

        throw new NotSupportedException(
            "SharePoint does not support presigned upload URLs. " +
            "Use CreateUploadSessionAsync for large file uploads."
        );
    }
}
