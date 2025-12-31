using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceTakeoffDto
{
    public int Id { get; set; }
    public int TraceRecordId { get; set; }
    public int? DrawingId { get; set; }
    public string PdfUrl { get; set; } = string.Empty;
    public decimal? Scale { get; set; }
    public string? ScaleUnit { get; set; }
    public string? CalibrationData { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<TraceTakeoffMeasurementDto> Measurements { get; set; } = new();
    public int MeasurementCount { get; set; }
    public decimal? TotalWeight { get; set; }
}
