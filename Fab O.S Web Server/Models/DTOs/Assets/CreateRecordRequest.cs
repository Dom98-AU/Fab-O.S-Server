using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CreateRecordRequest
{
    public int EquipmentId { get; set; }
    public int? ScheduleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MaintenanceType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? AssignedTo { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
}
