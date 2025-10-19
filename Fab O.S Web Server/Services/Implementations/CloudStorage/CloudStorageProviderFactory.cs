using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.CloudStorage;

/// <summary>
/// Factory for creating cloud storage provider instances based on provider name
/// Enables switching between SharePoint, Google Drive, Dropbox, Azure Blob, etc.
/// </summary>
public class CloudStorageProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CloudStorageProviderFactory> _logger;

    public CloudStorageProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<CloudStorageProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get cloud storage provider by name
    /// </summary>
    /// <param name="providerName">Provider name: "SharePoint", "GoogleDrive", "Dropbox", "AzureBlob"</param>
    /// <returns>Cloud storage provider instance</returns>
    /// <exception cref="NotSupportedException">Thrown if provider is not supported</exception>
    public ICloudStorageProvider GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            _logger.LogWarning(
                "[CloudStorageProviderFactory] Provider name is null or empty, defaulting to SharePoint"
            );
            providerName = "SharePoint";
        }

        _logger.LogInformation(
            "[CloudStorageProviderFactory] Getting provider for: {ProviderName}",
            providerName
        );

        return providerName.ToLower() switch
        {
            "sharepoint" => _serviceProvider.GetRequiredService<SharePointStorageProvider>(),
            "googledrive" => _serviceProvider.GetRequiredService<GoogleDriveStorageProvider>(),
            "dropbox" => _serviceProvider.GetRequiredService<DropboxStorageProvider>(),
            "azureblob" => _serviceProvider.GetRequiredService<AzureBlobStorageProvider>(),
            _ => throw new NotSupportedException(
                $"Storage provider '{providerName}' is not supported. " +
                $"Supported providers: SharePoint, GoogleDrive, Dropbox, AzureBlob"
            )
        };
    }

    /// <summary>
    /// Get cloud storage provider for a specific PackageDrawing
    /// Handles backward compatibility with legacy records
    /// </summary>
    /// <param name="drawing">Package drawing entity</param>
    /// <returns>Cloud storage provider instance</returns>
    public ICloudStorageProvider GetProviderForDrawing(PackageDrawing drawing)
    {
        if (drawing == null)
        {
            throw new ArgumentNullException(nameof(drawing));
        }

        // Backward compatibility: if StorageProvider is null, default to SharePoint
        var providerName = drawing.StorageProvider ?? "SharePoint";

        _logger.LogInformation(
            "[CloudStorageProviderFactory] Getting provider for drawing {DrawingId}: {ProviderName}",
            drawing.Id,
            providerName
        );

        return GetProvider(providerName);
    }

    /// <summary>
    /// Get default cloud storage provider based on configuration
    /// </summary>
    /// <returns>Default cloud storage provider (SharePoint)</returns>
    public ICloudStorageProvider GetDefaultProvider()
    {
        _logger.LogInformation(
            "[CloudStorageProviderFactory] Getting default provider (SharePoint)"
        );

        // For now, SharePoint is the default
        // Future: read from configuration
        return GetProvider("SharePoint");
    }

    /// <summary>
    /// Check if a provider is supported
    /// </summary>
    /// <param name="providerName">Provider name to check</param>
    /// <returns>True if supported, false otherwise</returns>
    public bool IsProviderSupported(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return false;
        }

        return providerName.ToLower() switch
        {
            "sharepoint" => true,
            "googledrive" => true,
            "dropbox" => true,
            "azureblob" => true,
            _ => false
        };
    }

    /// <summary>
    /// Get list of all supported provider names
    /// </summary>
    /// <returns>Array of supported provider names</returns>
    public string[] GetSupportedProviders()
    {
        return new[] { "SharePoint", "GoogleDrive", "Dropbox", "AzureBlob" };
    }
}
