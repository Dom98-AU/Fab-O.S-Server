namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceRecordSummaryDto
{
    public Guid TraceId { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
    public string EntityDescription { get; set; }
    public string EventType { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; }
    public string ResponsibleParty { get; set; }
}
