using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Estimate;

/// <summary>
/// Registry of available CatalogueItem fields that can be linked to worksheet columns.
/// Provides grouped field definitions for UI selection and data extraction.
/// </summary>
public static class CatalogueFieldRegistry
{
    /// <summary>
    /// Get all available catalogue fields organized by category.
    /// </summary>
    public static List<CatalogueFieldGroup> GetAvailableFields()
    {
        return new List<CatalogueFieldGroup>
        {
            new("Identity", new[]
            {
                new CatalogueFieldInfo("ItemCode", "Item Code", "text"),
                new CatalogueFieldInfo("Description", "Description", "text"),
                new CatalogueFieldInfo("Category", "Category", "text"),
                new CatalogueFieldInfo("Profile", "Profile/Size", "text"),
                new CatalogueFieldInfo("SubCategory", "Sub-Category", "text"),
            }, "fas fa-tag"),

            new("Material", new[]
            {
                new CatalogueFieldInfo("Material", "Material", "text"),
                new CatalogueFieldInfo("Grade", "Grade", "text"),
                new CatalogueFieldInfo("Standard", "Standard", "text"),
                new CatalogueFieldInfo("Finish", "Finish", "text"),
                new CatalogueFieldInfo("Coating", "Coating", "text"),
            }, "fas fa-cube"),

            new("Weight", new[]
            {
                new CatalogueFieldInfo("Mass_kg_m", "Weight (kg/m)", "number", "#,##0.00", "kg/m"),
                new CatalogueFieldInfo("Weight_kg", "Weight (kg)", "number", "#,##0.00", "kg"),
                new CatalogueFieldInfo("Mass_kg_m2", "Weight (kg/m\u00b2)", "number", "#,##0.00", "kg/m\u00b2"),
            }, "fas fa-weight-hanging"),

            new("Length & Width", new[]
            {
                new CatalogueFieldInfo("Length_mm", "Length (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("Width_mm", "Width (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("Height_mm", "Height (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("Flange_mm", "Flange (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("Web_mm", "Web (mm)", "number", "#,##0", "mm"),
            }, "fas fa-ruler-horizontal"),

            new("Thickness", new[]
            {
                new CatalogueFieldInfo("Thickness_mm", "Thickness (mm)", "number", "#,##0.0", "mm"),
                new CatalogueFieldInfo("FlangeThickness_mm", "Flange Thickness (mm)", "number", "#,##0.0", "mm"),
                new CatalogueFieldInfo("WebThickness_mm", "Web Thickness (mm)", "number", "#,##0.0", "mm"),
                new CatalogueFieldInfo("WallThickness_mm", "Wall Thickness (mm)", "number", "#,##0.0", "mm"),
            }, "fas fa-layer-group"),

            new("Circular Sections", new[]
            {
                new CatalogueFieldInfo("Diameter_mm", "Diameter (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("OD_mm", "Outside Diameter (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("ID_mm", "Inside Diameter (mm)", "number", "#,##0", "mm"),
                new CatalogueFieldInfo("NominalBore", "Nominal Bore", "text"),
            }, "fas fa-circle"),

            new("Section Properties", new[]
            {
                new CatalogueFieldInfo("SectionArea_mm2", "Section Area (mm\u00b2)", "number", "#,##0", "mm\u00b2"),
                new CatalogueFieldInfo("Ix_mm4", "Moment of Inertia Ix (mm\u2074)", "number", "#,##0", "mm\u2074"),
                new CatalogueFieldInfo("Iy_mm4", "Moment of Inertia Iy (mm\u2074)", "number", "#,##0", "mm\u2074"),
                new CatalogueFieldInfo("Zx_mm3", "Section Modulus Zx (mm\u00b3)", "number", "#,##0", "mm\u00b3"),
                new CatalogueFieldInfo("Zy_mm3", "Section Modulus Zy (mm\u00b3)", "number", "#,##0", "mm\u00b3"),
                new CatalogueFieldInfo("rx_mm", "Radius of Gyration rx (mm)", "number", "#,##0.0", "mm"),
                new CatalogueFieldInfo("ry_mm", "Radius of Gyration ry (mm)", "number", "#,##0.0", "mm"),
            }, "fas fa-calculator"),

            new("Availability", new[]
            {
                new CatalogueFieldInfo("StandardLengths", "Standard Lengths", "text"),
                new CatalogueFieldInfo("Unit", "Unit of Measure", "text"),
                new CatalogueFieldInfo("Cut_To_Size", "Cut to Size", "text"),
                new CatalogueFieldInfo("LeadTime_Days", "Lead Time (Days)", "number"),
            }, "fas fa-truck"),

            new("Pricing", new[]
            {
                new CatalogueFieldInfo("UnitPrice", "Unit Price", "currency", "$#,##0.00"),
                new CatalogueFieldInfo("PricePerMeter", "Price per Meter", "currency", "$#,##0.00"),
                new CatalogueFieldInfo("PricePerKg", "Price per Kg", "currency", "$#,##0.00"),
            }, "fas fa-dollar-sign"),
        };
    }

    /// <summary>
    /// Get a flat list of all available field names.
    /// </summary>
    public static List<string> GetAllFieldNames()
    {
        return GetAvailableFields()
            .SelectMany(g => g.Fields)
            .Select(f => f.FieldName)
            .ToList();
    }

    /// <summary>
    /// Get field info by field name.
    /// </summary>
    public static CatalogueFieldInfo? GetFieldInfo(string fieldName)
    {
        return GetAvailableFields()
            .SelectMany(g => g.Fields)
            .FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get the value of a specific field from a CatalogueItem using reflection.
    /// </summary>
    public static object? GetFieldValue(CatalogueItem item, string fieldName)
    {
        if (item == null || string.IsNullOrEmpty(fieldName))
            return null;

        var property = typeof(CatalogueItem).GetProperty(fieldName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        return property?.GetValue(item);
    }

    /// <summary>
    /// Maps the catalogue field data type to worksheet column data type.
    /// </summary>
    public static string MapToWorksheetDataType(string catalogueFieldType)
    {
        return catalogueFieldType.ToLower() switch
        {
            "number" => "Number",
            "currency" => "Currency",
            "date" => "Date",
            "checkbox" or "boolean" => "Checkbox",
            _ => "Text"
        };
    }
}
