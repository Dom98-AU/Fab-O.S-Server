using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class CreateTraceMaterialDto
{
    public int TraceRecordId { get; set; }
    public int? CatalogueItemId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    public string? CertificateNumber { get; set; }
}
