using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class KitCheckoutDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int KitId { get; set; }
    public string? KitCode { get; set; }
    public string? KitName { get; set; }
    public CheckoutStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public int CheckedOutToUserId { get; set; }
    public string? CheckedOutToUserName { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }
    public string? CheckoutPurpose { get; set; }
    public string? ProjectReference { get; set; }
    public EquipmentCondition CheckoutOverallCondition { get; set; }
    public string CheckoutConditionName => CheckoutOverallCondition.ToString();
    public string? CheckoutNotes { get; set; }
    public bool HasCheckoutSignature { get; set; }
    public DateTime? CheckoutSignedDate { get; set; }
    public int? CheckoutProcessedByUserId { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public int? ReturnedByUserId { get; set; }
    public string? ReturnedByUserName { get; set; }
    public EquipmentCondition? ReturnOverallCondition { get; set; }
    public string? ReturnConditionName => ReturnOverallCondition?.ToString();
    public string? ReturnNotes { get; set; }
    public bool HasReturnSignature { get; set; }
    public DateTime? ReturnSignedDate { get; set; }
    public int? ReturnProcessedByUserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int DaysUntilDue => (ExpectedReturnDate - DateTime.UtcNow).Days;
    public bool IsOverdue => ExpectedReturnDate < DateTime.UtcNow && Status == CheckoutStatus.CheckedOut;
    public List<KitCheckoutItemDto> CheckoutItems { get; set; } = new();
}
