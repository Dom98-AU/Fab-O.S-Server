using System.Text.Json;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.Filtering;

namespace FabOS.WebServer.Models.ViewState;

public class ViewState : ISaveableViewState
{
    private string _viewId = Guid.NewGuid().ToString();

    public string ViewId => _viewId;
    public string ViewName { get; set; } = "";
    public bool IsDefault { get; set; }
    public List<ColumnDefinition> Columns { get; set; } = new();
    public List<FilterRule> Filters { get; set; } = new();
    public string? SortColumn { get; set; }
    public bool SortAscending { get; set; } = true;
    public int PageSize { get; set; } = 25;
    public int CurrentPage { get; set; } = 1;

    public string SerializeState()
    {
        return JsonSerializer.Serialize(this);
    }

    public void DeserializeState(string state)
    {
        var deserialized = JsonSerializer.Deserialize<ViewState>(state);
        if (deserialized != null)
        {
            Columns = deserialized.Columns;
            Filters = deserialized.Filters;
            SortColumn = deserialized.SortColumn;
            SortAscending = deserialized.SortAscending;
            PageSize = deserialized.PageSize;
            CurrentPage = deserialized.CurrentPage;
        }
    }

    public ViewState Clone()
    {
        var json = SerializeState();
        var clone = new ViewState();
        clone.DeserializeState(json);
        return clone;
    }

    public Dictionary<string, object?> GetViewState()
    {
        return new Dictionary<string, object?>
        {
            ["Columns"] = Columns,
            ["Filters"] = Filters,
            ["SortColumn"] = SortColumn,
            ["SortAscending"] = SortAscending,
            ["PageSize"] = PageSize,
            ["CurrentPage"] = CurrentPage
        };
    }

    public void RestoreViewState(Dictionary<string, object?> state)
    {
        if (state.ContainsKey("Columns") && state["Columns"] is List<ColumnDefinition> columns)
            Columns = columns;
        if (state.ContainsKey("Filters") && state["Filters"] is List<FilterRule> filters)
            Filters = filters;
        if (state.ContainsKey("SortColumn"))
            SortColumn = state["SortColumn"]?.ToString();
        if (state.ContainsKey("SortAscending") && state["SortAscending"] is bool sortAscending)
            SortAscending = sortAscending;
        if (state.ContainsKey("PageSize") && state["PageSize"] is int pageSize)
            PageSize = pageSize;
        if (state.ContainsKey("CurrentPage") && state["CurrentPage"] is int currentPage)
            CurrentPage = currentPage;
    }

    public void ResetToDefaults()
    {
        Columns.Clear();
        Filters.Clear();
        SortColumn = null;
        SortAscending = true;
        PageSize = 25;
        CurrentPage = 1;
        IsDefault = false;
    }
}