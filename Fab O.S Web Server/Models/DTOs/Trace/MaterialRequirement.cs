namespace FabOS.WebServer.Models.DTOs.Trace;

public class MaterialRequirement
{
    public int CatalogueItemId { get; set; }
    public string ItemCode { get; set; }
    public string Description { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ToOrderQuantity { get; set; }
    public string Unit { get; set; }
}
