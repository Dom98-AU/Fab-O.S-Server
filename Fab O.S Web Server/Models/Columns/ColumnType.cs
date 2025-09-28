namespace FabOS.WebServer.Models.Columns;

public enum ColumnType
{
    Text,
    Number,
    Currency,
    Percentage,
    Date,
    DateTime,
    Boolean,
    Status,
    Badge,
    Link,
    Image,
    Actions,
    Custom
}

public enum FreezePosition
{
    None,
    Left,
    Right
}