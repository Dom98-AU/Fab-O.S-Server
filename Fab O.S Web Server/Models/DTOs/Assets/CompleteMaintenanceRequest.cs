using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CompleteMaintenanceRequest
{
    public string? PerformedBy { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? PartsCost { get; set; }
    public string? CompletedChecklist { get; set; }
    public string? PartsUsed { get; set; }
    public string? Notes { get; set; }
    public string? Attachments { get; set; }
}
