using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class UpdateTraceRecordDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
}
