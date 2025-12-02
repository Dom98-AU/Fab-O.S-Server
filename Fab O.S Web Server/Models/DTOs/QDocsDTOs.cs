namespace FabOS.WebServer.Models.DTOs;

// ==========================================
// DRAWING ASSEMBLY DTOs
// For SMLX/IFC assembly groupings
// ==========================================

public class DrawingAssemblyDto
{
    public int Id { get; set; }
    public int DrawingId { get; set; }
    public int? DrawingRevisionId { get; set; }
    public string AssemblyMark { get; set; } = string.Empty; // "ASM1", "ASM2"
    public string AssemblyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalWeight { get; set; } // kg
    public int PartCount { get; set; }
    public DateTime CreatedDate { get; set; }

    // Nested parts
    public List<DrawingPartDto> Parts { get; set; } = new();
}

public class CreateDrawingAssemblyDto
{
    public int DrawingId { get; set; }
    public int? DrawingRevisionId { get; set; }
    public string AssemblyMark { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TotalWeight { get; set; }
    public int PartCount { get; set; }
}

// ==========================================
// DRAWING PART DTOs
// Individual components with geometry + assembly fields
// ==========================================

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

// ==========================================
// DRAWING UPLOAD / PARSE RESULT DTOs
// Returned after SMLX/IFC file upload
// ==========================================

public class DrawingUploadResultDto
{
    public int DrawingId { get; set; }
    public int DrawingRevisionId { get; set; }
    public int DrawingFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // SMLX, IFC, DXF, PDF
    public string ParseStatus { get; set; } = string.Empty; // Completed, Failed, Pending
    public string? ParseErrors { get; set; }

    public int AssemblyCount { get; set; }
    public int PartCount { get; set; }
    public int LoosePartCount { get; set; } // Parts not in any assembly

    public List<DrawingAssemblyDto> Assemblies { get; set; } = new();
    public List<DrawingPartDto> LooseParts { get; set; } = new();

    public decimal TotalWeight { get; set; } // kg
    public DateTime UploadedDate { get; set; }
}

// ==========================================
// SMLX/IFC PARSER DTOs
// Intermediate DTOs used during parsing
// ==========================================

public class ParsedAssemblyDto
{
    public string AssemblyMark { get; set; } = string.Empty; // From SMLX m_strMark or IFC IFCRELAGGREGATES
    public string AssemblyName { get; set; } = string.Empty;
    public List<ParsedPartDto> Parts { get; set; } = new();
    public decimal TotalWeight { get; set; }
}

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
    public decimal? FlangeThickness { get; set; } // From profile or geometry points
    public decimal? FlangeWidth { get; set; }
    public decimal? WebThickness { get; set; }
    public decimal? WebDepth { get; set; }
    public decimal? LegA { get; set; }
    public decimal? LegB { get; set; }

    public decimal? Weight { get; set; } // m_ExactWeight or calculated
    public decimal? Volume { get; set; } // m_Volume
    public decimal? PaintedArea { get; set; } // m_PaintedArea

    public string? AssemblyMark { get; set; } // Parent assembly mark
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "EA";
}

// ==========================================
// QDOCS DRAWING DTOs
// Main drawing entities
// ==========================================

public class QDocsDrawingDto
{
    public int Id { get; set; }
    public string DrawingNumber { get; set; } = string.Empty;
    public string DrawingTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderId { get; set; }
    public int? WorkPackageId { get; set; }
    public int? AssemblyId { get; set; }
    public string CurrentStage { get; set; } = "IFA"; // IFA, IFC, Superseded
    public int? ActiveRevisionId { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public List<DrawingRevisionDto> Revisions { get; set; } = new();
}

public class CreateQDocsDrawingDto
{
    public string DrawingNumber { get; set; } = string.Empty;
    public string DrawingTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderId { get; set; }
    public int? WorkPackageId { get; set; }
}

// ==========================================
// DRAWING REVISION DTOs
// ==========================================

public class DrawingRevisionDto
{
    public int Id { get; set; }
    public int DrawingId { get; set; }
    public string RevisionCode { get; set; } = string.Empty; // Generated via NumberSeries
    public string RevisionType { get; set; } = string.Empty; // IFA, IFC
    public int RevisionNumber { get; set; }
    public string? RevisionNotes { get; set; }
    public string? DrawingFileName { get; set; }
    public string? CloudProvider { get; set; }
    public string? CloudFileId { get; set; }
    public string? CloudFilePath { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, UnderReview, Approved, Rejected
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovalComments { get; set; }
    public int? SupersededById { get; set; }
    public int? CreatedFromIFARevisionId { get; set; }
    public bool IsActiveForProduction { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateDrawingRevisionDto
{
    public int DrawingId { get; set; }
    public string RevisionType { get; set; } = string.Empty; // IFA or IFC
    public int RevisionNumber { get; set; }
    public string? RevisionNotes { get; set; }
    public int? CreatedFromIFARevisionId { get; set; } // For IFC revisions created from IFA
}
