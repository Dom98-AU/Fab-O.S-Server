using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class CreateTraceTakeoffDto
{
    public int TraceRecordId { get; set; }
    public int? DrawingId { get; set; }
    public string PdfUrl { get; set; } = string.Empty;
    public decimal? Scale { get; set; }
    public string? ScaleUnit { get; set; }
    public string? CalibrationData { get; set; }
}
