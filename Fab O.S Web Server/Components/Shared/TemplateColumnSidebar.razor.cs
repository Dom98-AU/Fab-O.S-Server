using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FabOS.WebServer.Components.Shared;

public partial class TemplateColumnSidebar : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public bool IsVisible { get; set; } = true;
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public EventCallback<FieldDefinition> OnFieldDragStart { get; set; }
    [Parameter] public EventCallback OnFieldDragEnd { get; set; }

    private string searchTerm = "";
    private HashSet<string> expandedSections = new() { "catalogue", "custom" };
    private HashSet<string> expandedGroups = new() { "cat_Identification", "custom_Text" };
    private FieldDefinition? draggedField;
    private IJSObjectReference? jsModule;
    private bool previousIsVisible;

    // Field definition model
    public class FieldDefinition
    {
        public string FieldName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string DataType { get; set; } = "text";
        public string Description { get; set; } = "";
        public bool IsCatalogueField { get; set; }
        public string? CatalogueFieldMapping { get; set; }
        public string? DefaultFormula { get; set; }
        public string? DefaultOptions { get; set; }
    }

    public class FieldGroup
    {
        public string GroupName { get; set; } = "";
        public List<FieldDefinition> Fields { get; set; } = new();
    }

    // Catalogue fields organized by group
    private static readonly List<FieldGroup> CatalogueFieldGroups = new()
    {
        new FieldGroup
        {
            GroupName = "Identification",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "ItemCode", DisplayName = "Item Code", DataType = "text", Description = "Catalogue item code", IsCatalogueField = true, CatalogueFieldMapping = "ItemCode" },
                new() { FieldName = "Description", DisplayName = "Description", DataType = "text", Description = "Item description", IsCatalogueField = true, CatalogueFieldMapping = "Description" },
                new() { FieldName = "Material", DisplayName = "Material", DataType = "text", Description = "Material type (Steel, Aluminium, etc.)", IsCatalogueField = true, CatalogueFieldMapping = "Material" },
                new() { FieldName = "Category", DisplayName = "Category", DataType = "text", Description = "Item category", IsCatalogueField = true, CatalogueFieldMapping = "Category" },
                new() { FieldName = "Profile", DisplayName = "Profile", DataType = "text", Description = "Profile type (UB, UC, PFC, etc.)", IsCatalogueField = true, CatalogueFieldMapping = "Profile" }
            }
        },
        new FieldGroup
        {
            GroupName = "Dimensions",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "Width_mm", DisplayName = "Width (mm)", DataType = "number", Description = "Width in millimeters", IsCatalogueField = true, CatalogueFieldMapping = "Width_mm" },
                new() { FieldName = "Height_mm", DisplayName = "Height (mm)", DataType = "number", Description = "Height in millimeters", IsCatalogueField = true, CatalogueFieldMapping = "Height_mm" },
                new() { FieldName = "Length_m", DisplayName = "Length (m)", DataType = "number", Description = "Length in meters", IsCatalogueField = true, CatalogueFieldMapping = "StandardLength_m" },
                new() { FieldName = "Thickness_mm", DisplayName = "Thickness (mm)", DataType = "number", Description = "Thickness in millimeters", IsCatalogueField = true, CatalogueFieldMapping = "Thickness_mm" },
                new() { FieldName = "Flange_mm", DisplayName = "Flange (mm)", DataType = "number", Description = "Flange width in millimeters", IsCatalogueField = true, CatalogueFieldMapping = "Flange_mm" },
                new() { FieldName = "Web_mm", DisplayName = "Web (mm)", DataType = "number", Description = "Web thickness in millimeters", IsCatalogueField = true, CatalogueFieldMapping = "Web_mm" }
            }
        },
        new FieldGroup
        {
            GroupName = "Weight & Mass",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "Mass_kg_m", DisplayName = "Mass (kg/m)", DataType = "number", Description = "Mass per meter", IsCatalogueField = true, CatalogueFieldMapping = "Mass_kg_m" },
                new() { FieldName = "Mass_kg_m2", DisplayName = "Mass (kg/m²)", DataType = "number", Description = "Mass per square meter", IsCatalogueField = true, CatalogueFieldMapping = "Mass_kg_m2" },
                new() { FieldName = "Weight_kg", DisplayName = "Weight (kg)", DataType = "number", Description = "Total weight in kg", IsCatalogueField = true, CatalogueFieldMapping = "Weight_kg" },
                new() { FieldName = "SurfaceArea_m2_per_m", DisplayName = "Surface Area (m²/m)", DataType = "number", Description = "Surface area per meter", IsCatalogueField = true, CatalogueFieldMapping = "SurfaceArea_m2_per_m" }
            }
        },
        new FieldGroup
        {
            GroupName = "Material Specs",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "Grade", DisplayName = "Grade", DataType = "text", Description = "Material grade (250, 300, 350, etc.)", IsCatalogueField = true, CatalogueFieldMapping = "Grade" },
                new() { FieldName = "Standard", DisplayName = "Standard", DataType = "text", Description = "Standard (AS/NZS 3678, etc.)", IsCatalogueField = true, CatalogueFieldMapping = "Standard" },
                new() { FieldName = "Finish", DisplayName = "Finish", DataType = "text", Description = "Surface finish", IsCatalogueField = true, CatalogueFieldMapping = "Finish" }
            }
        }
    };

    // Custom fields organized by group
    private static readonly List<FieldGroup> CustomFieldGroups = new()
    {
        new FieldGroup
        {
            GroupName = "Text",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "text", DisplayName = "Text (Single Line)", DataType = "text", Description = "Single line text input" },
                new() { FieldName = "multiline", DisplayName = "Multiline Text", DataType = "multiline", Description = "Multi-line text area" },
                new() { FieldName = "notes", DisplayName = "Notes", DataType = "multiline", Description = "Notes field with larger input" }
            }
        },
        new FieldGroup
        {
            GroupName = "Numbers",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "number", DisplayName = "Number", DataType = "number", Description = "Numeric value" },
                new() { FieldName = "currency", DisplayName = "Currency", DataType = "currency", Description = "Currency value with formatting" },
                new() { FieldName = "percentage", DisplayName = "Percentage", DataType = "number", Description = "Percentage value (0-100)" },
                new() { FieldName = "quantity", DisplayName = "Quantity", DataType = "number", Description = "Quantity field" },
                new() { FieldName = "computed", DisplayName = "Computed (Formula)", DataType = "computed", Description = "Calculated from formula" }
            }
        },
        new FieldGroup
        {
            GroupName = "Costing",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "unit_cost", DisplayName = "Unit Cost", DataType = "currency", Description = "Cost per unit (manual entry)" },
                new() { FieldName = "unit_price", DisplayName = "Unit Price", DataType = "currency", Description = "Selling price per unit (manual entry)" },
                new() { FieldName = "total_cost", DisplayName = "Total Cost", DataType = "computed", Description = "Total cost (qty × unit cost)", DefaultFormula = "qty * unit_cost" },
                new() { FieldName = "total_price", DisplayName = "Total Price", DataType = "computed", Description = "Total price (qty × unit price)", DefaultFormula = "qty * unit_price" },
                new() { FieldName = "margin", DisplayName = "Margin %", DataType = "number", Description = "Profit margin percentage" }
            }
        },
        new FieldGroup
        {
            GroupName = "Selection",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "choice", DisplayName = "Choice (Dropdown)", DataType = "select", Description = "Single selection dropdown", DefaultOptions = "Option 1,Option 2,Option 3" },
                new() { FieldName = "multichoice", DisplayName = "Multi-Choice", DataType = "multiselect", Description = "Multiple selection", DefaultOptions = "Option A,Option B,Option C" },
                new() { FieldName = "checkbox", DisplayName = "Checkbox", DataType = "checkbox", Description = "Yes/No checkbox" },
                new() { FieldName = "yesno", DisplayName = "Yes/No", DataType = "select", Description = "Yes/No dropdown", DefaultOptions = "Yes,No" }
            }
        },
        new FieldGroup
        {
            GroupName = "Date & Time",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "date", DisplayName = "Date", DataType = "date", Description = "Date picker" },
                new() { FieldName = "datetime", DisplayName = "Date & Time", DataType = "datetime", Description = "Date and time picker" },
                new() { FieldName = "time", DisplayName = "Time", DataType = "time", Description = "Time picker" }
            }
        },
        new FieldGroup
        {
            GroupName = "Other",
            Fields = new List<FieldDefinition>
            {
                new() { FieldName = "lookup", DisplayName = "Lookup (Entity)", DataType = "lookup", Description = "Reference to another entity" },
                new() { FieldName = "person", DisplayName = "Person", DataType = "person", Description = "User or contact selection" },
                new() { FieldName = "image", DisplayName = "Image", DataType = "image", Description = "Image attachment" },
                new() { FieldName = "location", DisplayName = "Location", DataType = "location", Description = "GPS coordinates or address" }
            }
        }
    };

    private async Task ToggleSidebar()
    {
        IsVisible = !IsVisible;
        await IsVisibleChanged.InvokeAsync(IsVisible);
        await UpdateBodyClasses();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/template-sidebar-helpers.js");
                previousIsVisible = IsVisible;
                await UpdateBodyClasses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateColumnSidebar] Error loading JS module: {ex.Message}");
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        // React to IsVisible parameter changes from parent component
        if (jsModule != null && previousIsVisible != IsVisible)
        {
            previousIsVisible = IsVisible;
            await UpdateBodyClasses();
        }
    }

    private async Task UpdateBodyClasses()
    {
        if (jsModule != null)
        {
            try
            {
                await jsModule.InvokeVoidAsync("updateTemplateSidebarState", IsVisible);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateColumnSidebar] Error updating body classes: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (jsModule != null)
        {
            try
            {
                await jsModule.InvokeVoidAsync("cleanupTemplateSidebar");
                await jsModule.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateColumnSidebar] Error during cleanup: {ex.Message}");
            }
        }
    }

    private void ToggleSection(string sectionName)
    {
        if (expandedSections.Contains(sectionName))
            expandedSections.Remove(sectionName);
        else
            expandedSections.Add(sectionName);
    }

    private void ToggleGroup(string groupName)
    {
        if (expandedGroups.Contains(groupName))
            expandedGroups.Remove(groupName);
        else
            expandedGroups.Add(groupName);
    }

    private void ClearSearch()
    {
        searchTerm = "";
    }

    private void HandleDragStart(FieldDefinition field)
    {
        draggedField = field;
        OnFieldDragStart.InvokeAsync(field);
    }

    private void HandleDragEnd()
    {
        draggedField = null;
        OnFieldDragEnd.InvokeAsync();
    }

    private IEnumerable<FieldDefinition> GetFilteredCatalogueFields()
    {
        var allFields = CatalogueFieldGroups.SelectMany(g => g.Fields);
        if (string.IsNullOrWhiteSpace(searchTerm))
            return allFields;

        return allFields.Where(f =>
            f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            f.FieldName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<FieldDefinition> GetFilteredCustomFields()
    {
        var allFields = CustomFieldGroups.SelectMany(g => g.Fields);
        if (string.IsNullOrWhiteSpace(searchTerm))
            return allFields;

        return allFields.Where(f =>
            f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            f.FieldName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<FieldGroup> GetFilteredCatalogueFieldGroups()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return CatalogueFieldGroups;

        return CatalogueFieldGroups
            .Select(g => new FieldGroup
            {
                GroupName = g.GroupName,
                Fields = g.Fields.Where(f =>
                    f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.FieldName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList()
            })
            .Where(g => g.Fields.Any());
    }

    private IEnumerable<FieldGroup> GetFilteredCustomFieldGroups()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return CustomFieldGroups;

        return CustomFieldGroups
            .Select(g => new FieldGroup
            {
                GroupName = g.GroupName,
                Fields = g.Fields.Where(f =>
                    f.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.FieldName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    f.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList()
            })
            .Where(g => g.Fields.Any());
    }

    private string GetFieldIcon(string dataType)
    {
        return dataType.ToLower() switch
        {
            "text" => "fas fa-font",
            "multiline" => "fas fa-align-left",
            "number" => "fas fa-hashtag",
            "currency" => "fas fa-dollar-sign",
            "computed" => "fas fa-calculator",
            "select" => "fas fa-list",
            "multiselect" => "fas fa-check-double",
            "checkbox" => "fas fa-check-square",
            "date" => "fas fa-calendar",
            "datetime" => "fas fa-calendar-alt",
            "time" => "fas fa-clock",
            "lookup" => "fas fa-link",
            "person" => "fas fa-user",
            "image" => "fas fa-image",
            "location" => "fas fa-map-marker-alt",
            _ => "fas fa-columns"
        };
    }

    // Public method to get the currently dragged field (for parent component)
    public FieldDefinition? GetDraggedField() => draggedField;
}
