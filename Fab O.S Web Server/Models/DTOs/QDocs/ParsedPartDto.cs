using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.QDocs;

public class ParsedPartDto
{
    // From SMLX or IFC
    public string PartReference { get; set; } = string.Empty; // m_strSinglePartMark or IFCBEAM.Tag
    public string Description { get; set; } = string.Empty; // m_strName or Profile name
    public string PartType { get; set; } = string.Empty; // m_strRole or IFC entity type

    public string? MaterialGrade { get; set; } // m_strName (CGREXMaterial) or IFCMATERIAL
    public string? MaterialStandard { get; set; } // m_strStandard or inferred from profile
    public string? Coating { get; set; } // m_strCoating

    // Geometry (mm)
    public decimal? Length { get; set; } // m_Length * 1000 (SMLX) or calculated (IFC)
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Diameter { get; set; } // For round bars
    public decimal? FlangeThickness { get; set; } // From profile or geometry points
    public decimal? FlangeWidth { get; set; }
    public decimal? WebThickness { get; set; }
    public decimal? WebDepth { get; set; }
    public decimal? OutsideDiameter { get; set; } // For CHS
    public decimal? WallThickness { get; set; } // For RHS/CHS
    public decimal? LegA { get; set; }
    public decimal? LegB { get; set; }

    public decimal? Weight { get; set; } // m_ExactWeight or calculated
    public decimal? Volume { get; set; } // m_Volume
    public decimal? PaintedArea { get; set; } // m_PaintedArea

    public string? AssemblyMark { get; set; } // Parent assembly mark
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "EA";
}
