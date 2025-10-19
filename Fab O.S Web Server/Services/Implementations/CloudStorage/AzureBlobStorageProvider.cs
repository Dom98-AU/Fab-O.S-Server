using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.CloudStorage;

/// <summary>
/// Azure Blob Storage implementation of ICloudStorageProvider (STUB - Not yet implemented)
/// Future implementation will use Azure Blob Storage SDK for file operations
/// </summary>
public class AzureBlobStorageProvider : ICloudStorageProvider
{
    private readonly ILogger<AzureBlobStorageProvider> _logger;

    public AzureBlobStorageProvider(ILogger<AzureBlobStorageProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "AzureBlob";

    public Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<Stream> DownloadFileAsync(string fileId)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<bool> DeleteFileAsync(string fileId)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudFileMetadata> GetFileMetadataAsync(string fileId)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<string> GetFileWebUrlAsync(string fileId)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }

    public Task<string> GetPresignedUploadUrlAsync(string folderPath, string fileName, TimeSpan expiration)
    {
        _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage integration not yet implemented");
        throw new NotImplementedException("Azure Blob Storage provider is not yet implemented. Coming soon!");
    }
}
