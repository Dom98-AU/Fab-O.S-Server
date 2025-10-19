using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.CloudStorage;

/// <summary>
/// Google Drive implementation of ICloudStorageProvider (STUB - Not yet implemented)
/// Future implementation will use Google Drive API for file operations
/// </summary>
public class GoogleDriveStorageProvider : ICloudStorageProvider
{
    private readonly ILogger<GoogleDriveStorageProvider> _logger;

    public GoogleDriveStorageProvider(ILogger<GoogleDriveStorageProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "GoogleDrive";

    public Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<Stream> DownloadFileAsync(string fileId)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<bool> DeleteFileAsync(string fileId)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudFileMetadata> GetFileMetadataAsync(string fileId)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<string> GetFileWebUrlAsync(string fileId)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }

    public Task<string> GetPresignedUploadUrlAsync(string folderPath, string fileName, TimeSpan expiration)
    {
        _logger.LogWarning("[GoogleDriveStorageProvider] Google Drive integration not yet implemented");
        throw new NotImplementedException("Google Drive storage provider is not yet implemented. Coming soon!");
    }
}
