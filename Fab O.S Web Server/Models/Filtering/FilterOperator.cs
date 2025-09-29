namespace FabOS.WebServer.Models.Filtering;

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