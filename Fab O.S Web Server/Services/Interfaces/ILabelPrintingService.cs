using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for Label generation and printing
/// </summary>
public interface ILabelPrintingService
{
    #region Legacy Methods (Backward Compatibility)

    /// <summary>
    /// Generates a label PDF for a single equipment item
    /// </summary>
    Task<LabelGenerationResult> GenerateLabelAsync(int equipmentId, string template = "Standard", LabelOptionsDto? options = null);

    /// <summary>
    /// Generates a label PDF for multiple equipment items (batch printing)
    /// </summary>
    Task<LabelGenerationResult> GenerateBatchLabelsAsync(IEnumerable<int> equipmentIds, string template = "Standard", LabelOptionsDto? options = null);

    /// <summary>
    /// Gets available label templates (legacy - returns static templates)
    /// </summary>
    IEnumerable<LabelTemplateDto> GetAvailableTemplates();

    /// <summary>
    /// Gets a specific label template by name (legacy)
    /// </summary>
    LabelTemplateDto? GetTemplate(string templateName);

    /// <summary>
    /// Validates label options for a given template
    /// </summary>
    bool ValidateOptions(string templateName, LabelOptionsDto options);

    /// <summary>
    /// Gets default label options for a template
    /// </summary>
    LabelOptionsDto GetDefaultOptions(string templateName);

    /// <summary>
    /// Generates labels for all equipment in a category
    /// </summary>
    Task<LabelGenerationResult> GenerateLabelsByCategoryAsync(int categoryId, int companyId, string template = "Standard", LabelOptionsDto? options = null);

    /// <summary>
    /// Generates labels for all equipment in a location
    /// </summary>
    Task<LabelGenerationResult> GenerateLabelsByLocationAsync(string location, int companyId, string template = "Standard", LabelOptionsDto? options = null);

    /// <summary>
    /// Generates labels for equipment needing maintenance
    /// </summary>
    Task<LabelGenerationResult> GenerateMaintenanceDueLabelsAsync(int companyId, int daysAhead = 7, string template = "Standard", LabelOptionsDto? options = null);

    #endregion

    #region Template-Based Label Generation

    /// <summary>
    /// Generates labels for equipment using a database template
    /// </summary>
    Task<LabelGenerationResult> GenerateEquipmentLabelsAsync(
        int companyId,
        IEnumerable<int> equipmentIds,
        LabelTemplate template);

    /// <summary>
    /// Generates labels for equipment kits using a database template
    /// </summary>
    Task<LabelGenerationResult> GenerateKitLabelsAsync(
        int companyId,
        IEnumerable<int> kitIds,
        LabelTemplate template);

    /// <summary>
    /// Generates labels for locations using a database template
    /// </summary>
    Task<LabelGenerationResult> GenerateLocationLabelsAsync(
        int companyId,
        IEnumerable<int> locationIds,
        LabelTemplate template);

    #endregion
}
