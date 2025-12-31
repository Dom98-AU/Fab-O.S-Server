using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class LabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public bool SupportsQRCode { get; set; }
    public bool SupportsBarcode { get; set; }
    public int MaxCustomFields { get; set; }
}
