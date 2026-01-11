using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Components.Pages.Forms;

/// <summary>
/// Type of form section - determines behavior and placement
/// </summary>
public enum FormSectionType
{
    Regular = 0,
    Header = 1,
    Footer = 2
}

public partial class FormFieldSidebar : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IModuleFeatureService ModuleFeatureService { get; set; } = default!;

    [Parameter] public bool IsVisible { get; set; } = true;
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback<FieldDefinition> OnFieldDragStart { get; set; }
    [Parameter] public EventCallback OnFieldDragEnd { get; set; }
    [Parameter] public EventCallback<SectionLayoutDefinition> OnSectionDragStart { get; set; }
    [Parameter] public EventCallback OnSectionDragEnd { get; set; }
    [Parameter] public decimal PageWidthMm { get; set; } = 210; // For layout constraint checking
    [Parameter] public bool HasHeader { get; set; } // Whether form already has a header section
    [Parameter] public bool HasFooter { get; set; } // Whether form already has a footer section

    private string searchTerm = "";
    private HashSet<string> expandedSections = new() { "sections", "basic", "selection", "document", "images & icons", "datafields" };
    private FieldDefinition? draggedField;
    private SectionLayoutDefinition? draggedSection;
    private IJSObjectReference? jsModule;
    private bool previousIsVisible;
    private Dictionary<string, bool> moduleAccess = new();

    // Field definition model for sidebar items
    public class FieldDefinition
    {
        public FormFieldDataType DataType { get; set; }
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Category { get; set; } = "";

        // Database-bound field properties
        public bool IsDatabaseField { get; set; } = false;
        public string? EntityType { get; set; }      // "Customer", "Equipment", etc.
        public string? PropertyName { get; set; }    // "Name", "SerialNumber", etc.
        public string? RequiredModule { get; set; }  // "Trace", "Assets", etc.
    }

    // Section layout definition for drag-drop
    public class SectionLayoutDefinition
    {
        public string LayoutType { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public int ColumnCount { get; set; }
        public string GridTemplateColumns { get; set; } = "";
        public decimal MinPageWidthMm { get; set; }
        public FormSectionType SectionType { get; set; } = FormSectionType.Regular;
    }

    public class FieldGroup
    {
        public string GroupName { get; set; } = "";
        public string GroupIcon { get; set; } = "";
        public string GroupColor { get; set; } = "";
        public List<FieldDefinition> Fields { get; set; } = new();
        public string? RequiredModule { get; set; }  // For database field groups
    }

    // Section layout options (regular body sections)
    private static readonly List<SectionLayoutDefinition> SectionLayouts = new()
    {
        new() { LayoutType = "1-col", DisplayName = "Single Column", Description = "Full width (100%)", Icon = "fas fa-square", ColumnCount = 1, GridTemplateColumns = "1fr", MinPageWidthMm = 100, SectionType = FormSectionType.Regular },
        new() { LayoutType = "2-col-equal", DisplayName = "Two Equal", Description = "50% / 50%", Icon = "fas fa-columns", ColumnCount = 2, GridTemplateColumns = "1fr 1fr", MinPageWidthMm = 140, SectionType = FormSectionType.Regular },
        new() { LayoutType = "2-col-left", DisplayName = "Wide Left", Description = "66% / 33%", Icon = "fas fa-align-left", ColumnCount = 2, GridTemplateColumns = "2fr 1fr", MinPageWidthMm = 140, SectionType = FormSectionType.Regular },
        new() { LayoutType = "2-col-right", DisplayName = "Wide Right", Description = "33% / 66%", Icon = "fas fa-align-right", ColumnCount = 2, GridTemplateColumns = "1fr 2fr", MinPageWidthMm = 140, SectionType = FormSectionType.Regular },
        new() { LayoutType = "3-col-equal", DisplayName = "Three Equal", Description = "33% / 33% / 33%", Icon = "fas fa-th", ColumnCount = 3, GridTemplateColumns = "1fr 1fr 1fr", MinPageWidthMm = 180, SectionType = FormSectionType.Regular }
    };

    // Header layout options (repeats on every page)
    private static readonly List<SectionLayoutDefinition> HeaderLayouts = new()
    {
        new() { LayoutType = "header-1-col", DisplayName = "Header", Description = "Page header (repeats)", Icon = "fas fa-arrow-up", ColumnCount = 1, GridTemplateColumns = "1fr", MinPageWidthMm = 100, SectionType = FormSectionType.Header },
        new() { LayoutType = "header-2-col", DisplayName = "Header 2-Col", Description = "Two-column header", Icon = "fas fa-columns", ColumnCount = 2, GridTemplateColumns = "1fr 1fr", MinPageWidthMm = 140, SectionType = FormSectionType.Header },
        new() { LayoutType = "header-logo-text", DisplayName = "Logo + Text", Description = "Logo left, text right", Icon = "fas fa-image", ColumnCount = 2, GridTemplateColumns = "auto 1fr", MinPageWidthMm = 140, SectionType = FormSectionType.Header }
    };

    // Footer layout options (repeats on every page)
    private static readonly List<SectionLayoutDefinition> FooterLayouts = new()
    {
        new() { LayoutType = "footer-1-col", DisplayName = "Footer", Description = "Page footer (repeats)", Icon = "fas fa-arrow-down", ColumnCount = 1, GridTemplateColumns = "1fr", MinPageWidthMm = 100, SectionType = FormSectionType.Footer },
        new() { LayoutType = "footer-3-col", DisplayName = "Footer 3-Col", Description = "Three-column footer", Icon = "fas fa-th", ColumnCount = 3, GridTemplateColumns = "1fr 1fr 1fr", MinPageWidthMm = 180, SectionType = FormSectionType.Footer }
    };

    // All form field types organized by category
    private static readonly List<FieldGroup> AllFieldGroups = new()
    {
        new FieldGroup
        {
            GroupName = "Basic",
            GroupIcon = "fas fa-font",
            GroupColor = "primary",
            Fields = new List<FieldDefinition>
            {
                new() { DataType = FormFieldDataType.Text, DisplayName = "Text", Description = "Single line text input", Icon = "fas fa-font", Category = "Basic" },
                new() { DataType = FormFieldDataType.MultilineText, DisplayName = "Multiline Text", Description = "Multi-line text area", Icon = "fas fa-align-left", Category = "Basic" },
                new() { DataType = FormFieldDataType.Number, DisplayName = "Number", Description = "Numeric input with validation", Icon = "fas fa-hashtag", Category = "Basic" },
                new() { DataType = FormFieldDataType.Currency, DisplayName = "Currency", Description = "Money value with formatting", Icon = "fas fa-dollar-sign", Category = "Basic" },
                new() { DataType = FormFieldDataType.Date, DisplayName = "Date", Description = "Date picker", Icon = "fas fa-calendar", Category = "Basic" },
                new() { DataType = FormFieldDataType.DateTime, DisplayName = "Date & Time", Description = "Date and time picker", Icon = "fas fa-calendar-alt", Category = "Basic" }
            }
        },
        new FieldGroup
        {
            GroupName = "Selection",
            GroupIcon = "fas fa-list",
            GroupColor = "success",
            Fields = new List<FieldDefinition>
            {
                new() { DataType = FormFieldDataType.Checkbox, DisplayName = "Checkbox", Description = "Yes/No toggle", Icon = "fas fa-check-square", Category = "Selection" },
                new() { DataType = FormFieldDataType.Choice, DisplayName = "Choice (Dropdown)", Description = "Single selection dropdown", Icon = "fas fa-list", Category = "Selection" },
                new() { DataType = FormFieldDataType.MultiChoice, DisplayName = "Multi-Choice", Description = "Multiple selection checkboxes", Icon = "fas fa-check-double", Category = "Selection" },
                new() { DataType = FormFieldDataType.PassFail, DisplayName = "Pass / Fail", Description = "Binary pass or fail choice", Icon = "fas fa-thumbs-up", Category = "Selection" }
            }
        },
        new FieldGroup
        {
            GroupName = "Media",
            GroupIcon = "fas fa-camera",
            GroupColor = "warning",
            Fields = new List<FieldDefinition>
            {
                new() { DataType = FormFieldDataType.Signature, DisplayName = "Signature", Description = "Digital signature capture", Icon = "fas fa-signature", Category = "Media" },
                new() { DataType = FormFieldDataType.Photo, DisplayName = "Photo", Description = "Photo capture (supports multiple)", Icon = "fas fa-camera", Category = "Media" },
                new() { DataType = FormFieldDataType.Location, DisplayName = "GPS Location", Description = "Capture current location", Icon = "fas fa-map-marker-alt", Category = "Media" }
            }
        },
        new FieldGroup
        {
            GroupName = "Advanced",
            GroupIcon = "fas fa-cog",
            GroupColor = "info",
            Fields = new List<FieldDefinition>
            {
                new() { DataType = FormFieldDataType.Percentage, DisplayName = "Percentage", Description = "Percentage value (0-100%)", Icon = "fas fa-percent", Category = "Advanced" },
                new() { DataType = FormFieldDataType.Computed, DisplayName = "Computed", Description = "Formula-based calculation", Icon = "fas fa-calculator", Category = "Advanced" },
                new() { DataType = FormFieldDataType.WorksheetLink, DisplayName = "Worksheet Link", Description = "Link to estimation worksheet", Icon = "fas fa-link", Category = "Advanced" }
            }
        },
        new FieldGroup
        {
            GroupName = "Document",
            GroupIcon = "fas fa-file-alt",
            GroupColor = "purple",
            Fields = new List<FieldDefinition>
            {
                new() { DataType = FormFieldDataType.CompanyLogo, DisplayName = "Company Logo", Description = "Company logo image", Icon = "fas fa-building", Category = "Document" },
                new() { DataType = FormFieldDataType.DocumentTitle, DisplayName = "Document Title", Description = "Form template name", Icon = "fas fa-heading", Category = "Document" },
                new() { DataType = FormFieldDataType.PageNumber, DisplayName = "Page Number", Description = "Page X of Y", Icon = "fas fa-file-alt", Category = "Document" },
                new() { DataType = FormFieldDataType.CurrentDate, DisplayName = "Current Date", Description = "Auto-populated date", Icon = "fas fa-calendar-day", Category = "Document" },
                new() { DataType = FormFieldDataType.FormNumber, DisplayName = "Form Number", Description = "Auto-generated form number", Icon = "fas fa-hashtag", Category = "Document" }
            }
        },
        new FieldGroup
        {
            GroupName = "Images & Icons",
            GroupIcon = "fas fa-icons",
            GroupColor = "decorative",
            Fields = new List<FieldDefinition>
            {
                // Special decorative elements
                new() { DataType = FormFieldDataType.DecorativeImage, DisplayName = "Image / Logo", Description = "Upload custom image", Icon = "fas fa-image", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeBanner, DisplayName = "Banner / Divider", Description = "Horizontal separator line", Icon = "fas fa-minus", Category = "Decorative" },
                new() { DataType = FormFieldDataType.Photo, DisplayName = "User Photo", Description = "Circular photo placeholder", Icon = "fas fa-user-circle", Category = "Decorative" },
                // Common decorative icons
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Phone Icon", Description = "Phone contact icon", Icon = "fas fa-phone", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Globe Icon", Description = "Website/globe icon", Icon = "fas fa-globe", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Email Icon", Description = "Email/envelope icon", Icon = "fas fa-envelope", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Location Icon", Description = "Map marker icon", Icon = "fas fa-map-marker-alt", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Home Icon", Description = "House/home icon", Icon = "fas fa-home", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "People Icon", Description = "Group of users icon", Icon = "fas fa-users", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Calendar Icon", Description = "Date/calendar icon", Icon = "fas fa-calendar-alt", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Star Icon", Description = "Star/favorite icon", Icon = "fas fa-star", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Check Icon", Description = "Checkmark icon", Icon = "fas fa-check-circle", Category = "Decorative" },
                new() { DataType = FormFieldDataType.DecorativeIcon, DisplayName = "Info Icon", Description = "Information icon", Icon = "fas fa-info-circle", Category = "Decorative" }
            }
        }
    };

    // Database-bound fields organized by module
    private static readonly List<FieldGroup> DatabaseFieldGroups = new()
    {
        // ===== TRACE MODULE (Takeoffs) =====
        new FieldGroup
        {
            GroupName = "Customer Fields",
            GroupIcon = "fas fa-building",
            GroupColor = "trace",
            RequiredModule = "Trace",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Customer Name", PropertyName = "Customer.Name", Description = "Customer company name", Icon = "fas fa-building", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Customer Code", PropertyName = "Customer.CustomerCode", Description = "Customer reference code", Icon = "fas fa-barcode", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Customer ABN", PropertyName = "Customer.ABN", Description = "Australian Business Number", Icon = "fas fa-id-card", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Customer Address", PropertyName = "Customer.Address", Description = "Customer street address", Icon = "fas fa-map-marker-alt", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Customer Email", PropertyName = "Customer.Email", Description = "Customer email address", Icon = "fas fa-envelope", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Customer Phone", PropertyName = "Customer.PhoneNumber", Description = "Customer phone number", Icon = "fas fa-phone", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Customer Website", PropertyName = "Customer.Website", Description = "Customer website URL", Icon = "fas fa-globe", Category = "Customer", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Contact Fields",
            GroupIcon = "fas fa-user",
            GroupColor = "trace",
            RequiredModule = "Trace",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Contact First Name", PropertyName = "Contact.FirstName", Description = "Contact's first name", Icon = "fas fa-user", Category = "Contact", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Contact Last Name", PropertyName = "Contact.LastName", Description = "Contact's last name", Icon = "fas fa-user", Category = "Contact", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Contact Title", PropertyName = "Contact.Title", Description = "Contact's job title", Icon = "fas fa-briefcase", Category = "Contact", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Contact Email", PropertyName = "Contact.Email", Description = "Contact's email address", Icon = "fas fa-envelope", Category = "Contact", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Contact Phone", PropertyName = "Contact.Phone", Description = "Contact's phone number", Icon = "fas fa-phone", Category = "Contact", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Contact Mobile", PropertyName = "Contact.Mobile", Description = "Contact's mobile number", Icon = "fas fa-mobile-alt", Category = "Contact", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Takeoff Fields",
            GroupIcon = "fas fa-ruler-combined",
            GroupColor = "trace",
            RequiredModule = "Trace",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Takeoff Number", PropertyName = "Takeoff.TakeoffNumber", Description = "Takeoff reference number", Icon = "fas fa-hashtag", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Drawing Number", PropertyName = "Takeoff.DrawingNumber", Description = "Source drawing number", Icon = "fas fa-drafting-compass", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Drawing Title", PropertyName = "Takeoff.DrawingTitle", Description = "Drawing title", Icon = "fas fa-heading", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Project Name", PropertyName = "Takeoff.ProjectName", Description = "Project name", Icon = "fas fa-project-diagram", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Client Name", PropertyName = "Takeoff.ClientName", Description = "Client name", Icon = "fas fa-user-tie", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Trace Name", PropertyName = "Takeoff.TraceName", Description = "Trace name", Icon = "fas fa-tag", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Scale", PropertyName = "Takeoff.Scale", Description = "Drawing scale", Icon = "fas fa-expand-arrows-alt", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Scale Unit", PropertyName = "Takeoff.ScaleUnit", Description = "Scale measurement unit", Icon = "fas fa-ruler", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Revision", PropertyName = "Takeoff.Revision", Description = "Drawing revision", Icon = "fas fa-code-branch", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Project Architect", PropertyName = "Takeoff.ProjectArchitect", Description = "Project architect", Icon = "fas fa-hard-hat", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Project Engineer", PropertyName = "Takeoff.ProjectEngineer", Description = "Project engineer", Icon = "fas fa-user-cog", Category = "Takeoff", IsDatabaseField = true, RequiredModule = "Trace", DataType = FormFieldDataType.Text }
            }
        },

        // ===== QDOCS MODULE (Quality - Drawings, Documents & Traceability) =====
        new FieldGroup
        {
            GroupName = "Drawing Fields",
            GroupIcon = "fas fa-drafting-compass",
            GroupColor = "qdocs",
            RequiredModule = "QDocs",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Drawing Number", PropertyName = "Drawing.DrawingNumber", Description = "Drawing reference number", Icon = "fas fa-hashtag", Category = "Drawing", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Drawing Title", PropertyName = "Drawing.DrawingTitle", Description = "Drawing title", Icon = "fas fa-heading", Category = "Drawing", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Drawing Description", PropertyName = "Drawing.Description", Description = "Drawing description", Icon = "fas fa-align-left", Category = "Drawing", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Current Stage", PropertyName = "Drawing.CurrentStage", Description = "IFA/IFC/Superseded", Icon = "fas fa-layer-group", Category = "Drawing", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Work Package ID", PropertyName = "Drawing.WorkPackageId", Description = "Work package identifier", Icon = "fas fa-box", Category = "Drawing", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Assembly ID", PropertyName = "Drawing.AssemblyId", Description = "Assembly identifier", Icon = "fas fa-cubes", Category = "Drawing", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Revision Fields",
            GroupIcon = "fas fa-code-branch",
            GroupColor = "qdocs",
            RequiredModule = "QDocs",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Revision Code", PropertyName = "DrawingRevision.RevisionCode", Description = "Revision code (e.g., IFA-R1)", Icon = "fas fa-code-branch", Category = "Revision", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Revision Type", PropertyName = "DrawingRevision.RevisionType", Description = "IFA or IFC", Icon = "fas fa-tag", Category = "Revision", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Revision Number", PropertyName = "DrawingRevision.RevisionNumber", Description = "Revision number", Icon = "fas fa-sort-numeric-up", Category = "Revision", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Number },
                new() { DisplayName = "Revision Notes", PropertyName = "DrawingRevision.RevisionNotes", Description = "Revision notes", Icon = "fas fa-sticky-note", Category = "Revision", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Revision Status", PropertyName = "DrawingRevision.Status", Description = "Draft/UnderReview/Approved/Rejected/Superseded", Icon = "fas fa-clipboard-check", Category = "Revision", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Approval Fields",
            GroupIcon = "fas fa-clipboard-check",
            GroupColor = "qdocs",
            RequiredModule = "QDocs",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Reviewed By", PropertyName = "DrawingRevision.ReviewedBy", Description = "Reviewer name", Icon = "fas fa-user-check", Category = "Approval", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Review Date", PropertyName = "DrawingRevision.ReviewDate", Description = "Date of review", Icon = "fas fa-calendar-check", Category = "Approval", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Approved By", PropertyName = "DrawingRevision.ApprovedBy", Description = "Approver name", Icon = "fas fa-user-shield", Category = "Approval", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Approval Date", PropertyName = "DrawingRevision.ApprovalDate", Description = "Date of approval", Icon = "fas fa-calendar-alt", Category = "Approval", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Approval Comments", PropertyName = "DrawingRevision.ApprovalComments", Description = "Approval comments", Icon = "fas fa-comment", Category = "Approval", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Active For Production", PropertyName = "DrawingRevision.IsActiveForProduction", Description = "Is active for production", Icon = "fas fa-industry", Category = "Approval", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Checkbox }
            }
        },
        new FieldGroup
        {
            GroupName = "Material Traceability",
            GroupIcon = "fas fa-link",
            GroupColor = "qdocs",
            RequiredModule = "QDocs",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Material Code", PropertyName = "TraceMaterial.MaterialCode", Description = "Material code", Icon = "fas fa-barcode", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Material Description", PropertyName = "TraceMaterial.Description", Description = "Material description", Icon = "fas fa-align-left", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Heat Number", PropertyName = "TraceMaterial.HeatNumber", Description = "Heat/cast number", Icon = "fas fa-fire", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Batch Number", PropertyName = "TraceMaterial.BatchNumber", Description = "Batch number", Icon = "fas fa-layer-group", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Serial Number", PropertyName = "TraceMaterial.SerialNumber", Description = "Serial number", Icon = "fas fa-fingerprint", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Supplier", PropertyName = "TraceMaterial.Supplier", Description = "Supplier name", Icon = "fas fa-truck", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Supplier Batch", PropertyName = "TraceMaterial.SupplierBatch", Description = "Supplier batch reference", Icon = "fas fa-truck-loading", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Mill Certificate", PropertyName = "TraceMaterial.MillCertificate", Description = "Mill certificate reference", Icon = "fas fa-certificate", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Cert Type", PropertyName = "TraceMaterial.CertType", Description = "Certificate type (2.1/2.2/3.1/3.2)", Icon = "fas fa-file-certificate", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Cert Date", PropertyName = "TraceMaterial.CertDate", Description = "Certificate date", Icon = "fas fa-calendar", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Chemical Composition", PropertyName = "TraceMaterial.ChemicalComposition", Description = "Chemical composition", Icon = "fas fa-flask", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Mechanical Properties", PropertyName = "TraceMaterial.MechanicalProperties", Description = "Mechanical properties", Icon = "fas fa-dumbbell", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Test Results", PropertyName = "TraceMaterial.TestResults", Description = "Test results", Icon = "fas fa-vial", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Quantity", PropertyName = "TraceMaterial.Quantity", Description = "Material quantity", Icon = "fas fa-sort-numeric-up", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Number },
                new() { DisplayName = "Unit", PropertyName = "TraceMaterial.Unit", Description = "Unit of measure", Icon = "fas fa-balance-scale", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Weight", PropertyName = "TraceMaterial.Weight", Description = "Material weight", Icon = "fas fa-weight-hanging", Category = "Traceability", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Number }
            }
        },
        new FieldGroup
        {
            GroupName = "Process Traceability",
            GroupIcon = "fas fa-cogs",
            GroupColor = "qdocs",
            RequiredModule = "QDocs",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Operation Code", PropertyName = "TraceProcess.OperationCode", Description = "Operation code", Icon = "fas fa-cog", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Operation Description", PropertyName = "TraceProcess.OperationDescription", Description = "Operation description", Icon = "fas fa-align-left", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Operator Name", PropertyName = "TraceProcess.OperatorName", Description = "Operator name", Icon = "fas fa-user-cog", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Machine Name", PropertyName = "TraceProcess.MachineName", Description = "Machine name", Icon = "fas fa-industry", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Start Time", PropertyName = "TraceProcess.StartTime", Description = "Process start time", Icon = "fas fa-play", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.DateTime },
                new() { DisplayName = "End Time", PropertyName = "TraceProcess.EndTime", Description = "Process end time", Icon = "fas fa-stop", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.DateTime },
                new() { DisplayName = "Duration", PropertyName = "TraceProcess.Duration", Description = "Process duration", Icon = "fas fa-clock", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Passed Inspection", PropertyName = "TraceProcess.PassedInspection", Description = "Passed inspection", Icon = "fas fa-check-circle", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.PassFail },
                new() { DisplayName = "Inspection Notes", PropertyName = "TraceProcess.InspectionNotes", Description = "Inspection notes", Icon = "fas fa-clipboard", Category = "Process", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText }
            }
        },
        new FieldGroup
        {
            GroupName = "Assembly Traceability",
            GroupIcon = "fas fa-cubes",
            GroupColor = "qdocs",
            RequiredModule = "QDocs",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Assembly Number", PropertyName = "TraceAssembly.AssemblyNumber", Description = "Assembly number", Icon = "fas fa-hashtag", Category = "Assembly", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Assembly Serial Number", PropertyName = "TraceAssembly.SerialNumber", Description = "Assembly serial number", Icon = "fas fa-fingerprint", Category = "Assembly", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Assembly Date", PropertyName = "TraceAssembly.AssemblyDate", Description = "Assembly date", Icon = "fas fa-calendar", Category = "Assembly", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Build Operator", PropertyName = "TraceAssembly.BuildOperator", Description = "Build operator name", Icon = "fas fa-user-cog", Category = "Assembly", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Build Location", PropertyName = "TraceAssembly.BuildLocation", Description = "Build location", Icon = "fas fa-map-marker-alt", Category = "Assembly", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Build Notes", PropertyName = "TraceAssembly.BuildNotes", Description = "Build notes", Icon = "fas fa-sticky-note", Category = "Assembly", IsDatabaseField = true, RequiredModule = "QDocs", DataType = FormFieldDataType.MultilineText }
            }
        },

        // ===== ASSETS MODULE =====
        new FieldGroup
        {
            GroupName = "Equipment Fields",
            GroupIcon = "fas fa-tools",
            GroupColor = "assets",
            RequiredModule = "Assets",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Equipment Code", PropertyName = "Equipment.EquipmentCode", Description = "Equipment code", Icon = "fas fa-barcode", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Equipment Name", PropertyName = "Equipment.Name", Description = "Equipment name", Icon = "fas fa-tools", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Equipment Description", PropertyName = "Equipment.Description", Description = "Equipment description", Icon = "fas fa-align-left", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.MultilineText },
                new() { DisplayName = "Manufacturer", PropertyName = "Equipment.Manufacturer", Description = "Manufacturer name", Icon = "fas fa-industry", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Model", PropertyName = "Equipment.Model", Description = "Equipment model", Icon = "fas fa-cube", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Serial Number", PropertyName = "Equipment.SerialNumber", Description = "Equipment serial number", Icon = "fas fa-fingerprint", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Purchase Date", PropertyName = "Equipment.PurchaseDate", Description = "Date of purchase", Icon = "fas fa-calendar-plus", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Purchase Cost", PropertyName = "Equipment.PurchaseCost", Description = "Purchase cost", Icon = "fas fa-dollar-sign", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Currency },
                new() { DisplayName = "Current Value", PropertyName = "Equipment.CurrentValue", Description = "Current asset value", Icon = "fas fa-chart-line", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Currency },
                new() { DisplayName = "Warranty Expiry", PropertyName = "Equipment.WarrantyExpiry", Description = "Warranty expiry date", Icon = "fas fa-shield-alt", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Asset Status", PropertyName = "Equipment.Status", Description = "Asset status", Icon = "fas fa-info-circle", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Department", PropertyName = "Equipment.Department", Description = "Assigned department", Icon = "fas fa-building", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Assigned To", PropertyName = "Equipment.AssignedTo", Description = "Assigned user", Icon = "fas fa-user", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Location", PropertyName = "Equipment.Location", Description = "Equipment location", Icon = "fas fa-map-marker-alt", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Last Maintenance Date", PropertyName = "Equipment.LastMaintenanceDate", Description = "Last maintenance date", Icon = "fas fa-wrench", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Next Maintenance Date", PropertyName = "Equipment.NextMaintenanceDate", Description = "Next scheduled maintenance", Icon = "fas fa-calendar-alt", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Maintenance Interval", PropertyName = "Equipment.MaintenanceInterval", Description = "Maintenance interval (days)", Icon = "fas fa-clock", Category = "Equipment", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Number }
            }
        },
        new FieldGroup
        {
            GroupName = "QR/Barcode Fields",
            GroupIcon = "fas fa-qrcode",
            GroupColor = "assets",
            RequiredModule = "Assets",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "QR Code Image", PropertyName = "Equipment.QRCodeImage", Description = "QR code image", Icon = "fas fa-qrcode", Category = "QR/Barcode", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Photo },
                new() { DisplayName = "QR Code Identifier", PropertyName = "Equipment.QRCodeIdentifier", Description = "QR code identifier", Icon = "fas fa-hashtag", Category = "QR/Barcode", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Barcode Data", PropertyName = "Equipment.BarcodeData", Description = "Barcode data", Icon = "fas fa-barcode", Category = "QR/Barcode", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Certification Fields",
            GroupIcon = "fas fa-certificate",
            GroupColor = "assets",
            RequiredModule = "Assets",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Certificate Type", PropertyName = "Certification.CertificateType", Description = "Certificate type", Icon = "fas fa-file-certificate", Category = "Certification", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Certificate Number", PropertyName = "Certification.CertificateNumber", Description = "Certificate number", Icon = "fas fa-hashtag", Category = "Certification", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Issuing Authority", PropertyName = "Certification.IssuingAuthority", Description = "Issuing authority", Icon = "fas fa-building", Category = "Certification", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Issue Date", PropertyName = "Certification.IssueDate", Description = "Issue date", Icon = "fas fa-calendar-plus", Category = "Certification", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Expiry Date", PropertyName = "Certification.ExpiryDate", Description = "Expiry date", Icon = "fas fa-calendar-times", Category = "Certification", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Certification Status", PropertyName = "Certification.Status", Description = "Certification status", Icon = "fas fa-info-circle", Category = "Certification", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Kit Fields",
            GroupIcon = "fas fa-toolbox",
            GroupColor = "assets",
            RequiredModule = "Assets",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Kit Code", PropertyName = "EquipmentKit.KitCode", Description = "Kit code", Icon = "fas fa-barcode", Category = "Kit", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Kit Name", PropertyName = "EquipmentKit.KitName", Description = "Kit name", Icon = "fas fa-toolbox", Category = "Kit", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Kit Status", PropertyName = "EquipmentKit.Status", Description = "Kit status", Icon = "fas fa-info-circle", Category = "Kit", IsDatabaseField = true, RequiredModule = "Assets", DataType = FormFieldDataType.Text }
            }
        },

        // ===== FABMATE MODULE (Work Orders) =====
        new FieldGroup
        {
            GroupName = "Work Order Fields",
            GroupIcon = "fas fa-clipboard-list",
            GroupColor = "fabmate",
            RequiredModule = "FabMate",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Work Order Number", PropertyName = "WorkOrder.WorkOrderNumber", Description = "Work order number", Icon = "fas fa-hashtag", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Operation Code", PropertyName = "WorkOrder.OperationCode", Description = "Operation code", Icon = "fas fa-cog", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Operation Description", PropertyName = "WorkOrder.OperationDescription", Description = "Operation description", Icon = "fas fa-align-left", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Operator Name", PropertyName = "WorkOrder.OperatorName", Description = "Operator name", Icon = "fas fa-user-cog", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Machine Name", PropertyName = "WorkOrder.MachineName", Description = "Machine name", Icon = "fas fa-industry", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Start Time", PropertyName = "WorkOrder.StartTime", Description = "Work order start time", Icon = "fas fa-play", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.DateTime },
                new() { DisplayName = "End Time", PropertyName = "WorkOrder.EndTime", Description = "Work order end time", Icon = "fas fa-stop", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.DateTime },
                new() { DisplayName = "Duration", PropertyName = "WorkOrder.Duration", Description = "Work order duration", Icon = "fas fa-clock", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Passed Inspection", PropertyName = "WorkOrder.PassedInspection", Description = "Passed inspection", Icon = "fas fa-check-circle", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.PassFail },
                new() { DisplayName = "Inspection Notes", PropertyName = "WorkOrder.InspectionNotes", Description = "Inspection notes", Icon = "fas fa-clipboard", Category = "WorkOrder", IsDatabaseField = true, RequiredModule = "FabMate", DataType = FormFieldDataType.MultilineText }
            }
        },

        // ===== ESTIMATE MODULE =====
        new FieldGroup
        {
            GroupName = "Quote Fields",
            GroupIcon = "fas fa-file-invoice-dollar",
            GroupColor = "estimate",
            RequiredModule = "Estimate",
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Quote Number", PropertyName = "Quote.QuoteNumber", Description = "Quote number", Icon = "fas fa-hashtag", Category = "Quote", IsDatabaseField = true, RequiredModule = "Estimate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Quote Date", PropertyName = "Quote.QuoteDate", Description = "Quote date", Icon = "fas fa-calendar", Category = "Quote", IsDatabaseField = true, RequiredModule = "Estimate", DataType = FormFieldDataType.Date },
                new() { DisplayName = "Project Reference", PropertyName = "Quote.ProjectReference", Description = "Project reference", Icon = "fas fa-project-diagram", Category = "Quote", IsDatabaseField = true, RequiredModule = "Estimate", DataType = FormFieldDataType.Text },
                new() { DisplayName = "Project Name", PropertyName = "Quote.ProjectName", Description = "Project name", Icon = "fas fa-building", Category = "Quote", IsDatabaseField = true, RequiredModule = "Estimate", DataType = FormFieldDataType.Text }
            }
        },

        // ===== CATALOGUE FIELDS (shared/general) =====
        new FieldGroup
        {
            GroupName = "Catalogue Item Fields",
            GroupIcon = "fas fa-book",
            GroupColor = "primary",
            RequiredModule = null,  // Available to all
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Item Code", PropertyName = "CatalogueItem.ItemCode", Description = "Catalogue item code", Icon = "fas fa-barcode", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Item Description", PropertyName = "CatalogueItem.Description", Description = "Item description", Icon = "fas fa-align-left", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Category", PropertyName = "CatalogueItem.Category", Description = "Item category", Icon = "fas fa-folder", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Material", PropertyName = "CatalogueItem.Material", Description = "Material type", Icon = "fas fa-cube", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Profile", PropertyName = "CatalogueItem.Profile", Description = "Profile type", Icon = "fas fa-shapes", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Grade", PropertyName = "CatalogueItem.Grade", Description = "Material grade", Icon = "fas fa-certificate", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text }
            }
        },
        new FieldGroup
        {
            GroupName = "Catalogue Dimensions",
            GroupIcon = "fas fa-ruler",
            GroupColor = "primary",
            RequiredModule = null,
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Length", PropertyName = "CatalogueItem.Length", Description = "Item length", Icon = "fas fa-ruler-horizontal", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Width", PropertyName = "CatalogueItem.Width", Description = "Item width", Icon = "fas fa-ruler-vertical", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Height", PropertyName = "CatalogueItem.Height", Description = "Item height", Icon = "fas fa-arrows-alt-v", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Thickness", PropertyName = "CatalogueItem.Thickness", Description = "Material thickness", Icon = "fas fa-layer-group", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Diameter", PropertyName = "CatalogueItem.Diameter", Description = "Diameter", Icon = "fas fa-circle", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "OD (Outer Diameter)", PropertyName = "CatalogueItem.OD", Description = "Outer diameter", Icon = "fas fa-circle", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "ID (Inner Diameter)", PropertyName = "CatalogueItem.ID", Description = "Inner diameter", Icon = "fas fa-circle-notch", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Wall Thickness", PropertyName = "CatalogueItem.WallThickness", Description = "Wall thickness", Icon = "fas fa-grip-lines-vertical", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Web", PropertyName = "CatalogueItem.Web", Description = "Web dimension", Icon = "fas fa-minus", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Flange", PropertyName = "CatalogueItem.Flange", Description = "Flange dimension", Icon = "fas fa-equals", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number }
            }
        },
        new FieldGroup
        {
            GroupName = "Catalogue Weight/Mass",
            GroupIcon = "fas fa-weight-hanging",
            GroupColor = "primary",
            RequiredModule = null,
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Mass kg/m", PropertyName = "CatalogueItem.MassKgPerMeter", Description = "Mass per meter (kg/m)", Icon = "fas fa-weight-hanging", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Mass kg/m²", PropertyName = "CatalogueItem.MassKgPerSquareMeter", Description = "Mass per square meter (kg/m²)", Icon = "fas fa-weight-hanging", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Weight kg", PropertyName = "CatalogueItem.WeightKg", Description = "Total weight (kg)", Icon = "fas fa-balance-scale", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number },
                new() { DisplayName = "Surface Area m²", PropertyName = "CatalogueItem.SurfaceArea", Description = "Surface area (m²)", Icon = "fas fa-expand", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Number }
            }
        },
        new FieldGroup
        {
            GroupName = "Catalogue Specs",
            GroupIcon = "fas fa-cogs",
            GroupColor = "primary",
            RequiredModule = null,
            Fields = new List<FieldDefinition>
            {
                new() { DisplayName = "Standard", PropertyName = "CatalogueItem.Standard", Description = "Industry standard", Icon = "fas fa-file-alt", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Alloy", PropertyName = "CatalogueItem.Alloy", Description = "Alloy type", Icon = "fas fa-atom", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Temper", PropertyName = "CatalogueItem.Temper", Description = "Temper designation", Icon = "fas fa-fire", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Finish", PropertyName = "CatalogueItem.Finish", Description = "Surface finish", Icon = "fas fa-paint-brush", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Coating", PropertyName = "CatalogueItem.Coating", Description = "Coating type", Icon = "fas fa-fill-drip", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Standard Lengths", PropertyName = "CatalogueItem.StandardLengths", Description = "Available standard lengths", Icon = "fas fa-list-ol", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Cut To Size", PropertyName = "CatalogueItem.CutToSize", Description = "Cut to size available", Icon = "fas fa-cut", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Checkbox },
                new() { DisplayName = "Unit", PropertyName = "CatalogueItem.Unit", Description = "Unit of measure", Icon = "fas fa-balance-scale", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.Text },
                new() { DisplayName = "Features", PropertyName = "CatalogueItem.Features", Description = "Product features", Icon = "fas fa-star", Category = "Catalogue", IsDatabaseField = true, DataType = FormFieldDataType.MultilineText }
            }
        }
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadModuleAccessAsync();
    }

    private async Task LoadModuleAccessAsync()
    {
        try
        {
            moduleAccess["Trace"] = await ModuleFeatureService.IsTraceEnabledAsync();
            moduleAccess["Estimate"] = await ModuleFeatureService.IsEstimateEnabledAsync();
            moduleAccess["FabMate"] = await ModuleFeatureService.IsFabMateEnabledAsync();
            moduleAccess["QDocs"] = await ModuleFeatureService.IsQDocsEnabledAsync();
            moduleAccess["Assets"] = await ModuleFeatureService.IsModuleEnabledAsync("Assets");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FormFieldSidebar] Error loading module access: {ex.Message}");
            // Default to all accessible if service fails
            moduleAccess["Trace"] = true;
            moduleAccess["Estimate"] = true;
            moduleAccess["FabMate"] = true;
            moduleAccess["QDocs"] = true;
            moduleAccess["Assets"] = true;
        }
    }

    private async Task ToggleSidebar()
    {
        IsVisible = !IsVisible;
        await IsVisibleChanged.InvokeAsync(IsVisible);
        await UpdateBodyClasses();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/template-sidebar-helpers.js");
                previousIsVisible = IsVisible;
                await UpdateBodyClasses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FormFieldSidebar] Error loading JS module: {ex.Message}");
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (jsModule != null && previousIsVisible != IsVisible)
        {
            previousIsVisible = IsVisible;
            await UpdateBodyClasses();
        }
    }

    private async Task UpdateBodyClasses()
    {
        if (jsModule != null)
        {
            try
            {
                await jsModule.InvokeVoidAsync("updateTemplateSidebarState", IsVisible);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FormFieldSidebar] Error updating body classes: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (jsModule != null)
        {
            try
            {
                await jsModule.InvokeVoidAsync("cleanupTemplateSidebar");
                await jsModule.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FormFieldSidebar] Error during cleanup: {ex.Message}");
            }
        }
    }

    private void ToggleSection(string sectionName)
    {
        var key = sectionName.ToLower();
        if (expandedSections.Contains(key))
            expandedSections.Remove(key);
        else
            expandedSections.Add(key);
    }

    private bool IsSectionExpanded(string sectionName)
    {
        return expandedSections.Contains(sectionName.ToLower());
    }

    private void ClearSearch()
    {
        searchTerm = "";
    }

    private void HandleDragStart(FieldDefinition field)
    {
        draggedField = field;
        OnFieldDragStart.InvokeAsync(field);
    }

    private void HandleDragEnd()
    {
        draggedField = null;
        OnFieldDragEnd.InvokeAsync();
    }

    private IEnumerable<FieldDefinition> GetAllFilteredFields()
    {
        var allFields = AllFieldGroups.SelectMany(g => g.Fields);
        if (string.IsNullOrWhiteSpace(searchTerm))
            return allFields;

        return allFields.Where(f =>
            f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<FieldGroup> GetFilteredFieldGroups()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return AllFieldGroups;

        return AllFieldGroups
            .Select(g => new FieldGroup
            {
                GroupName = g.GroupName,
                GroupIcon = g.GroupIcon,
                GroupColor = g.GroupColor,
                Fields = g.Fields.Where(f =>
                    f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList()
            })
            .Where(g => g.Fields.Any());
    }

    private int GetFieldCountInGroup(string groupName)
    {
        var group = GetFilteredFieldGroups().FirstOrDefault(g => g.GroupName == groupName);
        return group?.Fields.Count ?? 0;
    }

    private string GetGroupColorClass(string groupName)
    {
        var group = AllFieldGroups.FirstOrDefault(g => g.GroupName == groupName);
        return group?.GroupColor ?? "primary";
    }

    // Public method to get the currently dragged field (for parent component)
    public FieldDefinition? GetDraggedField() => draggedField;

    // Section drag handlers
    private void HandleSectionDragStart(SectionLayoutDefinition section)
    {
        draggedSection = section;
        OnSectionDragStart.InvokeAsync(section);
    }

    private void HandleSectionDragEnd()
    {
        draggedSection = null;
        OnSectionDragEnd.InvokeAsync();
    }

    // Get available section layouts filtered by page width
    private IEnumerable<SectionLayoutDefinition> GetAvailableSectionLayouts()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return SectionLayouts;

        return SectionLayouts.Where(s =>
            s.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsSectionLayoutDisabled(SectionLayoutDefinition layout)
    {
        return PageWidthMm < layout.MinPageWidthMm;
    }

    // Public method to get the currently dragged section (for parent component)
    public SectionLayoutDefinition? GetDraggedSection() => draggedSection;

    // ===== DATABASE FIELD METHODS =====

    private bool IsModuleAccessible(string? moduleName)
    {
        if (string.IsNullOrEmpty(moduleName))
            return true; // No module required = accessible to all
        return moduleAccess.TryGetValue(moduleName, out var hasAccess) && hasAccess;
    }

    private bool IsDatabaseFieldAccessible(FieldDefinition field)
    {
        if (!field.IsDatabaseField || string.IsNullOrEmpty(field.RequiredModule))
            return true;
        return IsModuleAccessible(field.RequiredModule);
    }

    private IEnumerable<FieldGroup> GetFilteredDatabaseFieldGroups()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return DatabaseFieldGroups;

        return DatabaseFieldGroups
            .Select(g => new FieldGroup
            {
                GroupName = g.GroupName,
                GroupIcon = g.GroupIcon,
                GroupColor = g.GroupColor,
                RequiredModule = g.RequiredModule,
                Fields = g.Fields.Where(f =>
                    f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList()
            })
            .Where(g => g.Fields.Any());
    }

    private int GetDatabaseFieldCount()
    {
        return GetFilteredDatabaseFieldGroups().Sum(g => g.Fields.Count);
    }

    private int GetAccessibleDatabaseFieldCount()
    {
        return GetFilteredDatabaseFieldGroups()
            .Where(g => IsModuleAccessible(g.RequiredModule))
            .Sum(g => g.Fields.Count);
    }

    // ===== ALL SECTION LAYOUTS (Regular + Header + Footer) =====

    /// <summary>
    /// Get all section layouts (regular, header, and footer) combined and filtered by search
    /// </summary>
    private IEnumerable<SectionLayoutDefinition> GetAllSectionLayouts()
    {
        // Combine all layouts: Header first, then Regular, then Footer
        var allLayouts = HeaderLayouts
            .Concat(SectionLayouts)
            .Concat(FooterLayouts);

        if (string.IsNullOrWhiteSpace(searchTerm))
            return allLayouts;

        return allLayouts.Where(s =>
            s.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            s.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }
}
