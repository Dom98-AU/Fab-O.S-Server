using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class KitDashboardDto
{
    public int TotalKits { get; set; }
    public int AvailableKits { get; set; }
    public int CheckedOutKits { get; set; }
    public int MaintenanceFlaggedKits { get; set; }
    public int OverdueCheckouts { get; set; }
    public Dictionary<string, int> KitsByTemplate { get; set; } = new();
    public Dictionary<KitStatus, int> KitsByStatus { get; set; } = new();
}
