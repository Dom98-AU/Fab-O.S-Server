namespace FabOS.WebServer.Models.DTOs.Trace;

public class ProcessSummaryDto
{
    public int ProcessId { get; set; }
    public string OperationName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string OperatorName { get; set; }
    public bool PassedInspection { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}
