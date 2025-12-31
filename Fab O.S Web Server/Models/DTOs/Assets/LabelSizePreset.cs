using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Preset label size option
/// </summary>
public class LabelSizePreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }
}
