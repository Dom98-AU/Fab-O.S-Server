using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class TraceRecordDto
{
    public int Id { get; set; }
    public Guid TraceId { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public TraceableType EntityType { get; set; }
    public int EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? Location { get; set; }
    public string? ResponsibleParty { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int CompanyId { get; set; }

    public List<TraceMaterialDto> Materials { get; set; } = new();
    public List<TraceProcessDto> Processes { get; set; } = new();
    public List<TraceDocumentDto> Documents { get; set; } = new();
}
