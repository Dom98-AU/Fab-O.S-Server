using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class PartialReturnRequest
{
    public List<ReturnItemConditionRequest> ReturnedItems { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
