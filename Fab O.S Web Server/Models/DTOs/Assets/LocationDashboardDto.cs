using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Dashboard statistics for locations
/// </summary>
public class LocationDashboardDto
{
    public int TotalLocations { get; set; }
    public int ActiveLocations { get; set; }
    public int PhysicalSites { get; set; }
    public int JobSites { get; set; }
    public int Vehicles { get; set; }
    public int TotalEquipmentAllocated { get; set; }
    public int TotalKitsAllocated { get; set; }
}
