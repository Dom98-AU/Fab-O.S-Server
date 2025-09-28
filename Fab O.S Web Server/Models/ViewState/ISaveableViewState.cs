using System.Collections.Generic;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.Filtering;

namespace FabOS.WebServer.Models.ViewState;

public interface ISaveableViewState
{
    string ViewId { get; }
    string ViewName { get; set; }
    bool IsDefault { get; set; }
    Dictionary<string, object?> GetViewState();
    void RestoreViewState(Dictionary<string, object?> state);
    void ResetToDefaults();
}

public class ViewStateData
{
    public string ViewId { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public List<FilterRule>? Filters { get; set; }
    public List<ColumnDefinition>? Columns { get; set; }
    public string? SortColumn { get; set; }
    public bool? SortAscending { get; set; }
    public int? PageSize { get; set; }
    public Dictionary<string, object?>? CustomSettings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}