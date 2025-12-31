using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class FlagMaintenanceRequest
{
    public int EquipmentId { get; set; }
    public string? Notes { get; set; }
}
