namespace FabOS.WebServer.Models.CloudStorage;

/// <summary>
/// Upload session model for large file uploads
/// Enables chunked upload for files larger than 4MB
/// </summary>
public class CloudUploadSession
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// URL to upload file chunks to
    /// </summary>
    public required string UploadUrl { get; set; }

    /// <summary>
    /// Date and time when the upload session expires
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Recommended chunk size in bytes
    /// Typically 5-10 MB for optimal performance
    /// </summary>
    public long? ChunkSize { get; set; }
}
