using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class UpdateKitStatusRequest
{
    public KitStatus Status { get; set; }
}
