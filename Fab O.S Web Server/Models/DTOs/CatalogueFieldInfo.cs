namespace FabOS.WebServer.Models.DTOs;

/// <summary>
/// Information about a catalogue field that can be linked to a worksheet column.
/// </summary>
public class CatalogueFieldInfo
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "text";
    public string? Format { get; set; }
    public string? Unit { get; set; }

    public CatalogueFieldInfo() { }

    public CatalogueFieldInfo(string fieldName, string displayName, string dataType, string? format = null, string? unit = null)
    {
        FieldName = fieldName;
        DisplayName = displayName;
        DataType = dataType;
        Format = format;
        Unit = unit;
    }
}

/// <summary>
/// A group of related catalogue fields for organized display in UI.
/// </summary>
public class CatalogueFieldGroup
{
    public string GroupName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public List<CatalogueFieldInfo> Fields { get; set; } = new();

    public CatalogueFieldGroup() { }

    public CatalogueFieldGroup(string groupName, IEnumerable<CatalogueFieldInfo> fields, string? icon = null)
    {
        GroupName = groupName;
        Fields = fields.ToList();
        Icon = icon;
    }
}
