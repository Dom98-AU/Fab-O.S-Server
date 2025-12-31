using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

/// <summary>
/// Request DTO for updating attachment metadata
/// </summary>
public class UpdateAttachmentRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public AttachmentCategory? Category { get; set; }

    // Certificate-specific
    public DateTime? ExpiryDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? IssuingAuthority { get; set; }
}
