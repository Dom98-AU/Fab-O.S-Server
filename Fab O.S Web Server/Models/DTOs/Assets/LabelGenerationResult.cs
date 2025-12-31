using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class LabelGenerationResult
{
    public bool Success { get; set; }
    public string? PdfBase64 { get; set; }
    public string? FileName { get; set; }
    public string? ErrorMessage { get; set; }
    public int LabelCount { get; set; }
}
