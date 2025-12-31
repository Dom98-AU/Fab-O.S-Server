namespace FabOS.WebServer.Models.DTOs.Trace;

public class BillOfMaterialsDto
{
    public int TakeoffId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public List<BOMLineItemDto> LineItems { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
    public decimal? TotalWeight { get; set; }
}
