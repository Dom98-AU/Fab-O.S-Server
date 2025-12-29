using System.ComponentModel.DataAnnotations;

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
    [Required]
    [Range(1, int.MaxValue)]
    public int DrawingId { get; set; }
    
    public int? DrawingRevisionId { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string AssemblyMark { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string AssemblyName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalWeight { get; set; }
    
    [Range(0, int.MaxValue)]
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
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DrawingNumber { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string DrawingTitle { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "OrderId must be a positive integer")]
    public int OrderId { get; set; }
    
    public int? WorkPackageId { get; set; }
}

public class UpdateQDocsDrawingDto
{
    public string? DrawingNumber { get; set; }
    public string? DrawingTitle { get; set; }
    public string? Description { get; set; }
    public int? WorkPackageId { get; set; }
}

// ==========================================
// DRAWING PART (Simple for API tests)
// ==========================================

public class SimpleDrawingPartDto
{
    public int Id { get; set; }
    public int? DrawingId { get; set; }
    public string PartMark { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal? WeightEach { get; set; }
    public string? MaterialGrade { get; set; }
    public bool IsManualEntry { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateSimpleDrawingPartDto
{
    public string PartMark { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal? WeightEach { get; set; }
    public string? MaterialGrade { get; set; }
}

public class CreateManualPartDto
{
    public string PartMark { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal? WeightEach { get; set; }
    public string? MaterialGrade { get; set; }
    public int OrderId { get; set; }
}

public class UpdateSimplePartDto
{
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? WeightEach { get; set; }
    public string? MaterialGrade { get; set; }
}

public class UpdateRevisionStatusDto
{
    [Required]
    [RegularExpression("^(Draft|UnderReview|Approved|Rejected|Superseded)$", 
        ErrorMessage = "Status must be one of: Draft, UnderReview, Approved, Rejected, Superseded")]
    public string Status { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? ReviewedBy { get; set; }
    
    [StringLength(200)]
    public string? ApprovedBy { get; set; }
    
    [StringLength(1000)]
    public string? ApprovalComments { get; set; }
}

// ==========================================
// DRAWING SUMMARY DTOs
// ==========================================

public class DrawingSummaryDto
{
    public int DrawingId { get; set; }
    public string DrawingNumber { get; set; } = string.Empty;
    public string DrawingTitle { get; set; } = string.Empty;
    public int TotalParts { get; set; }
    public decimal TotalWeight { get; set; }
    public int TotalQuantity { get; set; }
    public int RevisionCount { get; set; }
    public string CurrentStage { get; set; } = string.Empty;
}

public class OrderSummaryDto
{
    public int OrderId { get; set; }
    public int TotalDrawings { get; set; }
    public int TotalParts { get; set; }
    public decimal TotalWeight { get; set; }
    public int TotalQuantity { get; set; }
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
    [Required]
    [Range(1, int.MaxValue)]
    public int DrawingId { get; set; }
    
    [Required]
    [RegularExpression("^(IFA|IFC)$", ErrorMessage = "RevisionType must be 'IFA' or 'IFC'")]
    public string RevisionType { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 999)]
    public int RevisionNumber { get; set; }
    
    [StringLength(2000)]
    public string? RevisionNotes { get; set; }
    
    public int? CreatedFromIFARevisionId { get; set; }
}

// ==========================================
// INTERACTIVE CAD IMPORT DTOs
// For handling unidentified parts during import
// ==========================================

/// <summary>
/// Result of parsing a CAD file (SMLX/IFC) before final import.
/// Contains identified parts (with references) and unidentified parts (need user input).
/// </summary>
public class CadImportPreviewDto
{
    public string ImportSessionId { get; set; } = string.Empty; // Unique ID for this import session
    public int DrawingId { get; set; }
    public int DrawingRevisionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // SMLX, IFC

    /// <summary>
    /// Status of the import:
    /// - "Ready" = all parts identified, can import directly
    /// - "PendingReview" = has unidentified parts, needs user input
    /// - "Failed" = parsing failed
    /// </summary>
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    // Counts for summary
    public int TotalElementCount { get; set; }
    public int IdentifiedPartCount { get; set; }
    public int UnidentifiedPartCount { get; set; }
    public int AssemblyCount { get; set; }

    // Parts that are fully identified (have part references)
    public List<CadImportAssemblyPreviewDto> Assemblies { get; set; } = new();

    // Parts that need user identification (no part reference in CAD file)
    public List<UnidentifiedPartDto> UnidentifiedParts { get; set; } = new();

    public DateTime ParsedDate { get; set; }
    public DateTime ExpiresAt { get; set; } // When this preview session expires
}

/// <summary>
/// Assembly preview during import, showing both identified and unidentified parts
/// </summary>
public class CadImportAssemblyPreviewDto
{
    public string TempAssemblyId { get; set; } = string.Empty; // Temporary ID for mapping
    public string? AssemblyMark { get; set; } // May be null if assembly itself needs identification
    public string? AssemblyName { get; set; }
    public bool NeedsIdentification { get; set; } // True if assembly mark is missing/unclear
    public string? SuggestedAssemblyMark { get; set; } // Auto-generated or original mark for reference when unidentified

    // Parts within this assembly
    public List<IdentifiedPartPreviewDto> IdentifiedParts { get; set; } = new();
    public List<UnidentifiedPartDto> UnidentifiedParts { get; set; } = new();

    public decimal TotalWeight { get; set; }
    public int TotalPartCount { get; set; }
}

/// <summary>
/// A part that was successfully identified from the CAD file
/// </summary>
public class IdentifiedPartPreviewDto
{
    public string TempPartId { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty; // The identified part mark
    public string Description { get; set; } = string.Empty;
    public string PartType { get; set; } = string.Empty;
    public string? MaterialGrade { get; set; }
    public string? Profile { get; set; } // e.g., "PFC 150", "EA 100x100x10"
    public decimal? Weight { get; set; }
    public int Quantity { get; set; } = 1;
    public string? AssemblyMark { get; set; }
}

/// <summary>
/// A part that could not be identified from the CAD file.
/// User must provide a part reference/mark before import can complete.
/// </summary>
public class UnidentifiedPartDto
{
    public string TempPartId { get; set; } = string.Empty; // Unique ID for this unidentified part

    // Information extracted from CAD to help user identify the part
    public string PartType { get; set; } = string.Empty; // "Beam", "Plate", "Bolt", "Nut"
    public string? Profile { get; set; } // "PFC 150", "EA 100x100x10", etc.
    public string? Description { get; set; } // Additional description from CAD
    public string? MaterialGrade { get; set; }
    public string? ObjectType { get; set; } // IFC ObjectType if available
    public string? ElementName { get; set; } // IFC element Name if available

    // Geometry info to help identification
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Weight { get; set; }

    // Assembly context
    public string? TempAssemblyId { get; set; } // Which assembly this part belongs to (if any)
    public string? AssemblyMark { get; set; } // Parent assembly mark (if identified)

    public int Quantity { get; set; } = 1;

    // Suggested reference (system's best guess based on profile/type)
    public string? SuggestedReference { get; set; }

    // User-provided reference (set during review)
    public string? UserProvidedReference { get; set; }
}

/// <summary>
/// Request to confirm and complete an import after user has provided part references
/// </summary>
public class ConfirmCadImportRequestDto
{
    public string ImportSessionId { get; set; } = string.Empty;

    // Mappings for unidentified parts
    public List<PartReferenceMappingDto> PartMappings { get; set; } = new();

    // Mappings for unidentified assemblies (if any)
    public List<AssemblyReferenceMappingDto> AssemblyMappings { get; set; } = new();

    // Option to auto-generate references for remaining unidentified parts
    public bool AutoGenerateRemainingReferences { get; set; } = false;
}

/// <summary>
/// User-provided part reference mapping
/// </summary>
public class PartReferenceMappingDto
{
    public string TempPartId { get; set; } = string.Empty;
    public string PartReference { get; set; } = string.Empty; // User-provided part mark
}

/// <summary>
/// User-provided assembly reference mapping
/// </summary>
public class AssemblyReferenceMappingDto
{
    public string TempAssemblyId { get; set; } = string.Empty;
    public string AssemblyMark { get; set; } = string.Empty;
    public string? AssemblyName { get; set; }
}

/// <summary>
/// Extended ParsedPartDto that includes temp ID and identification status
/// Used internally during parsing
/// </summary>
public class ParsedPartWithIdDto : ParsedPartDto
{
    public string TempPartId { get; set; } = string.Empty;
    public string? TempAssemblyId { get; set; }
    public bool IsIdentified { get; set; } // True if has valid PartReference
    public string? IfcElementName { get; set; } // Original IFC element name
    public string? IfcObjectType { get; set; } // Original IFC object type
    public string? SuggestedReference { get; set; } // System's suggested reference
}

/// <summary>
/// Extended ParsedAssemblyDto that includes temp ID and unidentified parts
/// </summary>
public class ParsedAssemblyWithIdDto : ParsedAssemblyDto
{
    public string TempAssemblyId { get; set; } = string.Empty;
    public bool IsIdentified { get; set; } // True if has valid/meaningful AssemblyMark
    public string? SuggestedAssemblyMark { get; set; } // Auto-generated mark for reference when unidentified
    public List<ParsedPartWithIdDto> IdentifiedParts { get; set; } = new();
    public List<ParsedPartWithIdDto> UnidentifiedParts { get; set; } = new();
}
