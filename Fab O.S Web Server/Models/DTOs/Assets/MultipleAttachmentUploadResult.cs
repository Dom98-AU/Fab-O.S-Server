using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Multiple attachment upload result
/// </summary>
public class MultipleAttachmentUploadResult
{
    public bool Success { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<EquipmentAttachmentDto> UploadedAttachments { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
