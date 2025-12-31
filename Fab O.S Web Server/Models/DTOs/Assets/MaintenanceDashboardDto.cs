using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class MaintenanceDashboardDto
{
    public int TotalSchedules { get; set; }
    public int ActiveSchedules { get; set; }
    public int UpcomingMaintenanceCount { get; set; }
    public int OverdueMaintenanceCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedThisMonth { get; set; }
    public decimal TotalCostThisMonth { get; set; }
    public decimal AverageCostPerMaintenance { get; set; }
    public Dictionary<string, int> MaintenanceByType { get; set; } = new();
    public Dictionary<string, decimal> CostByEquipment { get; set; } = new();
    public List<MaintenanceRecordDto> UpcomingMaintenance { get; set; } = new();
    public List<MaintenanceRecordDto> OverdueMaintenance { get; set; } = new();
}
