using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Attachment upload result
/// </summary>
public class AttachmentUploadResult
{
    public bool Success { get; set; }
    public EquipmentAttachmentDto? Attachment { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
}
