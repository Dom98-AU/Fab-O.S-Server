using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class ConfirmCheckoutRequest
{
    public string Signature { get; set; } = string.Empty; // Base64 encoded
}
