namespace FabOS.WebServer.Models.DTOs.Trace;

public class MaterialSummaryDto
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialDescription { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
    public string BatchNumber { get; set; }
    public DateTime ReceivedDate { get; set; }
}
