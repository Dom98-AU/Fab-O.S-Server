using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CreateAdHocKitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<int> EquipmentIds { get; set; } = new();
}
