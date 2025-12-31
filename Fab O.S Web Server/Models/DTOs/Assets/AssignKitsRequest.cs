using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for assigning kits to a location
/// </summary>
public class AssignKitsRequest
{
    public List<int> KitIds { get; set; } = new();
}
