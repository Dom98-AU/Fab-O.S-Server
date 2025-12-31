using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class UpdateTemplateItemRequest
{
    public int Quantity { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? Notes { get; set; }
}
