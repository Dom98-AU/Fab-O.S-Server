using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceTakeoffMeasurementDto
{
    public int Id { get; set; }
    public int TraceTakeoffId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Coordinates { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CatalogueItemCode { get; set; }
    public string? CatalogueItemDescription { get; set; }
}
