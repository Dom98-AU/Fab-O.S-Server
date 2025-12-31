using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class GenerateBatchLabelsRequest
{
    public List<int> EquipmentIds { get; set; } = new();
    public string Template { get; set; } = "Standard";
    public LabelOptionsDto? Options { get; set; }
}
