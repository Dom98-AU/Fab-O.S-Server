using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// ==========================================
// QDOCS MODULE - QUALITY DOCUMENTATION SYSTEM
// Integrates with FabMate: Order → WorkPackage → Assembly workflow
// Supports IFA (Issued For Approval) → IFC (Issued For Construction) drawing process
// ==========================================

// ==========================================
// IFA/IFC DRAWING MANAGEMENT
// ==========================================

// QDocs Drawing - Container for engineering drawings (IFA → IFC workflow)
[Table("QDocsDrawings")]
public class QDocsDrawing
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string DrawingNumber { get; set; } = string.Empty; // COL-001

    [Required]
    [StringLength(200)]
    public string DrawingTitle { get; set; } = string.Empty; // "6m Column Assembly"

    [StringLength(1000)]
    public string? Description { get; set; }

    // Links to FabMate workflow
    [Required]
    public int OrderId { get; set; }

    public int? WorkPackageId { get; set; } // Optional - can be assigned later

    public int? AssemblyId { get; set; } // Links to FabMate Assembly after IFC approval

    // Drawing stage tracking
    [Required]
    [StringLength(20)]
    public string CurrentStage { get; set; } = "IFA"; // IFA, IFC, Superseded

    public int? ActiveRevisionId { get; set; } // Current active revision

    // Audit
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("WorkPackageId")]
    public virtual WorkPackage? WorkPackage { get; set; }

    [ForeignKey("AssemblyId")]
    public virtual Assembly? Assembly { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<DrawingRevision> Revisions { get; set; } = new List<DrawingRevision>();
    public virtual ICollection<DrawingPart> Parts { get; set; } = new List<DrawingPart>();
}

// Drawing Revision - Individual versions (IFA-R1, IFA-R2, IFC-R1, etc.)
[Table("DrawingRevisions")]
public class DrawingRevision
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DrawingId { get; set; }

    [Required]
    [StringLength(50)]
    public string RevisionCode { get; set; } = string.Empty; // IFA-R1, IFA-R2, IFC-R1

    [Required]
    [StringLength(10)]
    public string RevisionType { get; set; } = string.Empty; // IFA, IFC

    [Required]
    public int RevisionNumber { get; set; } // 1, 2, 3...

    [StringLength(2000)]
    public string? RevisionNotes { get; set; }

    // Cloud-stored drawing file
    [StringLength(500)]
    public string? DrawingFileName { get; set; }

    [StringLength(50)]
    public string? CloudProvider { get; set; } // SharePoint, GoogleDrive, Dropbox

    [StringLength(500)]
    public string? CloudFileId { get; set; }

    [StringLength(1000)]
    public string? CloudFilePath { get; set; }

    // Approval workflow
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, UnderReview, Approved, Rejected, Superseded

    [StringLength(200)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedDate { get; set; }

    [StringLength(200)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    [StringLength(1000)]
    public string? ApprovalComments { get; set; }

    public int? SupersededById { get; set; } // Link to newer revision that replaced this one

    // IFC specific
    public int? CreatedFromIFARevisionId { get; set; } // If this is IFC, which IFA was it created from

    public bool IsActiveForProduction { get; set; } = false; // Only one IFC can be active

    // Audit
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedBy { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("DrawingId")]
    public virtual QDocsDrawing Drawing { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("SupersededById")]
    public virtual DrawingRevision? SupersededByRevision { get; set; }

    [ForeignKey("CreatedFromIFARevisionId")]
    public virtual DrawingRevision? CreatedFromIFARevision { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;
}

// Drawing Part - Individual components/materials in the drawing BOM
[Table("DrawingParts")]
public class DrawingPart
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DrawingId { get; set; }

    public int? DrawingRevisionId { get; set; } // Which revision this part belongs to (optional)

    [Required]
    [StringLength(100)]
    public string PartReference { get; set; } = string.Empty; // "Part 1", "UC Beam", etc.

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string PartType { get; set; } = string.Empty; // BEAM, PLATE, BOLT, CONSUMABLE, WELD

    // Material specifications
    [StringLength(200)]
    public string? MaterialSpec { get; set; } // "Grade 300, AS/NZS 3679.1"

    [StringLength(50)]
    public string? MaterialGrade { get; set; }

    [StringLength(200)]
    public string? MaterialStandard { get; set; }

    // Dimensions
    [StringLength(500)]
    public string? Dimensions { get; set; } // "UC 310x97 @ 6000mm"

    // Geometry fields (extracted from CAD files - IFC/DXF/SMLX)
    // Basic Dimensions (mm)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Length { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Width { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Thickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Diameter { get; set; }

    // Structural Steel Specific (Beams/Channels/UCs/UBs)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FlangeThickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FlangeWidth { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WebThickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WebDepth { get; set; }

    // Hollow Section Specific (SHS/RHS/CHS)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OutsideDiameter { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WallThickness { get; set; }

    // Angle Specific (Equal/Unequal Angles)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? LegA { get; set; } // First leg

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LegB { get; set; } // Second leg

    // Assembly tracking (from SMLX/IFC imports)
    [StringLength(100)]
    public string? AssemblyMark { get; set; } // "ASM1", "ASM2", null for loose parts

    [StringLength(200)]
    public string? AssemblyName { get; set; } // User-friendly assembly description

    public int? ParentAssemblyId { get; set; } // FK to DrawingAssembly (null = loose part)

    // Additional CAD data (from SMLX/IFC imports)
    [StringLength(100)]
    public string? Coating { get; set; } // "HDG85", "GAL250", etc.

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Weight { get; set; } // Exact weight from CAD (kg)

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Volume { get; set; } // Volume in m³

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PaintedArea { get; set; } // Surface area for coating (m²)

    // Quantity
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty; // EA, KG, M, etc.

    // Link to catalogue
    public int? CatalogueItemId { get; set; } // Auto-matched or manually linked

    [StringLength(200)]
    public string? Notes { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("DrawingId")]
    public virtual QDocsDrawing Drawing { get; set; } = null!;

    [ForeignKey("DrawingRevisionId")]
    public virtual DrawingRevision? DrawingRevision { get; set; }

    [ForeignKey("ParentAssemblyId")]
    public virtual DrawingAssembly? ParentAssembly { get; set; }

    [ForeignKey("CatalogueItemId")]
    public virtual CatalogueItem? CatalogueItem { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;
}

// Drawing Assembly - Logical grouping of parts from CAD imports (SMLX/IFC assemblies)
[Table("DrawingAssemblies")]
public class DrawingAssembly
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DrawingId { get; set; }

    public int? DrawingRevisionId { get; set; }

    [Required]
    [StringLength(100)]
    public string AssemblyMark { get; set; } = string.Empty; // "ASM1", "ASM2" from SMLX/IFC

    [Required]
    [StringLength(500)]
    public string AssemblyName { get; set; } = string.Empty; // User-friendly name

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalWeight { get; set; } // Calculated sum of part weights (kg)

    [Required]
    public int PartCount { get; set; } // Number of parts in this assembly

    [Required]
    public int CompanyId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DrawingId")]
    public virtual QDocsDrawing Drawing { get; set; } = null!;

    [ForeignKey("DrawingRevisionId")]
    public virtual DrawingRevision? DrawingRevision { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<DrawingPart> Parts { get; set; } = new List<DrawingPart>();
}

// Uploaded CAD files for part extraction (IFC, SMLX, DXF, STEP, NC)
[Table("DrawingFiles")]
public class DrawingFile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DrawingRevisionId { get; set; }

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string FileType { get; set; } = string.Empty; // IFC, DXF, STEP, SMLX, NC

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty; // Blob storage path

    [Required]
    public long FileSizeBytes { get; set; }

    [Required]
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int UploadedBy { get; set; }

    [Required]
    [StringLength(20)]
    public string ParseStatus { get; set; } = "Pending"; // Pending, Parsing, Completed, Failed

    [StringLength(2000)]
    public string? ParseErrors { get; set; }

    public int? ParsedPartCount { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("DrawingRevisionId")]
    public virtual DrawingRevision DrawingRevision { get; set; } = null!;

    [ForeignKey("UploadedBy")]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;
}

// ==========================================
// INSPECTION TEST PLANS (ITP)
// ==========================================

// Inspection Test Plan - Main ITP entity linked to Order/Package/WorkOrder hierarchy
[Table("InspectionTestPlans")]
public class InspectionTestPlan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ITPNumber { get; set; } = string.Empty; // ITP-2025-0001

    // Workflow Integration - Links to Order hierarchy
    [Required]
    public int OrderId { get; set; }

    [Required]
    public int WorkPackageId { get; set; } // QDocs uses WorkPackage (FabMate), not Package (Trace)

    public int? WorkOrderId { get; set; } // Optional - can cover multiple WOs

    // ITP Configuration
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public int? TemplateId { get; set; }

    [StringLength(1000)]
    public string? CustomerRequirements { get; set; }

    [StringLength(500)]
    public string? ApplicableStandards { get; set; } // AS5131, AS/NZS 1554.1, etc.

    // Status Tracking
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Active, Complete

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedDate { get; set; }

    public int? ApprovedById { get; set; }

    public DateTime? CompletedDate { get; set; }

    // Audit
    [Required]
    public int CreatedBy { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation Properties
    // NOTE: ForeignKey attributes removed - relationships explicitly configured in ApplicationDbContext
    // QDocs module uses WorkPackage (FabMate), not Package (Trace/Estimate)
    public virtual Order Order { get; set; } = null!;

    public virtual WorkPackage WorkPackage { get; set; } = null!;

    public virtual WorkOrder? WorkOrder { get; set; }

    [ForeignKey("ApprovedById")]
    public virtual User? ApprovedByUser { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<ITPInspectionPoint> InspectionPoints { get; set; } = new List<ITPInspectionPoint>();
    public virtual ICollection<ITPAssembly> CoveredAssemblies { get; set; } = new List<ITPAssembly>();
}

// ITP Assembly - Tracks which assemblies are covered by an ITP
[Table("ITPAssemblies")]
public class ITPAssembly
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ITPId { get; set; }

    [Required]
    public int WorkOrderAssemblyEntryId { get; set; } // Link to WorkOrderAssemblyEntry junction

    [Required]
    public int AssemblyId { get; set; } // The actual assembly being inspected

    [Required]
    public int QuantityToBuild { get; set; }

    [Required]
    public int QuantityInspected { get; set; } = 0;

    [Required]
    public int QuantityPassed { get; set; } = 0;

    [Required]
    public int QuantityFailed { get; set; } = 0;

    // Navigation
    [ForeignKey("ITPId")]
    public virtual InspectionTestPlan ITP { get; set; } = null!;

    [ForeignKey("WorkOrderAssemblyEntryId")]
    public virtual WorkOrderAssemblyEntry WorkOrderAssemblyEntry { get; set; } = null!;

    [ForeignKey("AssemblyId")]
    public virtual Assembly Assembly { get; set; } = null!;
}

// ITP Inspection Point - Individual inspection activities within an ITP
[Table("ITPInspectionPoints")]
public class ITPInspectionPoint
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ITPId { get; set; }

    [Required]
    public int Sequence { get; set; }

    // Activity Details
    [Required]
    [StringLength(200)]
    public string ActivityName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ActivityDescription { get; set; }

    [StringLength(200)]
    public string? ReferenceStandard { get; set; }

    [StringLength(1000)]
    public string? AcceptanceCriteria { get; set; }

    // Inspection Type Configuration
    [Required]
    [StringLength(20)]
    public string InspectionType { get; set; } = "Review"; // Hold, Witness, Review, None

    [StringLength(10)]
    public string? ClientLevel { get; set; } // H, W, R, N

    [StringLength(10)]
    public string? ContractorLevel { get; set; } // H, W, R, N

    [StringLength(10)]
    public string? ThirdPartyLevel { get; set; } // H, W, R, N

    // Required Documentation/Tests (JSON arrays)
    [StringLength(1000)]
    public string? RequiredDocuments { get; set; } // ["WPS", "PQR", "WQT"]

    [StringLength(1000)]
    public string? RequiredTests { get; set; } // ["Visual", "NDT", "Dimensional"]

    // Workflow Integration
    public int? WorkOrderOperationId { get; set; } // Link to operation (EXISTING FIELD in WorkOrderOperation!)

    public int? AssemblyId { get; set; } // Which assembly this point inspects

    // Hold Point Management
    [Required]
    public bool IsHoldPoint { get; set; } = false;

    public DateTime? HoldReleasedDate { get; set; }

    public int? ReleasedById { get; set; }

    [StringLength(1000)]
    public string? ReleaseComments { get; set; }

    // Status Tracking
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "NotStarted"; // NotStarted, Scheduled, InProgress, Completed, Failed, OnHold, Released, Waived

    public DateTime? ScheduledDate { get; set; }

    public DateTime? ActualDate { get; set; }

    public int? InspectorId { get; set; }

    [StringLength(100)]
    public string? InspectorName { get; set; }

    // Results
    [StringLength(20)]
    public string? Result { get; set; } // Pass, Fail, ConditionalPass, NotApplicable

    [StringLength(2000)]
    public string? ResultComments { get; set; }

    [StringLength(50)]
    public string? NCRNumber { get; set; } // If failed

    // Linked Documents
    public int? InspectionReportId { get; set; }

    public int? HoldReleaseDocumentId { get; set; }

    // Navigation
    [ForeignKey("ITPId")]
    public virtual InspectionTestPlan ITP { get; set; } = null!;

    [ForeignKey("WorkOrderOperationId")]
    public virtual WorkOrderOperation? WorkOrderOperation { get; set; }

    [ForeignKey("AssemblyId")]
    public virtual Assembly? Assembly { get; set; }

    [ForeignKey("InspectorId")]
    public virtual User? Inspector { get; set; }

    [ForeignKey("ReleasedById")]
    public virtual User? ReleasedByUser { get; set; }

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

// Material Traceability - Complete material genealogy from mill certificate to final assembly
[Table("MaterialTraceability")]
public class MaterialTraceability
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TraceNumber { get; set; } = string.Empty; // MTR-2025-0001

    // Catalogue & Inventory Integration
    [Required]
    public int CatalogueItemId { get; set; } // WHAT type of material (PP350MS-001)

    public int? InventoryItemId { get; set; } // WHICH physical stock (INV-2025-000123)

    public int? WorkOrderMaterialEntryId { get; set; } // WHERE it was used

    // Material Identification
    [Required]
    [StringLength(100)]
    public string MaterialCode { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // Critical Tracking Data
    [Required]
    [StringLength(100)]
    public string HeatNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? BatchNumber { get; set; }

    [StringLength(100)]
    public string? MillCertificateNumber { get; set; }

    public DateTime? MillDate { get; set; }

    [StringLength(200)]
    public string? Supplier { get; set; }

    [StringLength(100)]
    public string? SupplierBatchNumber { get; set; }

    // Chemical Composition (%)
    [Column(TypeName = "decimal(6,4)")]
    public decimal? Carbon { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Manganese { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Silicon { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Phosphorus { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Sulfur { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Chromium { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Nickel { get; set; }

    [Column(TypeName = "decimal(6,4)")]
    public decimal? Molybdenum { get; set; }

    [StringLength(1000)]
    public string? OtherElements { get; set; } // JSON for additional elements

    // Mechanical Properties
    [Column(TypeName = "decimal(10,2)")]
    public decimal? YieldStrength { get; set; } // MPa

    [Column(TypeName = "decimal(10,2)")]
    public decimal? TensileStrength { get; set; } // MPa

    [Column(TypeName = "decimal(6,2)")]
    public decimal? Elongation { get; set; } // %

    [Column(TypeName = "decimal(6,2)")]
    public decimal? ReductionOfArea { get; set; } // %

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Hardness { get; set; }

    [StringLength(20)]
    public string? HardnessScale { get; set; } // HB, HRC, HV

    // Impact Testing
    [Column(TypeName = "decimal(10,2)")]
    public decimal? ImpactValue { get; set; } // Joules

    [Column(TypeName = "decimal(6,2)")]
    public decimal? ImpactTemperature { get; set; } // Celsius

    [StringLength(50)]
    public string? ImpactType { get; set; } // Charpy, Izod

    // Traceability Chain (Genealogy)
    public int? ParentTraceId { get; set; } // For tracking material processing/transformation

    // Audit
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("CatalogueItemId")]
    public virtual CatalogueItem CatalogueItem { get; set; } = null!;

    [ForeignKey("InventoryItemId")]
    public virtual InventoryItem? InventoryItem { get; set; }

    [ForeignKey("WorkOrderMaterialEntryId")]
    public virtual WorkOrderMaterialEntry? WorkOrderMaterialEntry { get; set; }

    [ForeignKey("ParentTraceId")]
    public virtual MaterialTraceability? ParentTrace { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<MaterialTraceability> ChildTraces { get; set; } = new List<MaterialTraceability>();
    public virtual ICollection<QualityDocument> Certificates { get; set; } = new List<QualityDocument>();
}

// Quality Document - Cloud-stored certificates, reports, and quality documentation
[Table("QualityDocuments")]
public class QualityDocument
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string DocumentNumber { get; set; } = string.Empty; // DOC-2025-0001

    [Required]
    [StringLength(50)]
    public string DocumentType { get; set; } = string.Empty; // MillTestCertificate, TestCertificate, InspectionReport, etc.

    // Multi-Level Linking (can link to any level of hierarchy)
    // QDocs module uses WorkPackage (FabMate), not Package (Trace/Estimate)
    public int? OrderId { get; set; }
    public int? WorkPackageId { get; set; }
    public int? WorkOrderId { get; set; }
    public int? AssemblyId { get; set; }
    public int? MaterialTraceabilityId { get; set; }
    public int? ITPInspectionPointId { get; set; }

    // Document Metadata
    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Standard { get; set; } // AS5131, AS/NZS 1554.1, etc.

    public DateTime DocumentDate { get; set; }

    // Cloud Storage Integration
    [Required]
    [StringLength(50)]
    public string CloudProvider { get; set; } = string.Empty; // SharePoint, GoogleDrive, Dropbox

    [StringLength(500)]
    public string? CloudFileId { get; set; }

    [StringLength(1000)]
    public string? CloudFilePath { get; set; }

    [StringLength(200)]
    public string? CloudUrl { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? FileSizeKB { get; set; }

    [StringLength(50)]
    public string? MimeType { get; set; }

    // Approval Workflow
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Submitted, UnderReview, Approved, Rejected

    [StringLength(200)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public int? ApprovedById { get; set; }

    [StringLength(1000)]
    public string? ApprovalComments { get; set; }

    // Version Control
    [Required]
    public int Version { get; set; } = 1;

    public int? SupersededById { get; set; } // Link to newer version

    // Audit
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [ForeignKey("WorkPackageId")]
    public virtual WorkPackage? WorkPackage { get; set; }

    [ForeignKey("WorkOrderId")]
    public virtual WorkOrder? WorkOrder { get; set; }

    [ForeignKey("AssemblyId")]
    public virtual Assembly? Assembly { get; set; }

    [ForeignKey("MaterialTraceabilityId")]
    public virtual MaterialTraceability? MaterialTrace { get; set; }

    [ForeignKey("ITPInspectionPointId")]
    public virtual ITPInspectionPoint? ITPPoint { get; set; }

    [ForeignKey("ApprovedById")]
    public virtual User? ApprovedByUser { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("SupersededById")]
    public virtual QualityDocument? SupersededByDocument { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

// Test Result - Comprehensive test recording with all parameters
[Table("TestResults")]
public class TestResult
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TestNumber { get; set; } = string.Empty; // TST-2025-0001

    [Required]
    public int QualityDocumentId { get; set; }

    public int? ITPInspectionPointId { get; set; }

    public int? MaterialTraceabilityId { get; set; }

    // Test Identification
    [Required]
    [StringLength(50)]
    public string TestType { get; set; } = string.Empty; // Tensile, Impact, Weld, NDT, etc.

    [StringLength(200)]
    public string? TestStandard { get; set; }

    [StringLength(200)]
    public string? TestProcedure { get; set; }

    [StringLength(100)]
    public string? TestLocation { get; set; }

    // Material Link
    [StringLength(100)]
    public string? HeatNumber { get; set; }

    [StringLength(100)]
    public string? SampleId { get; set; }

    // Test Parameters (JSON - varies by test type)
    [StringLength(4000)]
    public string? TestParameters { get; set; } // JSON object

    // Environmental Conditions
    [Column(TypeName = "decimal(6,2)")]
    public decimal? AmbientTemperature { get; set; }

    [Column(TypeName = "decimal(6,2)")]
    public decimal? Humidity { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal? AtmosphericPressure { get; set; }

    [StringLength(500)]
    public string? EnvironmentalNotes { get; set; }

    // Equipment Information
    [StringLength(100)]
    public string? EquipmentId { get; set; }

    [StringLength(200)]
    public string? EquipmentName { get; set; }

    [StringLength(100)]
    public string? CalibrationCertificate { get; set; }

    public DateTime? CalibrationExpiry { get; set; }

    [StringLength(1000)]
    public string? EquipmentSettings { get; set; } // JSON

    // Tester Information
    [Required]
    public int TesterId { get; set; }

    [StringLength(100)]
    public string? TesterName { get; set; }

    [StringLength(200)]
    public string? TesterQualification { get; set; }

    [StringLength(100)]
    public string? TesterCertification { get; set; }

    public DateTime? CertificationExpiry { get; set; }

    // Witness Information
    public int? WitnessId { get; set; }

    [StringLength(100)]
    public string? WitnessName { get; set; }

    [StringLength(200)]
    public string? WitnessOrganization { get; set; }

    // Test Execution
    [Required]
    public DateTime TestStartTime { get; set; }

    [Required]
    public DateTime TestEndTime { get; set; }

    [Required]
    public int NumberOfSamples { get; set; } = 1;

    public int NumberOfRetests { get; set; } = 0;

    // Results
    [Required]
    [StringLength(20)]
    public string Result { get; set; } = "Pending"; // Pass, Fail, ConditionalPass, Retest, Pending

    [StringLength(1000)]
    public string? ResultSummary { get; set; }

    [StringLength(4000)]
    public string? DetailedResults { get; set; } // JSON for complex results

    // Audit
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CompanyId { get; set; }

    // Navigation
    [ForeignKey("QualityDocumentId")]
    public virtual QualityDocument QualityDocument { get; set; } = null!;

    [ForeignKey("ITPInspectionPointId")]
    public virtual ITPInspectionPoint? ITPPoint { get; set; }

    [ForeignKey("MaterialTraceabilityId")]
    public virtual MaterialTraceability? MaterialTrace { get; set; }

    [ForeignKey("TesterId")]
    public virtual User Tester { get; set; } = null!;

    [ForeignKey("WitnessId")]
    public virtual User? Witness { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;
}
