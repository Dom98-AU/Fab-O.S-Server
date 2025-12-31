using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for creating a location
/// </summary>
public class CreateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LocationType Type { get; set; } = LocationType.PhysicalSite;
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}
