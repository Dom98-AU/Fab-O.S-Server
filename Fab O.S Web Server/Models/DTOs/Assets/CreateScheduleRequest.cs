using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CreateScheduleRequest
{
    public int EquipmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceFrequency Frequency { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime? NextDue { get; set; }
    public int ReminderDaysBefore { get; set; } = 7;
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? AssignedTo { get; set; }
    public string? ChecklistItems { get; set; }
    public string? RequiredParts { get; set; }
}
