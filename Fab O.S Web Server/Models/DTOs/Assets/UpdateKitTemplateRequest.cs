using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class UpdateKitTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconClass { get; set; }
    public int DefaultCheckoutDays { get; set; }
    public bool RequiresSignature { get; set; }
    public bool RequiresConditionCheck { get; set; }
    public bool IsActive { get; set; }
}
