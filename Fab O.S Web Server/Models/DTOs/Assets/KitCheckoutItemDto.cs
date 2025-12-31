using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class KitCheckoutItemDto
{
    public int Id { get; set; }
    public int KitCheckoutId { get; set; }
    public int KitItemId { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public bool WasPresentAtCheckout { get; set; }
    public EquipmentCondition? CheckoutCondition { get; set; }
    public string? CheckoutConditionName => CheckoutCondition?.ToString();
    public string? CheckoutNotes { get; set; }
    public bool? WasPresentAtReturn { get; set; }
    public EquipmentCondition? ReturnCondition { get; set; }
    public string? ReturnConditionName => ReturnCondition?.ToString();
    public string? ReturnNotes { get; set; }
    public bool DamageReported { get; set; }
    public string? DamageDescription { get; set; }
}
