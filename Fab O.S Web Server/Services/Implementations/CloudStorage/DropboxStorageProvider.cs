using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.CloudStorage;

/// <summary>
/// Dropbox implementation of ICloudStorageProvider (STUB - Not yet implemented)
/// Future implementation will use Dropbox API for file operations
/// </summary>
public class DropboxStorageProvider : ICloudStorageProvider
{
    private readonly ILogger<DropboxStorageProvider> _logger;

    public DropboxStorageProvider(ILogger<DropboxStorageProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "Dropbox";

    public Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<Stream> DownloadFileAsync(string fileId)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<bool> DeleteFileAsync(string fileId)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudFileMetadata> GetFileMetadataAsync(string fileId)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<string> GetFileWebUrlAsync(string fileId)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }

    public Task<string> GetPresignedUploadUrlAsync(string folderPath, string fileName, TimeSpan expiration)
    {
        _logger.LogWarning("[DropboxStorageProvider] Dropbox integration not yet implemented");
        throw new NotImplementedException("Dropbox storage provider is not yet implemented. Coming soon!");
    }
}
