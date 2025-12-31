using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceReport
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public List<TraceMaterial> Materials { get; set; } = new();
    public List<TraceProcess> Processes { get; set; } = new();
    public List<TraceDocument> Documents { get; set; } = new();
    public string? GenealogyTree { get; set; }
    public string? Summary { get; set; }
}
