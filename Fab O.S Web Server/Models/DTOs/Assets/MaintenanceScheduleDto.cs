using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class MaintenanceScheduleDto
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceFrequency Frequency { get; set; }
    public string FrequencyName => Frequency.ToString();
    public int? CustomIntervalDays { get; set; }
    public DateTime? LastPerformed { get; set; }
    public DateTime? NextDue { get; set; }
    public int ReminderDaysBefore { get; set; }
    public MaintenanceScheduleStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? AssignedTo { get; set; }
    public string? ChecklistItems { get; set; }
    public string? RequiredParts { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public int CompletedCount { get; set; }
}
