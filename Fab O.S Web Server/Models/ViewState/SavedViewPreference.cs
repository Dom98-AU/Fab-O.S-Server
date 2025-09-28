using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.ViewState;

public class SavedViewPreference
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string ViewType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }

    [NotMapped]
    public ViewState? ViewState { get; set; }

    public string? ViewStateJson { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
}