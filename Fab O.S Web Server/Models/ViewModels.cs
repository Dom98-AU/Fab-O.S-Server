using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Models
{
    public class FilterGroup
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public FilterType Type { get; set; }
        public List<FilterOption> Options { get; set; } = new();
    }

    public class FilterOption
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public enum FilterType
    {
        Select,
        DateRange,
        Checkbox,
        Search
    }

    public class GenericDisplayConfig<T>
    {
        public List<TableColumn<T>> Columns { get; set; } = new();
        public Func<T, RenderFragment>? CardTemplate { get; set; }
        public Func<T, RenderFragment>? ListTemplate { get; set; }
        public string? EmptyMessage { get; set; }
        public bool AllowSelection { get; set; } = false;
        public bool ShowPagination { get; set; } = true;
        public int PageSize { get; set; } = 20;
        
        // Additional properties used in Takeoffs.razor
        public Func<T, string>? TitleSelector { get; set; }
        public Func<T, string>? SubtitleSelector { get; set; }
        public Func<T, string>? StatusSelector { get; set; }
        public Func<T, string>? IconSelector { get; set; }
        public List<PropertyConfig<T>> Properties { get; set; } = new();
        public List<StatConfig<T>> Stats { get; set; } = new();
        
        // Card-specific properties for GenericCard compatibility
        public string PrimaryProperty { get; set; } = "";
        public string SecondaryProperty { get; set; } = "";
        public string TertiaryProperty { get; set; } = "";
        public string IconProperty { get; set; } = "";
        public string StatusProperty { get; set; } = "";
        public string DefaultIcon { get; set; } = "fas fa-cube";
    }

    public class TableColumn<T>
    {
        public string Header { get; set; } = string.Empty;
        public Func<T, object?>? ValueSelector { get; set; }
        public Func<T, RenderFragment>? Template { get; set; }
        public string? Width { get; set; }
        public bool IsSortable { get; set; } = true;
        public string? SortKey { get; set; }
        public string? CssClass { get; set; }
        
        // Additional properties used in Takeoffs.razor
        public string? Key { get; set; }
        public bool Sortable { get; set; } = true;
        public Func<T, RenderFragment>? CellTemplate { get; set; }
        
        // GenericTableView compatibility properties
        public string Title { get; set; } = "";
        public string PropertyPath { get; set; } = "";
        public string Format { get; set; } = "";
    }

    public enum ViewMode
    {
        Table,
        Card,
        List
    }

    public class PropertyConfig<T>
    {
        public string Label { get; set; } = string.Empty;
        public Func<T, object?>? ValueSelector { get; set; }
        public string? Icon { get; set; }
    }

    public class StatConfig<T>
    {
        public string Label { get; set; } = string.Empty;
        public Func<T, object?>? ValueSelector { get; set; }
        public StatType Type { get; set; }
        public string? Icon { get; set; }
        public Func<T, string>? FormatSelector { get; set; }
        
        // Card-specific properties for GenericCard compatibility
        public string PropertyPath { get; set; } = "";
        public string Value { get; set; } = "";
        public bool IsPercentage { get; set; } = false;
        public bool IsCurrency { get; set; } = false;
        public string CssClass { get; set; } = "";
    }

    public enum StatType
    {
        Count,
        Sum,
        Average,
        Percentage
    }
}
