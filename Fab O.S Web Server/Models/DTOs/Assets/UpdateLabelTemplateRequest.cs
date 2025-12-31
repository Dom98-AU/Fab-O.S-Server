using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for updating a label template
/// </summary>
public class UpdateLabelTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = "All";

    // Dimensions
    public decimal WidthMm { get; set; }
    public decimal HeightMm { get; set; }

    // QR Code settings
    public bool IncludeQRCode { get; set; }
    public int QRCodePixelsPerModule { get; set; }

    // Field visibility
    public bool IncludeCode { get; set; }
    public bool IncludeName { get; set; }
    public bool IncludeCategory { get; set; }
    public bool IncludeLocation { get; set; }
    public bool IncludeSerialNumber { get; set; }
    public bool IncludeServiceDate { get; set; }
    public bool IncludeContactInfo { get; set; }

    // Styling
    public int PrimaryFontSize { get; set; }
    public int SecondaryFontSize { get; set; }
    public decimal MarginMm { get; set; }

    // Set as default
    public bool SetAsDefault { get; set; }
}
