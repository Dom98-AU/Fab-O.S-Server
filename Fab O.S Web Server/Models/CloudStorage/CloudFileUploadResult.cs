namespace FabOS.WebServer.Models.CloudStorage;

/// <summary>
/// Result model returned after uploading a file to cloud storage
/// </summary>
public class CloudFileUploadResult
{
    /// <summary>
    /// Provider-specific file identifier (e.g., SharePoint item ID, Google Drive file ID)
    /// </summary>
    public required string FileId { get; set; }

    /// <summary>
    /// Web URL for viewing/previewing the file
    /// </summary>
    public required string WebUrl { get; set; }

    /// <summary>
    /// Size of the uploaded file in bytes
    /// </summary>
    public required long Size { get; set; }

    /// <summary>
    /// ETag or version identifier for the file
    /// Used for optimistic concurrency control
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Provider-specific metadata returned after upload
    /// Example: SharePoint item ID, Google Drive permissions, etc.
    /// </summary>
    public Dictionary<string, string>? ProviderMetadata { get; set; }
}
