namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceReportDto
{
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public List<TraceRecordSummaryDto> TraceRecords { get; set; } = new();
    public List<MaterialSummaryDto> Materials { get; set; } = new();
    public List<ProcessSummaryDto> Processes { get; set; } = new();
    public List<DocumentSummaryDto> Documents { get; set; } = new();
    public string? Summary { get; set; }
}
