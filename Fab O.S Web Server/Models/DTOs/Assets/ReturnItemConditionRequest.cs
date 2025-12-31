using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class ReturnItemConditionRequest
{
    public int KitItemId { get; set; }
    public int EquipmentId { get; set; }
    public bool WasPresent { get; set; } = true;
    public EquipmentCondition Condition { get; set; }
    public string? Notes { get; set; }
    public bool DamageReported { get; set; }
    public string? DamageDescription { get; set; }
}
