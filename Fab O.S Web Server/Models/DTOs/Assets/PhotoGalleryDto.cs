using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Photo gallery response DTO
/// </summary>
public class PhotoGalleryDto
{
    public EquipmentAttachmentDto? PrimaryPhoto { get; set; }
    public List<EquipmentAttachmentDto> Photos { get; set; } = new();
    public int TotalCount { get; set; }
}
