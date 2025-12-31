using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Models.DTOs.Trace;

public class CatalogueItemDto
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Unit { get; set; }
    public decimal? Weight { get; set; }
    public string? WeightUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Currency { get; set; }
    public string? Supplier { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? SupplierPartNumber { get; set; }
    public string? Specifications { get; set; }
    public bool IsActive { get; set; }
}
