using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for updating a location
/// </summary>
public class UpdateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LocationType Type { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
}
