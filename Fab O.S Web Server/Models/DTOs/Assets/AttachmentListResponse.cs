using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Attachment list response with pagination
/// </summary>
public class AttachmentListResponse
{
    public List<EquipmentAttachmentDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
