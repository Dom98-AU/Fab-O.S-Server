using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CancelCheckoutRequest
{
    public string? Reason { get; set; }
}
