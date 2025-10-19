namespace FabOS.WebServer.Models.CloudStorage;

/// <summary>
/// Request model for uploading a file to cloud storage
/// </summary>
public class CloudFileUploadRequest
{
    /// <summary>
    /// Destination folder path in cloud storage
    /// </summary>
    public required string FolderPath { get; set; }

    /// <summary>
    /// Name of the file to upload
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// File content stream
    /// </summary>
    public required Stream Content { get; set; }

    /// <summary>
    /// MIME type of the file (e.g., "application/pdf")
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Optional metadata to attach to the file
    /// Provider-specific metadata can be included here
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
