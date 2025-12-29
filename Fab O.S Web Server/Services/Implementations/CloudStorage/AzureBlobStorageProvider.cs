using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FabOS.WebServer.Services.Implementations.CloudStorage;

/// <summary>
/// Azure Blob Storage configuration settings
/// </summary>
public class AzureBlobStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "fabos-assets";
    public int MaxFileSizeMB { get; set; } = 100;
    public string AllowedPhotoExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.webp,.heic";
    public string AllowedDocumentExtensions { get; set; } = ".pdf,.doc,.docx,.xls,.xlsx,.txt,.csv";
    public bool GenerateThumbnails { get; set; } = true;
    public int ThumbnailMaxWidth { get; set; } = 300;
    public int ThumbnailMaxHeight { get; set; } = 300;
    public int SasTokenExpirationMinutes { get; set; } = 15;
}

/// <summary>
/// Azure Blob Storage implementation of ICloudStorageProvider
/// Used for storing asset photos with SAS token-based secure access
/// </summary>
public class AzureBlobStorageProvider : ICloudStorageProvider
{
    private readonly ILogger<AzureBlobStorageProvider> _logger;
    private readonly AzureBlobStorageSettings _settings;
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly BlobContainerClient? _containerClient;

    public AzureBlobStorageProvider(
        ILogger<AzureBlobStorageProvider> logger,
        IOptions<AzureBlobStorageSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        // Initialize Azure Blob clients if connection string is configured
        if (!string.IsNullOrEmpty(_settings.ConnectionString) &&
            !_settings.ConnectionString.Contains("Set in"))
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
                _containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
                _logger.LogInformation("[AzureBlobStorageProvider] Initialized with container: {Container}", _settings.ContainerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AzureBlobStorageProvider] Failed to initialize Azure Blob Storage client");
            }
        }
        else
        {
            _logger.LogWarning("[AzureBlobStorageProvider] Azure Blob Storage connection string not configured");
        }
    }

    public string ProviderName => "AzureBlob";

    /// <summary>
    /// Upload a file to Azure Blob Storage
    /// </summary>
    public async Task<CloudFileUploadResult> UploadFileAsync(CloudFileUploadRequest request)
    {
        EnsureInitialized();

        // Build the blob path: FolderPath/FileName
        var blobPath = string.IsNullOrEmpty(request.FolderPath)
            ? request.FileName
            : $"{request.FolderPath.TrimEnd('/')}/{request.FileName}";

        _logger.LogInformation("[AzureBlobStorageProvider] Uploading file to: {BlobPath}", blobPath);

        try
        {
            // Ensure container exists
            await _containerClient!.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobClient = _containerClient.GetBlobClient(blobPath);

            // Set blob metadata
            var blobHeaders = new BlobHttpHeaders
            {
                ContentType = request.ContentType
            };

            var metadata = request.Metadata ?? new Dictionary<string, string>();
            metadata["OriginalFileName"] = request.FileName;
            metadata["UploadedAt"] = DateTime.UtcNow.ToString("O");

            // Upload the blob
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHeaders,
                Metadata = metadata
            };

            var response = await blobClient.UploadAsync(request.Content, uploadOptions);

            // Get blob properties for size info
            var properties = await blobClient.GetPropertiesAsync();

            var result = new CloudFileUploadResult
            {
                FileId = blobPath, // Use blob path as the file ID
                WebUrl = blobClient.Uri.ToString(),
                Size = properties.Value.ContentLength,
                ETag = properties.Value.ETag.ToString(),
                ProviderMetadata = new Dictionary<string, string>
                {
                    ["BlobPath"] = blobPath,
                    ["ContainerName"] = _settings.ContainerName,
                    ["ContentMd5"] = response.Value.ContentHash != null
                        ? Convert.ToBase64String(response.Value.ContentHash)
                        : string.Empty
                }
            };

            _logger.LogInformation("[AzureBlobStorageProvider] Successfully uploaded: {BlobPath} ({Size} bytes)",
                blobPath, result.Size);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Failed to upload file: {BlobPath}", blobPath);
            throw;
        }
    }

    /// <summary>
    /// Download a file from Azure Blob Storage
    /// </summary>
    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        EnsureInitialized();

        _logger.LogInformation("[AzureBlobStorageProvider] Downloading file: {FileId}", fileId);

        try
        {
            var blobClient = _containerClient!.GetBlobClient(fileId);
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Failed to download file: {FileId}", fileId);
            throw;
        }
    }

    /// <summary>
    /// Update/replace an existing file in Azure Blob Storage
    /// </summary>
    public async Task<CloudFileUploadResult> UpdateFileAsync(string fileId, Stream content, string contentType)
    {
        EnsureInitialized();

        _logger.LogInformation("[AzureBlobStorageProvider] Updating file: {FileId}", fileId);

        try
        {
            var blobClient = _containerClient!.GetBlobClient(fileId);

            var blobHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHeaders,
                Metadata = new Dictionary<string, string>
                {
                    ["UpdatedAt"] = DateTime.UtcNow.ToString("O")
                }
            };

            var response = await blobClient.UploadAsync(content, uploadOptions);
            var properties = await blobClient.GetPropertiesAsync();

            return new CloudFileUploadResult
            {
                FileId = fileId,
                WebUrl = blobClient.Uri.ToString(),
                Size = properties.Value.ContentLength,
                ETag = properties.Value.ETag.ToString(),
                ProviderMetadata = new Dictionary<string, string>
                {
                    ["BlobPath"] = fileId,
                    ["ContainerName"] = _settings.ContainerName
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Failed to update file: {FileId}", fileId);
            throw;
        }
    }

    /// <summary>
    /// Delete a file from Azure Blob Storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileId)
    {
        EnsureInitialized();

        _logger.LogInformation("[AzureBlobStorageProvider] Deleting file: {FileId}", fileId);

        try
        {
            var blobClient = _containerClient!.GetBlobClient(fileId);
            var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

            _logger.LogInformation("[AzureBlobStorageProvider] Delete result for {FileId}: {Deleted}", fileId, response.Value);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Failed to delete file: {FileId}", fileId);
            return false;
        }
    }

    /// <summary>
    /// Get file metadata without downloading the file
    /// </summary>
    public async Task<CloudFileMetadata> GetFileMetadataAsync(string fileId)
    {
        EnsureInitialized();

        _logger.LogInformation("[AzureBlobStorageProvider] Getting metadata for: {FileId}", fileId);

        try
        {
            var blobClient = _containerClient!.GetBlobClient(fileId);
            var properties = await blobClient.GetPropertiesAsync();

            return new CloudFileMetadata
            {
                FileId = fileId,
                Name = Path.GetFileName(fileId),
                Size = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                CreatedDate = properties.Value.CreatedOn.DateTime,
                ModifiedDate = properties.Value.LastModified.DateTime,
                WebUrl = blobClient.Uri.ToString(),
                ETag = properties.Value.ETag.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Failed to get metadata for: {FileId}", fileId);
            throw;
        }
    }

    /// <summary>
    /// Get the direct URL for a blob (no SAS token - use for internal operations)
    /// </summary>
    public Task<string> GetFileWebUrlAsync(string fileId)
    {
        EnsureInitialized();

        var blobClient = _containerClient!.GetBlobClient(fileId);
        return Task.FromResult(blobClient.Uri.ToString());
    }

    /// <summary>
    /// Get a presigned download URL with SAS token for secure, time-limited access
    /// </summary>
    public Task<string> GetPresignedDownloadUrlAsync(string fileId, TimeSpan? expiration = null)
    {
        EnsureInitialized();

        var expirationTime = expiration ?? TimeSpan.FromMinutes(_settings.SasTokenExpirationMinutes);

        var blobClient = _containerClient!.GetBlobClient(fileId);

        // Create SAS token for read-only access
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _settings.ContainerName,
            BlobName = fileId,
            Resource = "b", // b = blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Start 5 minutes ago to handle clock skew
            ExpiresOn = DateTimeOffset.UtcNow.Add(expirationTime)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogInformation("[AzureBlobStorageProvider] Generated SAS URL for: {FileId} (expires in {Minutes} minutes)",
            fileId, expirationTime.TotalMinutes);

        return Task.FromResult(sasUri.ToString());
    }

    /// <summary>
    /// Create upload session for large files (not typically needed for photos, but implemented for interface compatibility)
    /// </summary>
    public Task<CloudUploadSession> CreateUploadSessionAsync(CloudFileUploadRequest request)
    {
        EnsureInitialized();

        // Azure Blob Storage doesn't require explicit upload sessions for most use cases
        // The standard upload handles large files automatically
        // However, we provide presigned URL functionality for direct client uploads

        var blobPath = string.IsNullOrEmpty(request.FolderPath)
            ? request.FileName
            : $"{request.FolderPath.TrimEnd('/')}/{request.FileName}";

        var blobClient = _containerClient!.GetBlobClient(blobPath);

        // Create SAS token for upload
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _settings.ContainerName,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // 1 hour for upload session
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        return Task.FromResult(new CloudUploadSession
        {
            SessionId = Guid.NewGuid().ToString(),
            UploadUrl = sasUri.ToString(),
            ExpirationDate = DateTime.UtcNow.AddHours(1),
            ChunkSize = 4 * 1024 * 1024 // 4MB chunks recommended
        });
    }

    /// <summary>
    /// Get presigned URL for client-side direct upload
    /// </summary>
    public Task<string> GetPresignedUploadUrlAsync(string folderPath, string fileName, TimeSpan expiration)
    {
        EnsureInitialized();

        var blobPath = string.IsNullOrEmpty(folderPath)
            ? fileName
            : $"{folderPath.TrimEnd('/')}/{fileName}";

        var blobClient = _containerClient!.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _settings.ContainerName,
            BlobName = blobPath,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiration)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogInformation("[AzureBlobStorageProvider] Generated presigned upload URL for: {BlobPath}", blobPath);

        return Task.FromResult(sasUri.ToString());
    }

    /// <summary>
    /// Check if the blob storage provider is properly configured and ready
    /// </summary>
    public bool IsConfigured => _containerClient != null;

    /// <summary>
    /// Ensure the provider is initialized before operations
    /// </summary>
    private void EnsureInitialized()
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException(
                "Azure Blob Storage is not configured. Please set the ConnectionString in appsettings.json under AzureBlobStorage section.");
        }
    }

    /// <summary>
    /// Check if a file exists in blob storage
    /// </summary>
    public async Task<bool> FileExistsAsync(string fileId)
    {
        EnsureInitialized();

        try
        {
            var blobClient = _containerClient!.GetBlobClient(fileId);
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Error checking if file exists: {FileId}", fileId);
            return false;
        }
    }

    /// <summary>
    /// List all blobs in a folder path
    /// </summary>
    public async Task<List<CloudFileMetadata>> ListFilesAsync(string folderPath)
    {
        EnsureInitialized();

        var results = new List<CloudFileMetadata>();
        var prefix = string.IsNullOrEmpty(folderPath) ? "" : folderPath.TrimEnd('/') + "/";

        try
        {
            await foreach (var blobItem in _containerClient!.GetBlobsAsync(prefix: prefix))
            {
                results.Add(new CloudFileMetadata
                {
                    FileId = blobItem.Name,
                    Name = Path.GetFileName(blobItem.Name),
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = blobItem.Properties.ContentType,
                    CreatedDate = blobItem.Properties.CreatedOn?.DateTime ?? DateTime.MinValue,
                    ModifiedDate = blobItem.Properties.LastModified?.DateTime ?? DateTime.MinValue,
                    ETag = blobItem.Properties.ETag?.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Error listing files in: {FolderPath}", folderPath);
        }

        return results;
    }

    /// <summary>
    /// Copy a blob from one path to another
    /// </summary>
    public async Task<CloudFileUploadResult> CopyFileAsync(string sourceFileId, string destinationPath)
    {
        EnsureInitialized();

        try
        {
            var sourceBlob = _containerClient!.GetBlobClient(sourceFileId);
            var destBlob = _containerClient.GetBlobClient(destinationPath);

            var copyOperation = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);
            await copyOperation.WaitForCompletionAsync();

            var properties = await destBlob.GetPropertiesAsync();

            return new CloudFileUploadResult
            {
                FileId = destinationPath,
                WebUrl = destBlob.Uri.ToString(),
                Size = properties.Value.ContentLength,
                ETag = properties.Value.ETag.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AzureBlobStorageProvider] Error copying file from {Source} to {Dest}",
                sourceFileId, destinationPath);
            throw;
        }
    }
}
