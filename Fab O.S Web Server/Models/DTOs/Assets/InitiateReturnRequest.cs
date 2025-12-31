using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class InitiateReturnRequest
{
    public EquipmentCondition OverallCondition { get; set; }
    public string? Notes { get; set; }
    public List<ReturnItemConditionRequest> ItemConditions { get; set; } = new();
}
