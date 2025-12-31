using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class EquipmentStatusUpdateRequest
{
    public string Status { get; set; } = string.Empty;
}
