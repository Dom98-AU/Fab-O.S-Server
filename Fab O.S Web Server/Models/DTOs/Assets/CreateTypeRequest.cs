using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CreateTypeRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DefaultMaintenanceIntervalDays { get; set; }
    public string? RequiredCertifications { get; set; }
    public int DisplayOrder { get; set; }
}
