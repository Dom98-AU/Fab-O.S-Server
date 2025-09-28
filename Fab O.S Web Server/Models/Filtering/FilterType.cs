namespace FabOS.WebServer.Models.Filtering;

public enum FilterType
{
    Text,
    Number,
    Date,
    DateTime,
    Boolean,
    Select,
    MultiSelect,
    Currency,
    Percentage
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Between,
    In,
    NotIn,
    IsNull,
    IsNotNull,
    IsTrue,
    IsFalse
}

public enum LogicalOperator
{
    And,
    Or
}