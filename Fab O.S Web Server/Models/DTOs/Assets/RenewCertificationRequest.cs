using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class RenewCertificationRequest
{
    public DateTime NewIssueDate { get; set; }
    public DateTime? NewExpiryDate { get; set; }
    public string? NewCertificateNumber { get; set; }
    public string? DocumentUrl { get; set; }
}
