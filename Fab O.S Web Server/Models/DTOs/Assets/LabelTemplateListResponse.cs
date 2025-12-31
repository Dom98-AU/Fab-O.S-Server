using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// List response for label templates
/// </summary>
public class LabelTemplateListResponse
{
    public List<LabelTemplateResponseDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
