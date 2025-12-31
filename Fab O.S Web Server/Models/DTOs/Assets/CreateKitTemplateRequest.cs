using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CreateKitTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconClass { get; set; }
    public int DefaultCheckoutDays { get; set; } = 7;
    public bool RequiresSignature { get; set; } = true;
    public bool RequiresConditionCheck { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
