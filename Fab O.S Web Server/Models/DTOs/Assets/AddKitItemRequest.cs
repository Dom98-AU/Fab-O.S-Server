using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class AddKitItemRequest
{
    public int EquipmentId { get; set; }
    public int? TemplateItemId { get; set; }
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}
