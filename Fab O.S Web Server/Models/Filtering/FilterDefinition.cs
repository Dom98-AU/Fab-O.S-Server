namespace FabOS.WebServer.Models.Filtering;

public class FilterDefinition
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FilterType Type { get; set; } = FilterType.Text;
    public bool IsRequired { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, string>? Options { get; set; }
    public object? DefaultValue { get; set; }
    public string? Format { get; set; }
    public string? Group { get; set; }
    public int Order { get; set; }

    public List<FilterOperator> GetAvailableOperators()
    {
        return Type switch
        {
            FilterType.Text => new List<FilterOperator>
            {
                FilterOperator.Contains,
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.StartsWith,
                FilterOperator.EndsWith,
                FilterOperator.NotContains
            },
            FilterType.Number or FilterType.Currency or FilterType.Percentage => new List<FilterOperator>
            {
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.GreaterThan,
                FilterOperator.LessThan,
                FilterOperator.GreaterThanOrEqual,
                FilterOperator.LessThanOrEqual,
                FilterOperator.Between
            },
            FilterType.Date or FilterType.DateTime => new List<FilterOperator>
            {
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.GreaterThan,
                FilterOperator.LessThan,
                FilterOperator.GreaterThanOrEqual,
                FilterOperator.LessThanOrEqual,
                FilterOperator.Between
            },
            FilterType.Boolean => new List<FilterOperator>
            {
                FilterOperator.IsTrue,
                FilterOperator.IsFalse
            },
            FilterType.Select => new List<FilterOperator>
            {
                FilterOperator.Equals,
                FilterOperator.NotEquals,
                FilterOperator.In,
                FilterOperator.NotIn
            },
            FilterType.MultiSelect => new List<FilterOperator>
            {
                FilterOperator.In,
                FilterOperator.NotIn
            },
            _ => new List<FilterOperator> { FilterOperator.Equals }
        };
    }
}

public class FilterFieldDefinition
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public Type? PropertyType { get; set; }
    public bool IsFilterable { get; set; } = true;
    public List<FilterOperator> AllowedOperators { get; set; } = new();
}