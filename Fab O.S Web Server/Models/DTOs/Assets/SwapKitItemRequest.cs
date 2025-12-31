using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class SwapKitItemRequest
{
    public int OldEquipmentId { get; set; }
    public int NewEquipmentId { get; set; }
}
