using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Models.Entities.Forms;

namespace FabOS.WebServer.Services.Interfaces.Forms;

/// <summary>
/// Service for managing form templates
/// </summary>
public interface IFormTemplateService
{
    /// <summary>
    /// Get all templates for a company, optionally filtered by module
    /// </summary>
    Task<FormTemplateListResponse> GetTemplatesAsync(
        int companyId,
        FormModuleContext? moduleContext = null,
        bool includeSystemTemplates = true,
        int page = 1,
        int pageSize = 20,
        string? search = null);

    /// <summary>
    /// Get a template by ID
    /// </summary>
    Task<FormTemplateDto?> GetTemplateByIdAsync(int id, int companyId);

    /// <summary>
    /// Get a template with its fields
    /// </summary>
    Task<FormTemplateDto?> GetTemplateWithFieldsAsync(int id, int companyId);

    /// <summary>
    /// Create a new form template
    /// </summary>
    Task<FormTemplateDto> CreateTemplateAsync(
        CreateFormTemplateRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Update an existing form template
    /// </summary>
    Task<FormTemplateDto> UpdateTemplateAsync(
        int id,
        UpdateFormTemplateRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Delete a form template (soft delete)
    /// </summary>
    Task<bool> DeleteTemplateAsync(int id, int companyId);

    /// <summary>
    /// Duplicate a form template
    /// </summary>
    Task<FormTemplateDto> DuplicateTemplateAsync(
        int id,
        string newName,
        int companyId,
        int userId);

    /// <summary>
    /// Publish a form template (make it available for use)
    /// </summary>
    Task<FormTemplateDto> PublishTemplateAsync(int id, int companyId, int userId);

    /// <summary>
    /// Unpublish a form template
    /// </summary>
    Task<FormTemplateDto> UnpublishTemplateAsync(int id, int companyId, int userId);

    /// <summary>
    /// Set a template as the company default for its module/type
    /// </summary>
    Task<FormTemplateDto> SetAsDefaultAsync(int id, int companyId, int userId);

    /// <summary>
    /// Get the default template for a module/type
    /// </summary>
    Task<FormTemplateDto?> GetDefaultTemplateAsync(
        int companyId,
        FormModuleContext moduleContext,
        string? formType = null);

    /// <summary>
    /// Add a field to a template
    /// </summary>
    Task<FormTemplateFieldDto> AddFieldAsync(
        int templateId,
        CreateFormTemplateFieldRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Update a field in a template
    /// </summary>
    Task<FormTemplateFieldDto> UpdateFieldAsync(
        int templateId,
        int fieldId,
        UpdateFormTemplateFieldRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Delete a field from a template (soft delete)
    /// </summary>
    Task<bool> DeleteFieldAsync(int templateId, int fieldId, int companyId);

    /// <summary>
    /// Reorder fields within a template
    /// </summary>
    Task<bool> ReorderFieldsAsync(
        int templateId,
        List<int> fieldIds,
        int companyId,
        int userId);

    /// <summary>
    /// Get form types used in the company
    /// </summary>
    Task<List<string>> GetFormTypesAsync(int companyId, FormModuleContext? moduleContext = null);

    /// <summary>
    /// Get available page size presets
    /// </summary>
    IEnumerable<FormPageSizePreset> GetPageSizePresets();

    #region Section Methods

    /// <summary>
    /// Get a template with its sections and fields organized by section
    /// </summary>
    Task<FormTemplateDto?> GetTemplateWithSectionsAsync(int id, int companyId);

    /// <summary>
    /// Add a section to a template
    /// </summary>
    Task<FormTemplateSectionDto> AddSectionAsync(
        int templateId,
        CreateFormTemplateSectionRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Update a section in a template
    /// </summary>
    Task<FormTemplateSectionDto> UpdateSectionAsync(
        int templateId,
        int sectionId,
        UpdateFormTemplateSectionRequest request,
        int companyId,
        int userId);

    /// <summary>
    /// Delete a section from a template
    /// </summary>
    /// <param name="deleteFields">If true, delete fields in the section. If false, move them to unassigned.</param>
    Task<bool> DeleteSectionAsync(int templateId, int sectionId, int companyId, bool deleteFields = false);

    /// <summary>
    /// Reorder sections within a template
    /// </summary>
    Task<bool> ReorderSectionsAsync(
        int templateId,
        List<int> sectionIds,
        int companyId,
        int userId);

    /// <summary>
    /// Move a field to a specific section and column position
    /// </summary>
    Task<bool> MoveFieldToSectionAsync(
        int templateId,
        int fieldId,
        int targetSectionId,
        int columnIndex,
        int rowIndex,
        int companyId,
        int userId);

    /// <summary>
    /// Get available section layout types
    /// </summary>
    IEnumerable<SectionLayoutOption> GetSectionLayoutOptions();

    #endregion
}
