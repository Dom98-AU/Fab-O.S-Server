using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for managing label templates
/// </summary>
public interface ILabelTemplateService
{
    #region CRUD Operations

    /// <summary>
    /// Creates a new label template for a company
    /// </summary>
    Task<LabelTemplate> CreateAsync(int companyId, CreateLabelTemplateRequest request, int userId);

    /// <summary>
    /// Gets a label template by ID
    /// </summary>
    Task<LabelTemplate?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a label template by name (searches company templates first, then system templates)
    /// </summary>
    Task<LabelTemplate?> GetByNameAsync(int companyId, string name);

    /// <summary>
    /// Gets all templates available for a company (company-specific + system templates)
    /// </summary>
    Task<IEnumerable<LabelTemplate>> GetAllForCompanyAsync(int companyId, string? entityType = null, bool includeSystem = true);

    /// <summary>
    /// Gets only system templates
    /// </summary>
    Task<IEnumerable<LabelTemplate>> GetSystemTemplatesAsync(string? entityType = null);

    /// <summary>
    /// Updates an existing label template
    /// </summary>
    Task<LabelTemplate> UpdateAsync(int id, UpdateLabelTemplateRequest request, int userId);

    /// <summary>
    /// Soft deletes a label template (cannot delete system templates)
    /// </summary>
    Task<bool> DeleteAsync(int id);

    #endregion

    #region Default Template Management

    /// <summary>
    /// Gets the default template for a company and entity type
    /// Falls back to system "Standard" template if no default is set
    /// </summary>
    Task<LabelTemplate?> GetDefaultTemplateAsync(int companyId, string entityType);

    /// <summary>
    /// Sets a template as the default for its entity type within a company
    /// </summary>
    Task SetAsDefaultAsync(int id, int companyId);

    /// <summary>
    /// Clears the default flag for all templates of a given entity type
    /// </summary>
    Task ClearDefaultAsync(int companyId, string entityType);

    #endregion

    #region Template Resolution

    /// <summary>
    /// Resolves the best template to use based on provided options
    /// Priority: TemplateId -> TemplateName -> Company Default -> System "Standard"
    /// </summary>
    Task<LabelTemplate> ResolveTemplateAsync(int companyId, int? templateId, string? templateName, string entityType);

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a template name already exists for a company
    /// </summary>
    Task<bool> TemplateNameExistsAsync(int companyId, string name, int? excludeId = null);

    /// <summary>
    /// Validates that a template can be modified (not a system template)
    /// </summary>
    bool CanModifyTemplate(LabelTemplate template);

    #endregion

    #region Preset Sizes

    /// <summary>
    /// Gets available preset label sizes
    /// </summary>
    IEnumerable<LabelSizePreset> GetPresetSizes();

    #endregion
}
