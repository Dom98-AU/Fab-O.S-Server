using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class UpdateMaintenanceScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FrequencyType { get; set; } = "Monthly";
    public int FrequencyValue { get; set; } = 1;
    public int? HoursInterval { get; set; }
    public string? ChecklistItems { get; set; }
    public decimal? EstimatedDurationHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public int ReminderDaysBefore { get; set; } = 7;
    public bool SendEmailReminder { get; set; }
    public bool IsActive { get; set; } = true;
}
