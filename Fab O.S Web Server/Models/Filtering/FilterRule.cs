namespace FabOS.WebServer.Models.Filtering;

public class FilterRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Field { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty; // Alternative property name
    public FilterOperator Operator { get; set; } = FilterOperator.Equals;
    public string? Value { get; set; }
    public string? SecondValue { get; set; } // For "between" operator
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
    public string? LogicalOperatorString { get; set; } // Alternative for string-based
    public bool IsActive { get; set; } = true;

    public string GetDisplayText()
    {
        var valueText = Value?.ToString() ?? "null";
        var operatorText = Operator switch
        {
            FilterOperator.Equals => "equals",
            FilterOperator.NotEquals => "not equals",
            FilterOperator.Contains => "contains",
            FilterOperator.NotContains => "doesn't contain",
            FilterOperator.StartsWith => "starts with",
            FilterOperator.EndsWith => "ends with",
            FilterOperator.GreaterThan => ">",
            FilterOperator.LessThan => "<",
            FilterOperator.GreaterThanOrEqual => ">=",
            FilterOperator.LessThanOrEqual => "<=",
            FilterOperator.Between => $"between {valueText} and {SecondValue}",
            FilterOperator.In => "is one of",
            FilterOperator.NotIn => "is not one of",
            FilterOperator.IsNull => "is empty",
            FilterOperator.IsNotNull => "is not empty",
            FilterOperator.IsTrue => "is yes",
            FilterOperator.IsFalse => "is no",
            _ => "equals"
        };

        if (Operator == FilterOperator.Between)
        {
            return $"{Field} {operatorText}";
        }
        else if (Operator == FilterOperator.IsNull || Operator == FilterOperator.IsNotNull ||
                 Operator == FilterOperator.IsTrue || Operator == FilterOperator.IsFalse)
        {
            return $"{Field} {operatorText}";
        }
        else
        {
            return $"{Field} {operatorText} {valueText}";
        }
    }
}