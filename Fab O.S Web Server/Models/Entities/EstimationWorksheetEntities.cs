using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// ==========================================
// ESTIMATION WORKSHEET SYSTEM
// Estimation → Revision → Package → Worksheet → Row
// ==========================================

/// <summary>
/// Estimation Revision - Letter-based revisions (A, B, C) for pricing iterations.
/// Each revision captures a complete snapshot of costs that can be sent to customer.
/// </summary>
[Table("EstimationRevisions")]
public class EstimationRevision
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int EstimationId { get; set; }

    /// <summary>
    /// Letter-based revision identifier (A, B, C, D...)
    /// </summary>
    [Required]
    [StringLength(10)]
    public string RevisionLetter { get; set; } = "A";

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Revision supersession tracking
    [StringLength(10)]
    public string? SupersedesLetter { get; set; }

    [StringLength(10)]
    public string? SupersededBy { get; set; }

    // Calculated totals (aggregated from packages)
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMaterialCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal OverheadPercentage { get; set; } = 15.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OverheadAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MarginPercentage { get; set; } = 20.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MarginAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // Validity
    public DateTime? ValidUntilDate { get; set; }

    // Approval workflow status
    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Draft";
    // Status values: Draft, SubmittedForReview, InReview, Approved, Rejected, Sent, Accepted, CustomerRejected, Expired

    public bool IsDeleted { get; set; } = false;

    // Approval workflow tracking
    public int? SubmittedBy { get; set; }
    public DateTime? SubmittedDate { get; set; }

    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedDate { get; set; }

    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }

    [StringLength(1000)]
    public string? ApprovalComments { get; set; }

    [StringLength(1000)]
    public string? RejectionReason { get; set; }

    // Customer communication
    public DateTime? SentDate { get; set; }
    public DateTime? CustomerResponseDate { get; set; }

    [StringLength(1000)]
    public string? CustomerComments { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("EstimationId")]
    public virtual Estimation Estimation { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("SubmittedBy")]
    public virtual User? SubmittedByUser { get; set; }

    [ForeignKey("ReviewedBy")]
    public virtual User? ReviewedByUser { get; set; }

    [ForeignKey("ApprovedBy")]
    public virtual User? ApprovedByUser { get; set; }

    public virtual ICollection<EstimationRevisionPackage> Packages { get; set; } = new List<EstimationRevisionPackage>();
}

/// <summary>
/// Estimation Revision Package - A grouping within a revision (e.g., "Building A - Level 1").
/// Links to worksheets for detailed costing.
/// </summary>
[Table("EstimationRevisionPackages")]
public class EstimationRevisionPackage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int RevisionId { get; set; }

    // Source tracking (if converted from Takeoff)
    public int? SourceTakeoffPackageId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    // Package totals (calculated from worksheets)
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCost { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal OverheadPercentage { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OverheadCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PackageTotal { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Schedule estimates
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public int? EstimatedDurationDays { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("RevisionId")]
    public virtual EstimationRevision Revision { get; set; } = null!;

    [ForeignKey("SourceTakeoffPackageId")]
    public virtual Package? SourceTakeoffPackage { get; set; }

    public virtual ICollection<EstimationWorksheet> Worksheets { get; set; } = new List<EstimationWorksheet>();

    // Backward compatibility aliases
    [NotMapped]
    public virtual EstimationRevision EstimationRevision { get => Revision; set => Revision = value; }

    [NotMapped]
    public string PackageName { get => Name; set => Name = value; }

    [NotMapped]
    public int SequenceNumber { get => SortOrder; set => SortOrder = value; }
}

// ==========================================
// WORKSHEET TEMPLATE SYSTEM
// Templates define columns, formulas, and structure
// ==========================================

/// <summary>
/// Worksheet Template - User-created templates for different costing types.
/// Examples: Material Costs, Labor Costs, Welding Costs, Custom sheets.
/// </summary>
[Table("EstimationWorksheetTemplates")]
public class EstimationWorksheetTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Worksheet type category: Material, Labor, Welding, Overhead, Summary, Custom
    /// </summary>
    [Required]
    [StringLength(50)]
    public string WorksheetType { get; set; } = "Custom";

    // Template scope
    public bool IsSystemTemplate { get; set; } = false;
    public bool IsCompanyDefault { get; set; } = false;
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Whether this is the default template for its worksheet type
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public int? CreatedBy { get; set; }

    // Configuration options
    public bool AllowColumnReorder { get; set; } = true;
    public bool AllowAddRows { get; set; } = true;
    public bool AllowDeleteRows { get; set; } = true;
    public bool ShowRowNumbers { get; set; } = true;
    public bool ShowColumnTotals { get; set; } = true;

    /// <summary>
    /// Default formulas and calculations (JSON format).
    /// Defines worksheet-level calculations and cross-worksheet references.
    /// </summary>
    public string? DefaultFormulas { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; set; }

    public virtual ICollection<EstimationWorksheetColumn> Columns { get; set; } = new List<EstimationWorksheetColumn>();
    public virtual ICollection<TemplateImportMapping> ImportMappings { get; set; } = new List<TemplateImportMapping>();
}

/// <summary>
/// Template Import Mapping - Defines how to map columns from imported files to template columns.
/// Allows users to upload a sample file, map source columns to template columns, and save for future imports.
/// </summary>
[Table("TemplateImportMappings")]
public class TemplateImportMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorksheetTemplateId { get; set; }

    /// <summary>
    /// Name of this mapping configuration (allows multiple mappings per template)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Original sample file name for reference
    /// </summary>
    [StringLength(255)]
    public string? SampleFileName { get; set; }

    /// <summary>
    /// File type of the sample: xlsx, xls, csv, pdf
    /// </summary>
    [StringLength(50)]
    public string? SampleFileType { get; set; }

    /// <summary>
    /// Which row contains headers (0-based index)
    /// </summary>
    public int HeaderRowIndex { get; set; } = 0;

    /// <summary>
    /// Which row data starts from (0-based index)
    /// </summary>
    public int DataStartRowIndex { get; set; } = 1;

    /// <summary>
    /// Sheet name for Excel files with multiple sheets
    /// </summary>
    [StringLength(50)]
    public string? SheetName { get; set; }

    /// <summary>
    /// Column mappings stored as JSON array.
    /// Structure: [{ sourceColumnIndex, sourceColumnName, targetColumnKey, targetColumnName, transformType, defaultValue }]
    /// </summary>
    public string ColumnMappingsJson { get; set; } = "[]";

    /// <summary>
    /// Whether this is the default mapping for the template
    /// </summary>
    public bool IsDefault { get; set; } = true;

    public bool IsDeleted { get; set; } = false;

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    // Navigation
    [ForeignKey("WorksheetTemplateId")]
    public virtual EstimationWorksheetTemplate WorksheetTemplate { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedBy")]
    public virtual User? ModifiedByUser { get; set; }
}

/// <summary>
/// Worksheet Column Definition - Defines a column in a worksheet template.
/// Supports various data types, formulas, and catalogue integration.
/// </summary>
[Table("EstimationWorksheetColumns")]
public class EstimationWorksheetColumn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorksheetTemplateId { get; set; }

    /// <summary>
    /// Unique key for this column (e.g., "description", "qty", "unit_cost")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ColumnKey { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in column header
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Data type: Text, Number, Currency, Percentage, Date, Checkbox, Select, Computed
    /// </summary>
    [Required]
    [StringLength(30)]
    public string DataType { get; set; } = "Text";

    // Column display configuration
    public int Width { get; set; } = 100;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsRequired { get; set; } = false;
    public bool IsEditable { get; set; } = true;
    public bool IsFrozen { get; set; } = false;

    [StringLength(20)]
    public string? FreezePosition { get; set; } // Left, Right

    [StringLength(100)]
    public string? CssClass { get; set; }

    [StringLength(50)]
    public string? TextAlign { get; set; } // left, center, right

    // Number/Currency configuration
    public int? DecimalPlaces { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MinValue { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? MaxValue { get; set; }

    [StringLength(50)]
    public string? NumberFormat { get; set; } // #,##0.00

    [StringLength(10)]
    public string? CurrencySymbol { get; set; } // $, AU$

    // Select/Dropdown configuration (JSON array)
    public string? SelectOptions { get; set; }

    // Formula configuration for computed columns
    /// <summary>
    /// Formula expression for computed columns.
    /// Supports: column references (qty, unit_cost), operators (+, -, *, /),
    /// functions (SUM, AVG, MIN, MAX, IF), cross-worksheet references (MaterialSheet.TotalCost)
    /// </summary>
    [StringLength(500)]
    public string? Formula { get; set; }

    /// <summary>
    /// Default value for this column
    /// </summary>
    [StringLength(200)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Show column total in footer (SUM, AVG, COUNT, MIN, MAX)
    /// </summary>
    public bool ShowColumnTotal { get; set; } = false;

    [StringLength(20)]
    public string? ColumnTotalFunction { get; set; } // SUM, AVG, COUNT, MIN, MAX

    // Catalogue integration
    public bool LinkToCatalogue { get; set; } = false;

    [StringLength(50)]
    public string? CatalogueField { get; set; }

    public bool AutoPopulateFromCatalogue { get; set; } = false;

    // Lookup configuration (for Lookup data type)
    [StringLength(100)]
    public string? LookupEntityType { get; set; } // CatalogueItem, Customer, Contact, etc.

    [StringLength(50)]
    public string? LookupDisplayField { get; set; } // Which field to display

    [StringLength(500)]
    public string? LookupFilter { get; set; } // Optional filter criteria (JSON)

    // Person configuration (for Person data type)
    [StringLength(50)]
    public string? PersonSource { get; set; } // Users, Contacts, Both

    // Image configuration (for Image data type)
    public int? MaxImageSizeKb { get; set; }

    [StringLength(100)]
    public string? AllowedImageTypes { get; set; } // jpg,png,gif

    // Location configuration (for Location data type)
    [StringLength(30)]
    public string? LocationFormat { get; set; } // Coordinates, Address, Both

    // Multiline text configuration
    public int? MultilineRows { get; set; } // Default row count for multiline text

    public int? MaxLength { get; set; } // Maximum character length

    // Validation
    [StringLength(500)]
    public string? ValidationRegex { get; set; }

    [StringLength(200)]
    public string? ValidationMessage { get; set; }

    // Help text
    [StringLength(500)]
    public string? HelpText { get; set; }

    [StringLength(200)]
    public string? Placeholder { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Backward compatibility aliases for property name variations
    [NotMapped]
    public string ColumnName { get => DisplayName; set => DisplayName = value; }

    [NotMapped]
    public int SortOrder { get => DisplayOrder; set => DisplayOrder = value; }

    [NotMapped]
    public bool IsHidden { get => !IsVisible; set => IsVisible = !value; }

    [NotMapped]
    public bool IsReadOnly { get => !IsEditable; set => IsEditable = !value; }

    [NotMapped]
    public int? Precision { get => DecimalPlaces; set => DecimalPlaces = value; }

    [NotMapped]
    public int TemplateId { get => WorksheetTemplateId; set => WorksheetTemplateId = value; }

    // Navigation
    [ForeignKey("WorksheetTemplateId")]
    public virtual EstimationWorksheetTemplate WorksheetTemplate { get; set; } = null!;
}

// ==========================================
// WORKSHEET INSTANCE & DATA
// ==========================================

/// <summary>
/// Estimation Worksheet - An instance of a worksheet template within a package.
/// Contains the actual row data for costing.
/// </summary>
[Table("EstimationWorksheets")]
public class EstimationWorksheet
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int PackageId { get; set; }

    public int? TemplateId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string WorksheetType { get; set; } = "Custom";

    public int SortOrder { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Worksheet totals (calculated from rows)
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMaterialCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalLaborCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Column configuration overrides from template (JSON).
    /// Allows per-worksheet customization of column visibility, order, width.
    /// </summary>
    public string? ColumnConfiguration { get; set; }

    /// <summary>
    /// Column totals calculated values (JSON).
    /// Stores computed column totals for quick access.
    /// </summary>
    public string? ColumnTotals { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }

    // Navigation properties
    // Backward compatibility alias for SortOrder
    [NotMapped]
    public int DisplayOrder { get => SortOrder; set => SortOrder = value; }

    // Backward compatibility alias for PackageId
    [NotMapped]
    public int EstimationRevisionPackageId { get => PackageId; set => PackageId = value; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("PackageId")]
    public virtual EstimationRevisionPackage Package { get; set; } = null!;

    // Alias for Package navigation
    [NotMapped]
    public virtual EstimationRevisionPackage EstimationRevisionPackage { get => Package; set => Package = value; }

    [ForeignKey("TemplateId")]
    public virtual EstimationWorksheetTemplate? Template { get; set; }

    // Alias for Template navigation
    [NotMapped]
    public virtual EstimationWorksheetTemplate? WorksheetTemplate { get => Template; set => Template = value; }

    // Alias for TemplateId
    [NotMapped]
    public int? WorksheetTemplateId { get => TemplateId; set => TemplateId = value; }

    public virtual ICollection<EstimationWorksheetInstanceColumn> Columns { get; set; } = new List<EstimationWorksheetInstanceColumn>();
    public virtual ICollection<EstimationWorksheetRow> Rows { get; set; } = new List<EstimationWorksheetRow>();
    public virtual ICollection<EstimationWorksheetChange> Changes { get; set; } = new List<EstimationWorksheetChange>();
}

/// <summary>
/// Worksheet Instance Column - A column definition specific to a worksheet instance.
/// Created when a worksheet is instantiated from a template or added manually.
/// </summary>
[Table("EstimationWorksheetInstanceColumns")]
public class EstimationWorksheetInstanceColumn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorksheetId { get; set; }

    [Required]
    [StringLength(50)]
    public string ColumnKey { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ColumnName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string DataType { get; set; } = "Text";

    public int Width { get; set; } = 100;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public bool IsFrozen { get; set; } = false;
    public bool IsHidden { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    [StringLength(500)]
    public string? Formula { get; set; }

    [StringLength(200)]
    public string? DefaultValue { get; set; }

    public string? SelectOptions { get; set; }

    public int? Precision { get; set; }

    // Catalogue integration
    public bool LinkToCatalogue { get; set; } = false;

    [StringLength(50)]
    public string? CatalogueField { get; set; }

    public bool AutoPopulateFromCatalogue { get; set; } = false;

    // Navigation
    [ForeignKey("WorksheetId")]
    public virtual EstimationWorksheet Worksheet { get; set; } = null!;
}

/// <summary>
/// Worksheet Row - A single row of data in a worksheet.
/// Dynamic column values are stored as JSON for flexibility.
/// </summary>
[Table("EstimationWorksheetRows")]
public class EstimationWorksheetRow
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int WorksheetId { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Dynamic column values stored as JSON.
    /// Example: {"description": "UC 310x97", "qty": 5, "unit_cost": 450.00, "total_cost": 2250.00}
    /// </summary>
    [Required]
    public string RowData { get; set; } = "{}";

    /// <summary>
    /// Calculated total for this row (denormalized for performance)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CalculatedTotal { get; set; }

    // Catalogue link
    public int? CatalogueItemId { get; set; }

    /// <summary>
    /// Catalogue matching status: Matched, Unmatched, ManuallyAssigned, NotApplicable
    /// </summary>
    [StringLength(30)]
    public string? MatchStatus { get; set; }

    /// <summary>
    /// Confidence score for auto-matching (0.0 to 1.0)
    /// </summary>
    [Column(TypeName = "decimal(3,2)")]
    public decimal? MatchConfidence { get; set; }

    // Row grouping support
    public int? ParentRowId { get; set; }
    public bool IsGroupHeader { get; set; } = false;

    [StringLength(200)]
    public string? GroupName { get; set; }

    public bool IsExpanded { get; set; } = true;

    // Row state
    public bool IsDeleted { get; set; } = false;

    [StringLength(500)]
    public string? Notes { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }

    // Backward compatibility aliases
    public int RowNumber { get => SortOrder; set => SortOrder = value; }
    public int EstimationWorksheetId { get => WorksheetId; set => WorksheetId = value; }
    public DateTime? LastModified { get => ModifiedDate; set => ModifiedDate = value; }
    public int? LastModifiedBy { get => ModifiedBy; set => ModifiedBy = value; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("WorksheetId")]
    public virtual EstimationWorksheet Worksheet { get; set; } = null!;

    [ForeignKey("CatalogueItemId")]
    public virtual CatalogueItem? CatalogueItem { get; set; }

    [ForeignKey("ParentRowId")]
    public virtual EstimationWorksheetRow? ParentRow { get; set; }

    public virtual ICollection<EstimationWorksheetRow> ChildRows { get; set; } = new List<EstimationWorksheetRow>();
}

/// <summary>
/// Worksheet Change - Audit trail for worksheet modifications.
/// Tracks who changed what and when for undo/redo support.
/// </summary>
[Table("EstimationWorksheetChanges")]
public class EstimationWorksheetChange
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorksheetId { get; set; }

    public int? RowId { get; set; }

    [Required]
    [StringLength(20)]
    public string ChangeType { get; set; } = string.Empty; // Added, Modified, Deleted

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public int? ChangedBy { get; set; }

    [Required]
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("WorksheetId")]
    public virtual EstimationWorksheet Worksheet { get; set; } = null!;

    [ForeignKey("ChangedBy")]
    public virtual User? User { get; set; }
}

// ==========================================
// ENUMS FOR ESTIMATION WORKFLOW
// ==========================================

public enum EstimationRevisionStatus
{
    Draft,
    SubmittedForReview,
    InReview,
    Approved,
    Rejected,
    Sent,
    Accepted,
    CustomerRejected,
    Expired
}

public enum WorksheetColumnDataType
{
    Text,
    MultilineText,
    Number,
    Currency,
    Percentage,
    Date,
    DateTime,
    Checkbox,
    Choice,         // Single select dropdown
    MultiChoice,    // Multi-select
    Image,          // Image attachment
    Location,       // GPS/Address
    Lookup,         // Reference to another entity
    Person,         // User/Contact reference
    Computed,       // Formula-based
    Catalogue,      // From CatalogueItem field
    Custom
}

public enum WorksheetType
{
    Material,
    Labor,
    Welding,
    Overhead,
    Summary,
    Custom
}

public enum ColumnTotalFunction
{
    Sum,
    Average,
    Count,
    Min,
    Max
}
