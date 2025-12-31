using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class BillOfMaterials
{
    public int TakeoffId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    public List<BOMLineItem> LineItems { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
}
