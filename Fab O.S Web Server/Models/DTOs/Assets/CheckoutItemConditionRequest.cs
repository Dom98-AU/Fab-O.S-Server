using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CheckoutItemConditionRequest
{
    public int KitItemId { get; set; }
    public int EquipmentId { get; set; }
    public bool WasPresent { get; set; } = true;
    public EquipmentCondition Condition { get; set; } = EquipmentCondition.Good;
    public string? Notes { get; set; }
}
