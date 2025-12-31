using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class MaintenanceRecordDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public int? ScheduleId { get; set; }
    public string? ScheduleTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MaintenanceType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public MaintenanceRecordStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? PerformedBy { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? PartsCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string? CompletedChecklist { get; set; }
    public string? PartsUsed { get; set; }
    public string? Notes { get; set; }
    public string? Attachments { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
}
