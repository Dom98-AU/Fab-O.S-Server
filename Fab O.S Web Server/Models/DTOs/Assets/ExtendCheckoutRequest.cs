using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class ExtendCheckoutRequest
{
    public DateTime NewExpectedReturnDate { get; set; }
    public string? Reason { get; set; }
}
