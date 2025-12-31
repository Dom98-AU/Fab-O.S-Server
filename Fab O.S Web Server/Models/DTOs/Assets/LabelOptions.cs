using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class LabelOptions
{
    public double? WidthMm { get; set; }
    public double? HeightMm { get; set; }
    public string? Template { get; set; }
    public bool IncludeLogo { get; set; }
    public bool IncludeQRCode { get; set; } = true;
    public bool IncludeServiceDate { get; set; } = true;
    public bool IncludeLocation { get; set; } = true;
    public bool IncludeCategory { get; set; } = true;
    public int LabelsPerRow { get; set; } = 2;
    public int RowsPerPage { get; set; } = 5;
    public double MarginMm { get; set; } = 5;
}
