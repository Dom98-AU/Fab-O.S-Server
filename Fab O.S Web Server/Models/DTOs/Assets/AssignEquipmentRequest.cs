using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for assigning equipment to a location
/// </summary>
public class AssignEquipmentRequest
{
    public List<int> EquipmentIds { get; set; } = new();
}
