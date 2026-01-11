using System.Text;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;

namespace FabOS.WebServer.Services.Implementations.Forms;

/// <summary>
/// Service for generating HTML exports of forms (for Nutrient PDF conversion)
/// </summary>
public class FormPdfService : IFormPdfService
{
    private readonly ApplicationDbContext _context;
    private readonly IFormInstanceService _instanceService;
    private readonly IFormTemplateService _templateService;
    private readonly ILogger<FormPdfService> _logger;

    public FormPdfService(
        ApplicationDbContext context,
        IFormInstanceService instanceService,
        IFormTemplateService templateService,
        ILogger<FormPdfService> logger)
    {
        _context = context;
        _instanceService = instanceService;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<string> GenerateFormHtmlAsync(int formInstanceId, int companyId, FormExportOptions? options = null)
    {
        options ??= new FormExportOptions();

        var form = await _instanceService.GetInstanceByIdAsync(formInstanceId, companyId);
        if (form == null)
        {
            throw new InvalidOperationException($"Form instance {formInstanceId} not found");
        }

        var company = await _context.Companies.FindAsync(companyId);

        var html = new StringBuilder();

        // Start HTML document
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"<title>{form.FormNumber} - {form.Template?.Name}</title>");
        html.AppendLine(GetPdfStyles(
            form.Template?.PageWidthMm ?? 210,
            form.Template?.PageHeightMm ?? 297,
            form.Template?.PageOrientation ?? "Portrait"));
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Header
        if (options.IncludeHeader)
        {
            html.AppendLine("<div class=\"form-header\">");
            html.AppendLine($"<div class=\"company-name\">{company?.Name ?? "Company"}</div>");
            html.AppendLine($"<div class=\"form-title\">{form.Template?.Name}</div>");
            html.AppendLine("<div class=\"form-meta\">");
            html.AppendLine($"<span class=\"form-number\">{form.FormNumber}</span>");
            html.AppendLine($"<span class=\"form-status badge badge-{GetStatusColor(form.Status)}\">{form.StatusName}</span>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        // Linked entity info
        if (!string.IsNullOrEmpty(form.LinkedEntityDisplay))
        {
            html.AppendLine("<div class=\"linked-entity\">");
            html.AppendLine($"<strong>Linked to:</strong> {form.LinkedEntityDisplay}");
            html.AppendLine("</div>");
        }

        // Form sections and fields
        if (form.Template?.Fields != null && form.Values != null)
        {
            var fieldValues = form.Values.ToDictionary(v => v.FieldKey, v => v);
            var sections = form.Template.Fields
                .Select(f => f.SectionName ?? "")
                .Distinct()
                .OrderBy(s => form.Template.Fields.FirstOrDefault(f => f.SectionName == s)?.SectionOrder ?? 0);

            foreach (var section in sections)
            {
                html.AppendLine("<div class=\"form-section\">");

                if (form.Template.ShowSectionHeaders && !string.IsNullOrEmpty(section))
                {
                    html.AppendLine($"<div class=\"section-header\">{section}</div>");
                }

                html.AppendLine("<div class=\"section-fields\">");

                var fields = form.Template.Fields
                    .Where(f => (f.SectionName ?? "") == section && f.IsVisible)
                    .OrderBy(f => f.DisplayOrder);

                foreach (var field in fields)
                {
                    fieldValues.TryGetValue(field.FieldKey, out var value);
                    html.AppendLine(RenderFieldHtml(field, value, options));
                }

                html.AppendLine("</div>");
                html.AppendLine("</div>");
            }
        }

        // Notes section
        if (!string.IsNullOrEmpty(form.Notes))
        {
            html.AppendLine("<div class=\"form-section\">");
            html.AppendLine("<div class=\"section-header\">Notes</div>");
            html.AppendLine("<div class=\"notes-content\">");
            html.AppendLine($"<p>{form.Notes}</p>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        // Approval history
        if (options.IncludeApprovalHistory && form.Status != FormInstanceStatus.Draft)
        {
            html.AppendLine("<div class=\"form-section\">");
            html.AppendLine("<div class=\"section-header\">Approval History</div>");
            html.AppendLine("<div class=\"timeline\">");

            if (form.SubmittedDate.HasValue)
            {
                html.AppendLine("<div class=\"timeline-item\">");
                html.AppendLine($"<div class=\"timeline-date\">{form.SubmittedDate:MMM dd, yyyy HH:mm}</div>");
                html.AppendLine($"<div class=\"timeline-content\"><strong>Submitted</strong> by {form.SubmittedByName ?? "Unknown"}</div>");
                html.AppendLine("</div>");
            }

            if (form.ReviewedDate.HasValue)
            {
                html.AppendLine("<div class=\"timeline-item\">");
                html.AppendLine($"<div class=\"timeline-date\">{form.ReviewedDate:MMM dd, yyyy HH:mm}</div>");
                html.AppendLine($"<div class=\"timeline-content\"><strong>Reviewed</strong> by {form.ReviewedByName ?? "Unknown"}</div>");
                html.AppendLine("</div>");
            }

            if (form.ApprovedDate.HasValue)
            {
                html.AppendLine("<div class=\"timeline-item\">");
                html.AppendLine($"<div class=\"timeline-date\">{form.ApprovedDate:MMM dd, yyyy HH:mm}</div>");
                html.AppendLine($"<div class=\"timeline-content\"><strong>Approved</strong> by {form.ApprovedByName ?? "Unknown"}</div>");
                html.AppendLine("</div>");
            }

            if (form.Status == FormInstanceStatus.Rejected && !string.IsNullOrEmpty(form.RejectionReason))
            {
                html.AppendLine("<div class=\"timeline-item rejected\">");
                html.AppendLine($"<div class=\"timeline-date\">{form.ReviewedDate:MMM dd, yyyy HH:mm}</div>");
                html.AppendLine($"<div class=\"timeline-content\"><strong>Rejected</strong> by {form.ReviewedByName ?? "Unknown"}</div>");
                html.AppendLine($"<div class=\"rejection-reason\">{form.RejectionReason}</div>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");
            html.AppendLine("</div>");
        }

        // Footer
        if (options.IncludeFooter)
        {
            html.AppendLine("<div class=\"form-footer\">");
            html.AppendLine($"<div>Generated: {DateTime.Now:MMM dd, yyyy HH:mm}</div>");
            html.AppendLine($"<div>Form: {form.FormNumber}</div>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GenerateBlankFormHtmlAsync(int templateId, int companyId)
    {
        var template = await _templateService.GetTemplateWithFieldsAsync(templateId, companyId);
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found");
        }

        var company = await _context.Companies.FindAsync(companyId);

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"UTF-8\">");
        html.AppendLine($"<title>{template.Name} - Blank Form</title>");
        html.AppendLine(GetPdfStyles(
            template.PageWidthMm,
            template.PageHeightMm,
            template.PageOrientation));
        html.AppendLine("</head>");
        html.AppendLine("<body>");

        // Header
        html.AppendLine("<div class=\"form-header\">");
        html.AppendLine($"<div class=\"company-name\">{company?.Name ?? "Company"}</div>");
        html.AppendLine($"<div class=\"form-title\">{template.Name}</div>");
        html.AppendLine("<div class=\"form-meta\">");
        html.AppendLine("<span class=\"form-number\">Form #: _____________</span>");
        html.AppendLine("<span class=\"form-date\">Date: _____________</span>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");

        // Render sections with empty fields
        if (template.Fields != null)
        {
            var sections = template.Fields
                .Select(f => f.SectionName ?? "")
                .Distinct()
                .OrderBy(s => template.Fields.FirstOrDefault(f => f.SectionName == s)?.SectionOrder ?? 0);

            foreach (var section in sections)
            {
                html.AppendLine("<div class=\"form-section\">");

                if (template.ShowSectionHeaders && !string.IsNullOrEmpty(section))
                {
                    html.AppendLine($"<div class=\"section-header\">{section}</div>");
                }

                html.AppendLine("<div class=\"section-fields\">");

                var fields = template.Fields
                    .Where(f => (f.SectionName ?? "") == section && f.IsVisible)
                    .OrderBy(f => f.DisplayOrder);

                foreach (var field in fields)
                {
                    html.AppendLine(RenderBlankFieldHtml(field));
                }

                html.AppendLine("</div>");
                html.AppendLine("</div>");
            }
        }

        // Notes section
        if (template.AllowNotes)
        {
            html.AppendLine("<div class=\"form-section\">");
            html.AppendLine("<div class=\"section-header\">Notes</div>");
            html.AppendLine("<div class=\"notes-box\"></div>");
            html.AppendLine("</div>");
        }

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    public async Task<string> GenerateFormWithWorksheetHtmlAsync(int formInstanceId, int worksheetId, int companyId)
    {
        // Generate form HTML
        var formHtml = await GenerateFormHtmlAsync(formInstanceId, companyId);

        // TODO: Integrate worksheet HTML generation when WorksheetPdfService is implemented
        // For now, return just the form HTML

        return formHtml;
    }

    private string RenderFieldHtml(FormTemplateFieldDto field, FormInstanceValueDto? value, FormExportOptions options)
    {
        var html = new StringBuilder();
        var widthClass = GetWidthClass(field.Width);

        html.AppendLine($"<div class=\"field {widthClass}\">");
        html.AppendLine($"<div class=\"field-label\">{field.DisplayName}{(field.IsRequired ? " *" : "")}</div>");
        html.AppendLine("<div class=\"field-value\">");

        switch (field.DataType)
        {
            case FormFieldDataType.Text:
            case FormFieldDataType.MultilineText:
                html.AppendLine($"<span>{value?.TextValue ?? "-"}</span>");
                break;

            case FormFieldDataType.Number:
                var numVal = value?.NumberValue;
                var formatted = numVal.HasValue
                    ? numVal.Value.ToString(field.DecimalPlaces.HasValue ? $"N{field.DecimalPlaces}" : "N")
                    : "-";
                html.AppendLine($"<span>{formatted}</span>");
                break;

            case FormFieldDataType.Currency:
                var curVal = value?.NumberValue;
                var curFormatted = curVal.HasValue
                    ? $"{field.CurrencySymbol ?? "$"}{curVal.Value:N2}"
                    : "-";
                html.AppendLine($"<span>{curFormatted}</span>");
                break;

            case FormFieldDataType.Percentage:
                var pctVal = value?.NumberValue;
                html.AppendLine($"<span>{(pctVal.HasValue ? $"{pctVal.Value:N1}%" : "-")}</span>");
                break;

            case FormFieldDataType.Date:
                html.AppendLine($"<span>{(value?.DateValue.HasValue == true ? value.DateValue.Value.ToString("MMM dd, yyyy") : "-")}</span>");
                break;

            case FormFieldDataType.DateTime:
                html.AppendLine($"<span>{(value?.DateValue.HasValue == true ? value.DateValue.Value.ToString("MMM dd, yyyy HH:mm") : "-")}</span>");
                break;

            case FormFieldDataType.Checkbox:
                var isChecked = value?.BoolValue ?? false;
                html.AppendLine($"<span class=\"checkbox {(isChecked ? "checked" : "")}\">{(isChecked ? "[X]" : "[ ]")}</span>");
                break;

            case FormFieldDataType.Choice:
            case FormFieldDataType.MultiChoice:
                html.AppendLine($"<span>{value?.TextValue ?? "-"}</span>");
                break;

            case FormFieldDataType.PassFail:
                if (value?.PassFailValue.HasValue == true)
                {
                    var passFail = value.PassFailValue.Value ? "PASS" : "FAIL";
                    var pfClass = value.PassFailValue.Value ? "pass" : "fail";
                    html.AppendLine($"<span class=\"pass-fail {pfClass}\">{passFail}</span>");
                    if (!string.IsNullOrEmpty(value.PassFailComment))
                    {
                        html.AppendLine($"<div class=\"pass-fail-comment\">{value.PassFailComment}</div>");
                    }
                }
                else
                {
                    html.AppendLine("<span>-</span>");
                }
                break;

            case FormFieldDataType.Signature:
                if (options.IncludeSignatures && !string.IsNullOrEmpty(value?.SignatureDataUrl))
                {
                    html.AppendLine($"<img src=\"{value.SignatureDataUrl}\" class=\"signature-image\" alt=\"Signature\" />");
                }
                else if (!string.IsNullOrEmpty(value?.SignatureDataUrl))
                {
                    html.AppendLine("<span class=\"signed\">[Signed]</span>");
                }
                else
                {
                    html.AppendLine("<span>-</span>");
                }
                break;

            case FormFieldDataType.Photo:
                if (options.IncludePhotos && value?.Attachments != null && value.Attachments.Any())
                {
                    html.AppendLine("<div class=\"photo-grid\">");
                    foreach (var photo in value.Attachments)
                    {
                        if (!string.IsNullOrEmpty(photo.StoragePath))
                        {
                            html.AppendLine($"<img src=\"{photo.StoragePath}\" class=\"photo\" alt=\"{photo.FileName}\" />");
                        }
                    }
                    html.AppendLine("</div>");
                }
                else
                {
                    var photoCount = value?.Attachments?.Count ?? 0;
                    html.AppendLine($"<span>{(photoCount > 0 ? $"{photoCount} photo(s)" : "-")}</span>");
                }
                break;

            case FormFieldDataType.Location:
                html.AppendLine($"<span>{value?.JsonValue?.ToString() ?? "-"}</span>");
                break;

            case FormFieldDataType.Computed:
                var compVal = value?.NumberValue;
                html.AppendLine($"<span class=\"computed\">{(compVal.HasValue ? compVal.Value.ToString("N2") : "-")}</span>");
                break;

            case FormFieldDataType.WorksheetLink:
                html.AppendLine("<span>[See attached worksheet]</span>");
                break;

            default:
                html.AppendLine($"<span>{value?.TextValue ?? "-"}</span>");
                break;
        }

        html.AppendLine("</div>");
        html.AppendLine("</div>");

        return html.ToString();
    }

    private string RenderBlankFieldHtml(FormTemplateFieldDto field)
    {
        var html = new StringBuilder();
        var widthClass = GetWidthClass(field.Width);

        html.AppendLine($"<div class=\"field {widthClass}\">");
        html.AppendLine($"<div class=\"field-label\">{field.DisplayName}{(field.IsRequired ? " *" : "")}</div>");
        html.AppendLine("<div class=\"field-input\">");

        switch (field.DataType)
        {
            case FormFieldDataType.Text:
                html.AppendLine("<div class=\"input-line\"></div>");
                break;

            case FormFieldDataType.MultilineText:
                html.AppendLine("<div class=\"input-box\"></div>");
                break;

            case FormFieldDataType.Checkbox:
                html.AppendLine("<span class=\"checkbox\">[ ]</span>");
                break;

            case FormFieldDataType.PassFail:
                html.AppendLine("<span class=\"pass-fail-options\">[ ] Pass  [ ] Fail</span>");
                break;

            case FormFieldDataType.Signature:
                html.AppendLine("<div class=\"signature-box\">Signature:</div>");
                break;

            case FormFieldDataType.Photo:
                html.AppendLine("<div class=\"photo-box\">[Attach photos]</div>");
                break;

            case FormFieldDataType.Choice:
                html.AppendLine("<div class=\"select-options\">");
                if (field.SelectOptions != null)
                {
                    foreach (var option in field.SelectOptions)
                    {
                        html.AppendLine($"<span>[ ] {option}</span>");
                    }
                }
                html.AppendLine("</div>");
                break;

            default:
                html.AppendLine("<div class=\"input-line\"></div>");
                break;
        }

        html.AppendLine("</div>");
        html.AppendLine("</div>");

        return html.ToString();
    }

    private string GetWidthClass(string width)
    {
        return width switch
        {
            "half" => "width-half",
            "third" => "width-third",
            _ => "width-full"
        };
    }

    private string GetStatusColor(FormInstanceStatus status)
    {
        return status switch
        {
            FormInstanceStatus.Draft => "secondary",
            FormInstanceStatus.Submitted => "info",
            FormInstanceStatus.UnderReview => "warning",
            FormInstanceStatus.Approved => "success",
            FormInstanceStatus.Rejected => "danger",
            _ => "secondary"
        };
    }

    private string GetPdfStyles(decimal widthMm = 210, decimal heightMm = 297, string orientation = "Portrait")
    {
        // Swap dimensions for landscape orientation
        var pageWidth = orientation == "Landscape" ? heightMm : widthMm;
        var pageHeight = orientation == "Landscape" ? widthMm : heightMm;

        // Build @page rule separately to avoid escaping issues
        var pageRule = $"@page {{ size: {pageWidth}mm {pageHeight}mm; margin: 15mm; }}";

        return @"
<style>
    " + pageRule + @"

    * {
        box-sizing: border-box;
        margin: 0;
        padding: 0;
    }

    body {
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
        font-size: 11pt;
        line-height: 1.4;
        color: #333;
        padding: 20px;
    }

    .form-header {
        border-bottom: 2px solid #0D1A80;
        padding-bottom: 15px;
        margin-bottom: 20px;
    }

    .company-name {
        font-size: 14pt;
        font-weight: bold;
        color: #0D1A80;
    }

    .form-title {
        font-size: 18pt;
        font-weight: bold;
        margin: 10px 0;
    }

    .form-meta {
        display: flex;
        gap: 20px;
        align-items: center;
    }

    .form-number {
        font-weight: bold;
    }

    .badge {
        padding: 4px 12px;
        border-radius: 4px;
        font-size: 10pt;
        font-weight: bold;
    }

    .badge-secondary { background: #6c757d; color: white; }
    .badge-info { background: #17a2b8; color: white; }
    .badge-warning { background: #ffc107; color: #212529; }
    .badge-success { background: #28a745; color: white; }
    .badge-danger { background: #dc3545; color: white; }

    .linked-entity {
        background: #f8f9fa;
        padding: 10px 15px;
        border-radius: 4px;
        margin-bottom: 20px;
    }

    .form-section {
        background: white;
        border: 1px solid #dee2e6;
        border-radius: 6px;
        margin-bottom: 20px;
        page-break-inside: avoid;
    }

    .section-header {
        background: #f8f9fa;
        padding: 10px 15px;
        font-weight: bold;
        border-bottom: 1px solid #dee2e6;
        font-size: 12pt;
    }

    .section-fields {
        padding: 15px;
        display: flex;
        flex-wrap: wrap;
        gap: 15px;
    }

    .field {
        margin-bottom: 10px;
    }

    .width-full { width: 100%; }
    .width-half { width: calc(50% - 8px); }
    .width-third { width: calc(33.333% - 10px); }

    .field-label {
        font-weight: 600;
        font-size: 10pt;
        color: #555;
        margin-bottom: 5px;
    }

    .field-value {
        font-size: 11pt;
    }

    .pass-fail.pass {
        color: #28a745;
        font-weight: bold;
    }

    .pass-fail.fail {
        color: #dc3545;
        font-weight: bold;
    }

    .pass-fail-comment {
        font-size: 9pt;
        color: #666;
        margin-top: 5px;
        font-style: italic;
    }

    .signature-image {
        max-width: 200px;
        max-height: 60px;
        border: 1px solid #ddd;
    }

    .signed {
        color: #28a745;
        font-weight: bold;
    }

    .photo-grid {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
    }

    .photo {
        max-width: 150px;
        max-height: 150px;
        border: 1px solid #ddd;
        border-radius: 4px;
    }

    .computed {
        font-family: 'Courier New', monospace;
        background: #f8f9fa;
        padding: 2px 6px;
        border-radius: 3px;
    }

    .timeline {
        padding: 10px 15px;
    }

    .timeline-item {
        padding: 10px 0;
        border-bottom: 1px solid #eee;
    }

    .timeline-item:last-child {
        border-bottom: none;
    }

    .timeline-date {
        font-size: 9pt;
        color: #666;
    }

    .timeline-content {
        font-size: 10pt;
    }

    .rejection-reason {
        background: #f8d7da;
        color: #721c24;
        padding: 8px;
        border-radius: 4px;
        margin-top: 5px;
        font-size: 10pt;
    }

    .notes-content, .notes-box {
        padding: 15px;
    }

    .notes-box {
        border: 1px dashed #ccc;
        min-height: 100px;
    }

    .form-footer {
        margin-top: 30px;
        padding-top: 15px;
        border-top: 1px solid #dee2e6;
        display: flex;
        justify-content: space-between;
        font-size: 9pt;
        color: #666;
    }

    /* Blank form styles */
    .input-line {
        border-bottom: 1px solid #333;
        min-height: 25px;
    }

    .input-box {
        border: 1px solid #333;
        min-height: 60px;
    }

    .signature-box {
        border: 1px solid #333;
        min-height: 50px;
        padding: 5px;
    }

    .photo-box {
        border: 1px dashed #999;
        min-height: 50px;
        padding: 10px;
        text-align: center;
        color: #999;
    }

    .select-options {
        display: flex;
        flex-direction: column;
        gap: 5px;
    }

    .pass-fail-options {
        display: flex;
        gap: 20px;
    }

    @media print {
        body { padding: 0; }
        .form-section { page-break-inside: avoid; }
    }
</style>";
    }
}
