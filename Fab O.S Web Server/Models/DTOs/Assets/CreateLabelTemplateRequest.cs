using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for creating a label template
/// </summary>
public class CreateLabelTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = "All";

    // Dimensions (custom sizes)
    public decimal WidthMm { get; set; } = 50;
    public decimal HeightMm { get; set; } = 25;

    // QR Code settings
    public bool IncludeQRCode { get; set; } = true;
    public int QRCodePixelsPerModule { get; set; } = 10;

    // Field visibility
    public bool IncludeCode { get; set; } = true;
    public bool IncludeName { get; set; } = true;
    public bool IncludeCategory { get; set; } = true;
    public bool IncludeLocation { get; set; } = true;
    public bool IncludeSerialNumber { get; set; } = false;
    public bool IncludeServiceDate { get; set; } = false;
    public bool IncludeContactInfo { get; set; } = false;

    // Styling
    public int PrimaryFontSize { get; set; } = 8;
    public int SecondaryFontSize { get; set; } = 6;
    public decimal MarginMm { get; set; } = 2;

    // Set as default for entity type
    public bool SetAsDefault { get; set; }
}
