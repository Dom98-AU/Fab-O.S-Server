using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public int TotalDrawings { get; set; }
    public int TotalParts { get; set; }
    public decimal TotalWeight { get; set; }
    public int TotalQuantity { get; set; }
}
