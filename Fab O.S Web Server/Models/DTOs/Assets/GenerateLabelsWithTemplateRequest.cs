using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for generating labels with a template
/// </summary>
public class GenerateLabelsWithTemplateRequest
{
    /// <summary>
    /// Template ID to use (takes priority over TemplateName)
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Template name to use (if TemplateId not provided)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// List of entity IDs to generate labels for
    /// </summary>
    public List<int> EntityIds { get; set; } = new();
}
