using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceProcessDto
{
    public int Id { get; set; }
    public int TraceRecordId { get; set; }
    public int? OperationId { get; set; }
    public string OperationName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? Duration { get; set; }
    public int? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? MachineId { get; set; }
    public bool PassedInspection { get; set; }
    public string? InspectionNotes { get; set; }
    public DateTime CreatedDate { get; set; }
}
