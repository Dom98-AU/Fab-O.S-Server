using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Attachment download response
/// </summary>
public class AttachmentDownloadResponse
{
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
