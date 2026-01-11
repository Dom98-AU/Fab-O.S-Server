using FabOS.WebServer.Models.DTOs.Forms;

namespace FabOS.WebServer.Services.Interfaces.Forms;

/// <summary>
/// Service for generating HTML exports of forms (for Nutrient PDF conversion)
/// </summary>
public interface IFormPdfService
{
    /// <summary>
    /// Generate HTML representation of a form instance (for Nutrient PDF export)
    /// </summary>
    /// <param name="formInstanceId">The form instance ID</param>
    /// <param name="companyId">The company ID</param>
    /// <param name="options">Export options</param>
    /// <returns>HTML string ready for PDF conversion</returns>
    Task<string> GenerateFormHtmlAsync(int formInstanceId, int companyId, FormExportOptions? options = null);

    /// <summary>
    /// Generate HTML for a blank form template (for printing)
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <param name="companyId">The company ID</param>
    /// <returns>HTML string ready for PDF conversion</returns>
    Task<string> GenerateBlankFormHtmlAsync(int templateId, int companyId);

    /// <summary>
    /// Generate HTML for a form with linked worksheet data
    /// </summary>
    /// <param name="formInstanceId">The form instance ID</param>
    /// <param name="worksheetId">The worksheet ID</param>
    /// <param name="companyId">The company ID</param>
    /// <returns>Combined HTML string</returns>
    Task<string> GenerateFormWithWorksheetHtmlAsync(int formInstanceId, int worksheetId, int companyId);
}

/// <summary>
/// Options for form export
/// </summary>
public class FormExportOptions
{
    /// <summary>
    /// Include company header/logo
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// Include form footer with page numbers
    /// </summary>
    public bool IncludeFooter { get; set; } = true;

    /// <summary>
    /// Include approval history section
    /// </summary>
    public bool IncludeApprovalHistory { get; set; } = true;

    /// <summary>
    /// Include attached photos inline
    /// </summary>
    public bool IncludePhotos { get; set; } = true;

    /// <summary>
    /// Include signatures as images
    /// </summary>
    public bool IncludeSignatures { get; set; } = true;

    /// <summary>
    /// Maximum photo size in pixels
    /// </summary>
    public int MaxPhotoSize { get; set; } = 300;
}
