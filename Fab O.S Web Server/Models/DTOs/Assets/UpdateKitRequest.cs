using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class UpdateKitRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
}
