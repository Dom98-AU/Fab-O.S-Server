using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class UpdateItemConditionRequest
{
    public EquipmentCondition Condition { get; set; }
    public string? Notes { get; set; }
}
