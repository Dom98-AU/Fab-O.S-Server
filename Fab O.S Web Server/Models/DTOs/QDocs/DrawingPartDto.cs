using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class DrawingPartDto
{
    public int Id { get; set; }
    public int DrawingId { get; set; }
    public int? DrawingRevisionId { get; set; }

    // Part identification
    public string PartReference { get; set; } = string.Empty; // "B1", "B2", "B4"
    public string Description { get; set; } = string.Empty; // "150 PFC", "100x100x10 EA"
    public string PartType { get; set; } = string.Empty; // BEAM, PLATE, ANGLE, etc.

    // Material
    public string? MaterialSpec { get; set; }
    public string? MaterialGrade { get; set; } // "300+", "C450L0"
    public string? MaterialStandard { get; set; } // "ASNZS PFC"
    public string? Dimensions { get; set; }

    // 12 Geometry fields (all in mm)
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
    public string? AssemblyMark { get; set; } // "ASM1", "ASM2", null = loose part
    public string? AssemblyName { get; set; }
    public int? ParentAssemblyId { get; set; }

    // Additional CAD data
    public string? Coating { get; set; } // "HDG85", "GAL250"
    public decimal? Weight { get; set; } // kg
    public decimal? Volume { get; set; } // m³
    public decimal? PaintedArea { get; set; } // m²

    // Quantity
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty; // EA, KG, M

    public int? CatalogueItemId { get; set; }
    public string? Notes { get; set; }
}
