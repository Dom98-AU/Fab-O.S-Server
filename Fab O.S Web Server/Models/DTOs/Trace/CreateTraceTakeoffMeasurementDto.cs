using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class CreateTraceTakeoffMeasurementDto
{
    public int TraceTakeoffId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Coordinates { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
}
