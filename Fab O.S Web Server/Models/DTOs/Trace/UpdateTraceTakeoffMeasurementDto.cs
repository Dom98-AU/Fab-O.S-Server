using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class UpdateTraceTakeoffMeasurementDto
{
    public int? CatalogueItemId { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
}
