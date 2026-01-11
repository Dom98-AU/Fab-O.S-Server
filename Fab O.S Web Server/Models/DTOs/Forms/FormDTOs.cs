using FabOS.WebServer.Models.Entities.Forms;

namespace FabOS.WebServer.Models.DTOs.Forms;

#region FormTemplate DTOs

/// <summary>
/// Summary DTO for form template list views
/// </summary>
public class FormTemplateSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FormModuleContext ModuleContext { get; set; }
    public string ModuleContextName => ModuleContext.ToString();
    public string? FormType { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsCompanyDefault { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; }
    public int FieldCount { get; set; }
    public int InstanceCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Page Size
    public decimal PageWidthMm { get; set; }
    public decimal PageHeightMm { get; set; }
    public string PageOrientation { get; set; } = "Portrait";
}

/// <summary>
/// Full DTO for form template with all fields
/// </summary>
public class FormTemplateDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FormModuleContext ModuleContext { get; set; }
    public string ModuleContextName => ModuleContext.ToString();
    public string? FormType { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsCompanyDefault { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; }
    public string? NumberPrefix { get; set; }
    public bool ShowSectionHeaders { get; set; }
    public bool AllowNotes { get; set; }

    // Page Size
    public decimal PageWidthMm { get; set; }
    public decimal PageHeightMm { get; set; }
    public string PageOrientation { get; set; } = "Portrait";

    // Page Margins (in mm)
    public decimal MarginTopMm { get; set; } = 20;
    public decimal MarginRightMm { get; set; } = 15;
    public decimal MarginBottomMm { get; set; } = 20;
    public decimal MarginLeftMm { get; set; } = 15;

    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public string? ModifiedByName { get; set; }
    public List<FormTemplateSectionDto> Sections { get; set; } = new();
    public List<FormTemplateFieldDto> Fields { get; set; } = new();
}

/// <summary>
/// Request to create a new form template
/// </summary>
public class CreateFormTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FormModuleContext ModuleContext { get; set; }
    public string? FormType { get; set; }
    public bool IsCompanyDefault { get; set; }
    public string? NumberPrefix { get; set; }
    public bool ShowSectionHeaders { get; set; } = true;
    public bool AllowNotes { get; set; } = true;

    // Page Size (default A4 Portrait)
    public decimal PageWidthMm { get; set; } = 210;
    public decimal PageHeightMm { get; set; } = 297;
    public string PageOrientation { get; set; } = "Portrait";

    // Page Margins (in mm)
    public decimal MarginTopMm { get; set; } = 20;
    public decimal MarginRightMm { get; set; } = 15;
    public decimal MarginBottomMm { get; set; } = 20;
    public decimal MarginLeftMm { get; set; } = 15;

    public List<CreateFormTemplateFieldRequest>? Fields { get; set; }
}

/// <summary>
/// Request to update a form template
/// </summary>
public class UpdateFormTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FormModuleContext ModuleContext { get; set; }
    public string? FormType { get; set; }
    public bool IsCompanyDefault { get; set; }
    public bool IsPublished { get; set; }
    public string? NumberPrefix { get; set; }
    public bool ShowSectionHeaders { get; set; }
    public bool AllowNotes { get; set; }

    // Page Size
    public decimal PageWidthMm { get; set; } = 210;
    public decimal PageHeightMm { get; set; } = 297;
    public string PageOrientation { get; set; } = "Portrait";

    // Page Margins (in mm)
    public decimal MarginTopMm { get; set; } = 20;
    public decimal MarginRightMm { get; set; } = 15;
    public decimal MarginBottomMm { get; set; } = 20;
    public decimal MarginLeftMm { get; set; } = 15;

    public List<UpdateFormTemplateFieldRequest>? Fields { get; set; }
    public List<UpdateFormTemplateSectionRequest>? Sections { get; set; }
}

#endregion

#region FormTemplateSection DTOs

/// <summary>
/// DTO for form template section
/// </summary>
public class FormTemplateSectionDto
{
    public int Id { get; set; }
    public int FormTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LayoutType { get; set; } = "1-col";
    public int DisplayOrder { get; set; }

    // Page behavior
    public bool PageBreakBefore { get; set; }
    public bool KeepTogether { get; set; }

    // Styling
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public string? HeaderBackgroundColor { get; set; }
    public string? HeaderTextColor { get; set; }
    public int? BorderWidth { get; set; }
    public int? BorderRadius { get; set; }

    // Individual padding (in pixels)
    public int? Padding { get; set; }  // Legacy - single padding value
    public int? PaddingTop { get; set; }
    public int? PaddingRight { get; set; }
    public int? PaddingBottom { get; set; }
    public int? PaddingLeft { get; set; }

    // Content alignment
    public string? ContentAlignHorizontal { get; set; }  // "left", "center", "right"
    public string? ContentAlignVertical { get; set; }    // "top", "middle", "bottom"

    public bool IsCollapsible { get; set; }
    public bool IsCollapsedByDefault { get; set; }

    // Header/Footer flags
    /// <summary>
    /// Indicates this section is a page header (repeats on each page)
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary>
    /// Indicates this section is a page footer (repeats on each page)
    /// </summary>
    public bool IsFooter { get; set; }

    /// <summary>
    /// Computed column count based on layout type
    /// </summary>
    public int ColumnCount => LayoutType switch
    {
        "1-col" => 1,
        "2-col-equal" or "2-col-left" or "2-col-right" => 2,
        "3-col-equal" => 3,
        _ => 1
    };

    /// <summary>
    /// CSS grid-template-columns value for this layout
    /// </summary>
    public string GridTemplateColumns => LayoutType switch
    {
        "1-col" => "1fr",
        "2-col-equal" => "1fr 1fr",
        "2-col-left" => "2fr 1fr",
        "2-col-right" => "1fr 2fr",
        "3-col-equal" => "1fr 1fr 1fr",
        _ => "1fr"
    };

    /// <summary>
    /// Fields in this section
    /// </summary>
    public List<FormTemplateFieldDto> Fields { get; set; } = new();
}

/// <summary>
/// Request to create a new form template section
/// </summary>
public class CreateFormTemplateSectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string LayoutType { get; set; } = "1-col";
    public int DisplayOrder { get; set; }

    // Page behavior
    public bool PageBreakBefore { get; set; }
    public bool KeepTogether { get; set; } = true;

    // Styling
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public string? HeaderBackgroundColor { get; set; }
    public string? HeaderTextColor { get; set; }
    public int? BorderWidth { get; set; }
    public int? BorderRadius { get; set; }

    // Individual padding (in pixels)
    public int? Padding { get; set; }  // Legacy - single padding value
    public int? PaddingTop { get; set; }
    public int? PaddingRight { get; set; }
    public int? PaddingBottom { get; set; }
    public int? PaddingLeft { get; set; }

    // Content alignment
    public string? ContentAlignHorizontal { get; set; }  // "left", "center", "right"
    public string? ContentAlignVertical { get; set; }    // "top", "middle", "bottom"

    public bool IsCollapsible { get; set; }
    public bool IsCollapsedByDefault { get; set; }

    // Header/Footer flags
    public bool IsHeader { get; set; }
    public bool IsFooter { get; set; }
}

/// <summary>
/// Request to update a form template section
/// </summary>
public class UpdateFormTemplateSectionRequest : CreateFormTemplateSectionRequest
{
    /// <summary>
    /// Section ID. Null for new sections.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Fields in this section
    /// </summary>
    public List<UpdateFormTemplateFieldRequest>? Fields { get; set; }
}

/// <summary>
/// Request to reorder sections
/// </summary>
public class ReorderSectionsRequest
{
    public List<int> SectionIds { get; set; } = new();
}

/// <summary>
/// Request to move a field to a section
/// </summary>
public class MoveFieldToSectionRequest
{
    public int FieldId { get; set; }
    public int TargetSectionId { get; set; }
    public int ColumnIndex { get; set; }
    public int RowIndex { get; set; }
}

#endregion

#region FormTemplateField DTOs

/// <summary>
/// DTO for form template field
/// </summary>
public class FormTemplateFieldDto
{
    public int Id { get; set; }
    public int FormTemplateId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public FormFieldDataType DataType { get; set; }
    public string DataTypeName => DataType.ToString();
    public int DisplayOrder { get; set; }

    // New section-based positioning
    public int? FormTemplateSectionId { get; set; }
    public int ColumnIndex { get; set; }
    public int RowIndex { get; set; }

    // Deprecated - kept for backward compatibility
    public string? SectionName { get; set; }
    public int SectionOrder { get; set; }
    public string Width { get; set; } = "full";

    // Spacing (in pixels)
    public int? PaddingTop { get; set; }
    public int? PaddingRight { get; set; }
    public int? PaddingBottom { get; set; }
    public int? PaddingLeft { get; set; }
    public int? MarginTop { get; set; }
    public int? MarginBottom { get; set; }

    // Layout
    public string? TextAlign { get; set; }  // "left", "center", "right", "justify"
    public int? FixedHeight { get; set; }   // null = auto height

    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; }
    public bool IsReadOnly { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public List<string>? SelectOptions { get; set; }
    public string? Formula { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? DecimalPlaces { get; set; }
    public string? CurrencySymbol { get; set; }
    public int? LinkedWorksheetTemplateId { get; set; }
    public string? LinkedWorksheetTemplateName { get; set; }
    public int? MaxPhotos { get; set; }
    public bool RequirePhotoLocation { get; set; }
}

/// <summary>
/// Request to create a form template field
/// </summary>
public class CreateFormTemplateFieldRequest
{
    public string FieldKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public FormFieldDataType DataType { get; set; }
    public int DisplayOrder { get; set; }
    public string? SectionName { get; set; }
    public int SectionOrder { get; set; }
    public int ColumnIndex { get; set; } = 0;
    public int RowIndex { get; set; } = 0;
    public string Width { get; set; } = "full";

    // Spacing (in pixels)
    public int? PaddingTop { get; set; }
    public int? PaddingRight { get; set; }
    public int? PaddingBottom { get; set; }
    public int? PaddingLeft { get; set; }
    public int? MarginTop { get; set; }
    public int? MarginBottom { get; set; }

    // Layout
    public string? TextAlign { get; set; }  // "left", "center", "right", "justify"
    public int? FixedHeight { get; set; }   // null = auto height

    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsReadOnly { get; set; }
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public List<string>? SelectOptions { get; set; }
    public string? Formula { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? DecimalPlaces { get; set; }
    public string? CurrencySymbol { get; set; }
    public int? LinkedWorksheetTemplateId { get; set; }
    public int? MaxPhotos { get; set; }
    public bool RequirePhotoLocation { get; set; }
}

/// <summary>
/// Request to update a form template field
/// </summary>
public class UpdateFormTemplateFieldRequest : CreateFormTemplateFieldRequest
{
    public int? Id { get; set; }
}

#endregion

#region FormInstance DTOs

/// <summary>
/// Summary DTO for form instance list views
/// </summary>
public class FormInstanceSummaryDto
{
    public int Id { get; set; }
    public string FormNumber { get; set; } = string.Empty;
    public int FormTemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public FormModuleContext ModuleContext { get; set; }
    public string ModuleContextName => ModuleContext.ToString();
    public FormInstanceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public string? LinkedEntityDisplay { get; set; }
    public int CompletedFieldCount { get; set; }
    public int TotalFieldCount { get; set; }
    public int CompletionPercentage => TotalFieldCount > 0
        ? (int)Math.Round((double)CompletedFieldCount / TotalFieldCount * 100)
        : 0;
    public DateTime CreatedDate { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByName { get; set; }
}

/// <summary>
/// Full DTO for form instance with all values
/// </summary>
public class FormInstanceDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string FormNumber { get; set; } = string.Empty;
    public int FormTemplateId { get; set; }
    public FormTemplateDto? Template { get; set; }
    public FormInstanceStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public string? LinkedEntityDisplay { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedByUserId { get; set; }
    public string? ModifiedByName { get; set; }
    public int? SubmittedByUserId { get; set; }
    public string? SubmittedByName { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public List<FormInstanceValueDto> Values { get; set; } = new();
    public List<FormInstanceAttachmentDto> Attachments { get; set; } = new();
}

/// <summary>
/// Request to create a new form instance
/// </summary>
public class CreateFormInstanceRequest
{
    public int FormTemplateId { get; set; }
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public string? LinkedEntityDisplay { get; set; }
    public string? Notes { get; set; }
    public List<FormInstanceValueRequest>? Values { get; set; }
}

/// <summary>
/// Request to update a form instance
/// </summary>
public class UpdateFormInstanceRequest
{
    public string? Notes { get; set; }
    public List<FormInstanceValueRequest>? Values { get; set; }
}

/// <summary>
/// Request to reject a form instance
/// </summary>
public class RejectFormInstanceRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Filter parameters for form instance queries
/// </summary>
public class FormInstanceFilterRequest
{
    public FormModuleContext? ModuleContext { get; set; }
    public FormInstanceStatus? Status { get; set; }
    public int? FormTemplateId { get; set; }
    public string? LinkedEntityType { get; set; }
    public int? LinkedEntityId { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

#endregion

#region FormInstanceValue DTOs

/// <summary>
/// DTO for form instance field value
/// </summary>
public class FormInstanceValueDto
{
    public int Id { get; set; }
    public int FormInstanceId { get; set; }
    public int FormTemplateFieldId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public FormFieldDataType DataType { get; set; }
    public string? DisplayName { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BoolValue { get; set; }
    public object? JsonValue { get; set; }
    public string? SignatureDataUrl { get; set; }
    public bool? PassFailValue { get; set; }
    public string? PassFailComment { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedByName { get; set; }
    public List<FormInstanceAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// Request to set a form instance field value
/// </summary>
public class FormInstanceValueRequest
{
    public string FieldKey { get; set; } = string.Empty;
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BoolValue { get; set; }
    public object? JsonValue { get; set; }
    public string? SignatureDataUrl { get; set; }
    public bool? PassFailValue { get; set; }
    public string? PassFailComment { get; set; }
}

#endregion

#region FormInstanceAttachment DTOs

/// <summary>
/// DTO for form instance attachment
/// </summary>
public class FormInstanceAttachmentDto
{
    public int Id { get; set; }
    public int FormInstanceId { get; set; }
    public int? FormInstanceValueId { get; set; }
    public string? FieldKey { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? StorageProvider { get; set; }
    public string? StoragePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Caption { get; set; }
    public DateTime UploadedDate { get; set; }
    public int? UploadedByUserId { get; set; }
    public string? UploadedByName { get; set; }
}

/// <summary>
/// Request to upload a form attachment
/// </summary>
public class UploadFormAttachmentRequest
{
    public int FormInstanceId { get; set; }
    public string? FieldKey { get; set; }
    public string? Caption { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Paginated response for form templates
/// </summary>
public class FormTemplateListResponse
{
    public List<FormTemplateSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Paginated response for form instances
/// </summary>
public class FormInstanceListResponse
{
    public List<FormInstanceSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

#endregion

#region Section Layout Options

/// <summary>
/// Available section layout option
/// </summary>
public class SectionLayoutOption
{
    /// <summary>
    /// Layout identifier (e.g., "1-col", "2-col-equal")
    /// </summary>
    public string LayoutType { get; set; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "Single Column", "Two Equal Columns")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the layout
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Number of columns in this layout
    /// </summary>
    public int ColumnCount { get; set; }

    /// <summary>
    /// CSS grid-template-columns value
    /// </summary>
    public string GridTemplateColumns { get; set; } = string.Empty;

    /// <summary>
    /// Minimum page width in mm required for this layout
    /// </summary>
    public decimal MinPageWidthMm { get; set; }

    /// <summary>
    /// Icon class for visual representation
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}

#endregion

#region Page Size Presets

/// <summary>
/// Preset page size for form templates
/// </summary>
public class FormPageSizePreset
{
    /// <summary>
    /// Preset name (e.g., "A4", "Letter")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display description (e.g., "A4 (210mm x 297mm)")
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Page width in millimeters
    /// </summary>
    public decimal WidthMm { get; set; }

    /// <summary>
    /// Page height in millimeters
    /// </summary>
    public decimal HeightMm { get; set; }
}

#endregion
