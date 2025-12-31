using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Response DTO for label templates
/// </summary>
public class LabelTemplateResponseDto
{
    public int Id { get; set; }
    public int? CompanyId { get; set; }
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

    // Flags
    public bool IsSystemTemplate { get; set; }
    public bool IsDefault { get; set; }

    // Audit
    public DateTime CreatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? LastModified { get; set; }
    public int? LastModifiedByUserId { get; set; }
}
