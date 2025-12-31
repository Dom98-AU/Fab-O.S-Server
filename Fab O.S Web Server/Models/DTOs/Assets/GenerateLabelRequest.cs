using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class GenerateLabelRequest
{
    public int EquipmentId { get; set; }
    public string Template { get; set; } = "Standard";
    public LabelOptionsDto? Options { get; set; }
}
