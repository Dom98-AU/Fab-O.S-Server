namespace FabOS.WebServer.Models.Filtering;

public class FilterFieldDefinition
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public List<string>? AvailableValues { get; set; }
}