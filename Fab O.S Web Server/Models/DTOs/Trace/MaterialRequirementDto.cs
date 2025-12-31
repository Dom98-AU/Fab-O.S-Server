namespace FabOS.WebServer.Models.DTOs.Trace;

public class MaterialRequirementDto
{
    public int TraceRecordId { get; set; }
    public List<MaterialRequirement> Requirements { get; set; } = new();
    public decimal TotalWeight { get; set; }
}
