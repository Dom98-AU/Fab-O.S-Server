using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities.Forms;

#region Enums

/// <summary>
/// Module context for forms - determines which modules can access this form
/// </summary>
public enum FormModuleContext
{
    Estimate = 0,
    FabMate = 1,
    QDocs = 2,
    Assets = 3
}

/// <summary>
/// Data types for form fields
/// </summary>
public enum FormFieldDataType
{
    Text = 0,
    MultilineText = 1,
    Number = 2,
    Currency = 3,
    Percentage = 4,
    Date = 5,
    DateTime = 6,
    Checkbox = 7,
    Choice = 8,
    MultiChoice = 9,
    Signature = 10,
    Photo = 11,
    Location = 12,
    PassFail = 13,
    Computed = 14,
    WorksheetLink = 15,

    // Header/Footer special fields
    PageNumber = 16,       // "Page X of Y" - auto-generated page numbers
    CompanyLogo = 17,      // Company logo image
    DocumentTitle = 18,    // Form/Document title
    CurrentDate = 19,      // Auto-populated current date
    FormNumber = 20,       // Auto-generated form number (e.g., "ITP-001")

    // Decorative elements
    DecorativeIcon = 21,   // Non-data decorative icons (phone, mail, location, etc.)
    DecorativeBanner = 22, // Horizontal divider/banner element
    DecorativeImage = 23   // Custom image placeholder
}

/// <summary>
/// Status of a form instance through the approval workflow
/// </summary>
public enum FormInstanceStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4
}

#endregion

#region FormTemplate

/// <summary>
/// Form template definition - reusable form structure
/// </summary>
[Table("FormTemplates")]
public class FormTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    /// <summary>
    /// Template name (e.g., "Inspection Test Plan", "Asset Repair Form")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the form template
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Module context - which module(s) can use this form
    /// </summary>
    public FormModuleContext ModuleContext { get; set; }

    /// <summary>
    /// Form type identifier (e.g., "ITP", "RepairForm", "PreContract")
    /// </summary>
    [MaxLength(50)]
    public string? FormType { get; set; }

    /// <summary>
    /// System templates cannot be deleted by users
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Company default template for this module/type
    /// </summary>
    public bool IsCompanyDefault { get; set; }

    /// <summary>
    /// Whether the template is published and available for use
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Version number for tracking template changes
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Prefix for form numbers (e.g., "ITP" generates "ITP-001")
    /// </summary>
    [MaxLength(20)]
    public string? NumberPrefix { get; set; }

    /// <summary>
    /// Whether to show section headers in the form
    /// </summary>
    public bool ShowSectionHeaders { get; set; } = true;

    /// <summary>
    /// Whether to allow adding notes to the form
    /// </summary>
    public bool AllowNotes { get; set; } = true;

    // Page Size Configuration

    /// <summary>
    /// Page width in millimeters (default A4: 210mm)
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal PageWidthMm { get; set; } = 210;

    /// <summary>
    /// Page height in millimeters (default A4: 297mm)
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal PageHeightMm { get; set; } = 297;

    /// <summary>
    /// Page orientation: "Portrait" or "Landscape"
    /// </summary>
    [MaxLength(20)]
    public string PageOrientation { get; set; } = "Portrait";

    // Page Margins

    /// <summary>
    /// Top margin in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal MarginTopMm { get; set; } = 20;

    /// <summary>
    /// Right margin in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal MarginRightMm { get; set; } = 15;

    /// <summary>
    /// Bottom margin in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal MarginBottomMm { get; set; } = 20;

    /// <summary>
    /// Left margin in millimeters
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal MarginLeftMm { get; set; } = 15;

    // Soft delete
    public bool IsDeleted { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserId")]
    public virtual User? ModifiedByUser { get; set; }

    public virtual ICollection<FormTemplateSection> Sections { get; set; } = new List<FormTemplateSection>();
    public virtual ICollection<FormTemplateField> Fields { get; set; } = new List<FormTemplateField>();
    public virtual ICollection<FormInstance> Instances { get; set; } = new List<FormInstance>();
}

#endregion

#region FormTemplateSection

/// <summary>
/// Section within a form template - provides grid-based layout for fields
/// </summary>
[Table("FormTemplateSections")]
public class FormTemplateSection
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FormTemplateId { get; set; }

    /// <summary>
    /// Section name (e.g., "General Information", "Inspection Details")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Layout type: "1-col", "2-col-equal", "2-col-left", "2-col-right", "3-col-equal"
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string LayoutType { get; set; } = "1-col";

    /// <summary>
    /// Display order of the section within the template
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether section should start on a new page (page break before)
    /// </summary>
    public bool PageBreakBefore { get; set; }

    /// <summary>
    /// Whether section should NOT be split across pages (keep together)
    /// </summary>
    public bool KeepTogether { get; set; }

    // Styling properties

    /// <summary>
    /// Background color (e.g., "#f8f9fa", "transparent")
    /// </summary>
    [MaxLength(20)]
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Border color (e.g., "#dee2e6")
    /// </summary>
    [MaxLength(20)]
    public string? BorderColor { get; set; }

    /// <summary>
    /// Header background color
    /// </summary>
    [MaxLength(20)]
    public string? HeaderBackgroundColor { get; set; }

    /// <summary>
    /// Header text color
    /// </summary>
    [MaxLength(20)]
    public string? HeaderTextColor { get; set; }

    /// <summary>
    /// Border width in pixels (0-4)
    /// </summary>
    public int? BorderWidth { get; set; }

    /// <summary>
    /// Border radius in pixels (0-16)
    /// </summary>
    public int? BorderRadius { get; set; }

    /// <summary>
    /// Padding in pixels (0-32) - Legacy single value
    /// </summary>
    public int? Padding { get; set; }

    /// <summary>
    /// Top padding in pixels
    /// </summary>
    public int? PaddingTop { get; set; }

    /// <summary>
    /// Right padding in pixels
    /// </summary>
    public int? PaddingRight { get; set; }

    /// <summary>
    /// Bottom padding in pixels
    /// </summary>
    public int? PaddingBottom { get; set; }

    /// <summary>
    /// Left padding in pixels
    /// </summary>
    public int? PaddingLeft { get; set; }

    /// <summary>
    /// Horizontal content alignment: "left", "center", "right"
    /// </summary>
    [MaxLength(20)]
    public string? ContentAlignHorizontal { get; set; }

    /// <summary>
    /// Vertical content alignment: "top", "middle", "bottom"
    /// </summary>
    [MaxLength(20)]
    public string? ContentAlignVertical { get; set; }

    /// <summary>
    /// Whether section can be collapsed/expanded
    /// </summary>
    public bool IsCollapsible { get; set; }

    /// <summary>
    /// Whether section is collapsed by default
    /// </summary>
    public bool IsCollapsedByDefault { get; set; }

    /// <summary>
    /// Indicates this section is a page header (repeats on each printed page)
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary>
    /// Indicates this section is a page footer (repeats on each printed page)
    /// </summary>
    public bool IsFooter { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("FormTemplateId")]
    public virtual FormTemplate? FormTemplate { get; set; }

    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserId")]
    public virtual User? ModifiedByUser { get; set; }

    public virtual ICollection<FormTemplateField> Fields { get; set; } = new List<FormTemplateField>();
}

#endregion

#region FormTemplateField

/// <summary>
/// Field definition within a form template
/// </summary>
[Table("FormTemplateFields")]
public class FormTemplateField
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FormTemplateId { get; set; }

    /// <summary>
    /// Unique field key identifier (e.g., "inspector_name", "inspection_date")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown to users
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the field
    /// </summary>
    public FormFieldDataType DataType { get; set; }

    /// <summary>
    /// Display order within section
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Foreign key to FormTemplateSection (new section-based architecture)
    /// </summary>
    public int? FormTemplateSectionId { get; set; }

    /// <summary>
    /// Column index within the section (0-based)
    /// e.g., for 3-col layout: 0=left, 1=middle, 2=right
    /// </summary>
    public int ColumnIndex { get; set; } = 0;

    /// <summary>
    /// Row index within the column (for ordering fields in same column)
    /// </summary>
    public int RowIndex { get; set; } = 0;

    /// <summary>
    /// Section name for grouping fields (DEPRECATED - use FormTemplateSectionId)
    /// Kept for backward compatibility during migration
    /// </summary>
    [MaxLength(100)]
    public string? SectionName { get; set; }

    /// <summary>
    /// Section display order (DEPRECATED - use FormTemplateSection.DisplayOrder)
    /// Kept for backward compatibility during migration
    /// </summary>
    public int SectionOrder { get; set; }

    /// <summary>
    /// Field width: "full", "half", "third" (DEPRECATED - layout controlled by section)
    /// Kept for backward compatibility during migration
    /// </summary>
    [MaxLength(20)]
    public string Width { get; set; } = "full";

    // Spacing Properties

    /// <summary>
    /// Top padding in pixels
    /// </summary>
    public int? PaddingTop { get; set; }

    /// <summary>
    /// Right padding in pixels
    /// </summary>
    public int? PaddingRight { get; set; }

    /// <summary>
    /// Bottom padding in pixels
    /// </summary>
    public int? PaddingBottom { get; set; }

    /// <summary>
    /// Left padding in pixels
    /// </summary>
    public int? PaddingLeft { get; set; }

    /// <summary>
    /// Top margin in pixels
    /// </summary>
    public int? MarginTop { get; set; }

    /// <summary>
    /// Bottom margin in pixels
    /// </summary>
    public int? MarginBottom { get; set; }

    // Layout Properties

    /// <summary>
    /// Text alignment: "left", "center", "right", "justify"
    /// </summary>
    [MaxLength(20)]
    public string? TextAlign { get; set; }

    /// <summary>
    /// Fixed height in pixels (null = auto height)
    /// </summary>
    public int? FixedHeight { get; set; }

    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether the field is visible
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Whether the field is read-only
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Default value for the field
    /// </summary>
    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Placeholder text for input fields
    /// </summary>
    [MaxLength(200)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Help text shown below the field
    /// </summary>
    [MaxLength(500)]
    public string? HelpText { get; set; }

    /// <summary>
    /// Validation regex pattern
    /// </summary>
    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    /// <summary>
    /// Validation error message
    /// </summary>
    [MaxLength(200)]
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Options for Choice/MultiChoice fields (JSON array)
    /// </summary>
    public string? SelectOptions { get; set; }

    /// <summary>
    /// Formula for Computed fields
    /// </summary>
    [MaxLength(1000)]
    public string? Formula { get; set; }

    /// <summary>
    /// Minimum value for Number fields
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Maximum value for Number fields
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Decimal places for Number/Currency fields
    /// </summary>
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// Currency symbol for Currency fields
    /// </summary>
    [MaxLength(10)]
    public string? CurrencySymbol { get; set; }

    /// <summary>
    /// Linked worksheet template ID for WorksheetLink fields
    /// </summary>
    public int? LinkedWorksheetTemplateId { get; set; }

    /// <summary>
    /// Maximum photo count for Photo fields
    /// </summary>
    public int? MaxPhotos { get; set; }

    /// <summary>
    /// Whether to require GPS location for Photo fields
    /// </summary>
    public bool RequirePhotoLocation { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }

    // Navigation
    [ForeignKey("FormTemplateId")]
    public virtual FormTemplate? FormTemplate { get; set; }

    [ForeignKey("FormTemplateSectionId")]
    public virtual FormTemplateSection? Section { get; set; }

    [ForeignKey("LinkedWorksheetTemplateId")]
    public virtual EstimationWorksheetTemplate? LinkedWorksheetTemplate { get; set; }

    public virtual ICollection<FormInstanceValue> Values { get; set; } = new List<FormInstanceValue>();
}

#endregion

#region FormInstance

/// <summary>
/// Filled form instance
/// </summary>
[Table("FormInstances")]
public class FormInstance
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public int FormTemplateId { get; set; }

    /// <summary>
    /// Auto-generated form number (e.g., "ITP-001", "FORM-001")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FormNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status in the approval workflow
    /// </summary>
    public FormInstanceStatus Status { get; set; } = FormInstanceStatus.Draft;

    /// <summary>
    /// Type of linked entity (e.g., "WorkOrder", "Asset", "Estimation")
    /// </summary>
    [MaxLength(50)]
    public string? LinkedEntityType { get; set; }

    /// <summary>
    /// ID of the linked entity
    /// </summary>
    public int? LinkedEntityId { get; set; }

    /// <summary>
    /// Display name of the linked entity (for quick reference)
    /// </summary>
    [MaxLength(200)]
    public string? LinkedEntityDisplay { get; set; }

    /// <summary>
    /// General notes for the form
    /// </summary>
    public string? Notes { get; set; }

    // Submission tracking
    public int? SubmittedByUserId { get; set; }
    public DateTime? SubmittedDate { get; set; }

    // Review tracking
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedDate { get; set; }

    // Approval tracking
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Reason for rejection (if rejected)
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedByUserId { get; set; }

    // Navigation properties
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    [ForeignKey("FormTemplateId")]
    public virtual FormTemplate? FormTemplate { get; set; }

    [ForeignKey("CreatedByUserId")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserId")]
    public virtual User? ModifiedByUser { get; set; }

    [ForeignKey("SubmittedByUserId")]
    public virtual User? SubmittedByUser { get; set; }

    [ForeignKey("ReviewedByUserId")]
    public virtual User? ReviewedByUser { get; set; }

    [ForeignKey("ApprovedByUserId")]
    public virtual User? ApprovedByUser { get; set; }

    public virtual ICollection<FormInstanceValue> Values { get; set; } = new List<FormInstanceValue>();
    public virtual ICollection<FormInstanceAttachment> Attachments { get; set; } = new List<FormInstanceAttachment>();
}

#endregion

#region FormInstanceValue

/// <summary>
/// Field value within a form instance
/// </summary>
[Table("FormInstanceValues")]
public class FormInstanceValue
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FormInstanceId { get; set; }

    [Required]
    public int FormTemplateFieldId { get; set; }

    /// <summary>
    /// Field key for quick lookup (denormalized from template)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Text value storage
    /// </summary>
    public string? TextValue { get; set; }

    /// <summary>
    /// Numeric value storage
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal? NumberValue { get; set; }

    /// <summary>
    /// Date value storage
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// Boolean value storage
    /// </summary>
    public bool? BoolValue { get; set; }

    /// <summary>
    /// Complex JSON value storage (for location, multi-choice, etc.)
    /// </summary>
    public string? JsonValue { get; set; }

    /// <summary>
    /// Signature data URL (base64 PNG)
    /// </summary>
    public string? SignatureDataUrl { get; set; }

    /// <summary>
    /// Pass/Fail value for PassFail fields
    /// </summary>
    public bool? PassFailValue { get; set; }

    /// <summary>
    /// Comment for Pass/Fail fields
    /// </summary>
    [MaxLength(1000)]
    public string? PassFailComment { get; set; }

    // Audit
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedByUserId { get; set; }

    // Navigation
    [ForeignKey("FormInstanceId")]
    public virtual FormInstance? FormInstance { get; set; }

    [ForeignKey("FormTemplateFieldId")]
    public virtual FormTemplateField? FormTemplateField { get; set; }

    [ForeignKey("ModifiedByUserId")]
    public virtual User? ModifiedByUser { get; set; }

    public virtual ICollection<FormInstanceAttachment> Attachments { get; set; } = new List<FormInstanceAttachment>();
}

#endregion

#region FormInstanceAttachment

/// <summary>
/// Attachment (photo/file) for a form instance
/// </summary>
[Table("FormInstanceAttachments")]
public class FormInstanceAttachment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FormInstanceId { get; set; }

    /// <summary>
    /// Link to specific field value (optional - for field-level attachments)
    /// </summary>
    public int? FormInstanceValueId { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (e.g., "image/jpeg", "application/pdf")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Storage provider (Azure, SharePoint, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? StorageProvider { get; set; }

    /// <summary>
    /// Path/URL in storage
    /// </summary>
    [MaxLength(1000)]
    public string? StoragePath { get; set; }

    /// <summary>
    /// Thumbnail path for images
    /// </summary>
    [MaxLength(1000)]
    public string? ThumbnailPath { get; set; }

    /// <summary>
    /// GPS latitude (for geotagged photos)
    /// </summary>
    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// GPS longitude (for geotagged photos)
    /// </summary>
    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Caption or description
    /// </summary>
    [MaxLength(500)]
    public string? Caption { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }

    // Audit
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public int? UploadedByUserId { get; set; }

    // Navigation
    [ForeignKey("FormInstanceId")]
    public virtual FormInstance? FormInstance { get; set; }

    [ForeignKey("FormInstanceValueId")]
    public virtual FormInstanceValue? FormInstanceValue { get; set; }

    [ForeignKey("UploadedByUserId")]
    public virtual User? UploadedByUser { get; set; }
}

#endregion
