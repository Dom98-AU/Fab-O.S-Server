using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class CreateDrawingPartDto
{
    public int DrawingId { get; set; }
    public int? DrawingRevisionId { get; set; }

    public string PartReference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;

    public string? MaterialSpec { get; set; }
    public string? MaterialGrade { get; set; }
    public string? MaterialStandard { get; set; }
    public string? Dimensions { get; set; }

    // 12 Geometry fields
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Diameter { get; set; }
    public decimal? FlangeThickness { get; set; }
    public decimal? FlangeWidth { get; set; }
    public decimal? WebThickness { get; set; }
    public decimal? WebDepth { get; set; }
    public decimal? OutsideDiameter { get; set; }
    public decimal? WallThickness { get; set; }
    public decimal? LegA { get; set; }
    public decimal? LegB { get; set; }

    // Assembly tracking
    public string? AssemblyMark { get; set; }
    public string? AssemblyName { get; set; }
    public int? ParentAssemblyId { get; set; }

    // Additional CAD data
    public string? Coating { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Volume { get; set; }
    public decimal? PaintedArea { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int? CatalogueItemId { get; set; }
    public string? Notes { get; set; }
}
