using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentDashboardDto
{
    public int TotalEquipment { get; set; }
    public int ActiveEquipment { get; set; }
    public int InMaintenanceEquipment { get; set; }
    public int OutOfServiceEquipment { get; set; }
    public int RetiredEquipment { get; set; }
    public int UpcomingMaintenanceCount { get; set; }
    public int OverdueMaintenanceCount { get; set; }
    public int ExpiringCertificationsCount { get; set; }
    public Dictionary<string, int> EquipmentByCategory { get; set; } = new();
    public Dictionary<string, int> EquipmentByLocation { get; set; } = new();
    public List<EquipmentDto> RecentEquipment { get; set; } = new();
    public List<MaintenanceRecordDto> RecentMaintenance { get; set; } = new();
}
