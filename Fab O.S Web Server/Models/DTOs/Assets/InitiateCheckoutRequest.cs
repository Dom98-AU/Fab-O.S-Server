using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class InitiateCheckoutRequest
{
    public int KitId { get; set; }
    public int CheckedOutToUserId { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public string? CheckoutPurpose { get; set; }
    public string? ProjectReference { get; set; }
    public EquipmentCondition OverallCondition { get; set; } = EquipmentCondition.Good;
    public string? Notes { get; set; }
    public List<CheckoutItemConditionRequest> ItemConditions { get; set; } = new();
}
