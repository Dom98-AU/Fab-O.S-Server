using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class CreateTraceProcessDto
{
    public int TraceRecordId { get; set; }
    public int? OperationId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? MachineId { get; set; }
}
