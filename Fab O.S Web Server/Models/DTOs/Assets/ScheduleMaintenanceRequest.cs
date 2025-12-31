using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class ScheduleMaintenanceRequest
{
    public int EquipmentId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public string? Description { get; set; }
}
