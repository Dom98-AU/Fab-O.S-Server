using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class LabelOptionsDto
{
    public bool IncludeQRCode { get; set; } = true;
    public bool IncludeCategory { get; set; } = true;
    public bool IncludeLocation { get; set; } = true;
    public bool IncludeNextServiceDate { get; set; } = true;
    public bool IncludeSerialNumber { get; set; } = true;
    public bool IncludeBarcode { get; set; }
    public string? CustomField1Label { get; set; }
    public string? CustomField1Value { get; set; }
    public string? CustomField2Label { get; set; }
    public string? CustomField2Value { get; set; }
}
