namespace FabOS.WebServer.Models.CloudStorage;

/// <summary>
/// Metadata model for a file in cloud storage
/// </summary>
public class CloudFileMetadata
{
    /// <summary>
    /// Provider-specific file identifier
    /// </summary>
    public required string FileId { get; set; }

    /// <summary>
    /// Name of the file
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public required long Size { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Date and time the file was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Date and time the file was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Web URL for viewing/previewing the file
    /// </summary>
    public string? WebUrl { get; set; }

    /// <summary>
    /// ETag or version identifier
    /// </summary>
    public string? ETag { get; set; }
}
