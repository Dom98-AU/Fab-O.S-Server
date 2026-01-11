using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;
using System.Security.Claims;
using System.Text.Json;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Forms;

public partial class FormDesigner : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IFormTemplateService TemplateService { get; set; } = default!;
    [Inject] private ILogger<FormDesigner> Logger { get; set; } = default!;

    // State
    private bool isLoading = true;
    private bool isEditMode = true;
    private bool hasUnsavedChanges = false;
    private bool sidebarVisible = true;
    private string? errorMessage;

    // Data
    private FormTemplateDto? template;
    private List<FormTemplateSectionDto> sections = new();
    private FormTemplateFieldDto? selectedField;
    private FormTemplateSectionDto? selectedSection;

    // Dialog state
    private bool showAddSectionDialog = false;
    private bool showSectionPropertiesPanel = false;
    private bool showMarginControls = false;
    private string newSectionName = "";
    private string newSectionLayout = "1-col";

    // Page size state
    private List<FormPageSizePreset> pageSizePresets = new();
    private string selectedPageSize = "A4";
    private string selectedOrientation = "Portrait";

    // Zoom state
    private double zoomLevel = 1.0;
    private const double ZoomMin = 0.5;
    private const double ZoomMax = 2.0;
    private const double ZoomStep = 0.1;

    // Drag-drop state
    private FormFieldDataType? draggedNewFieldType;
    private FormTemplateFieldDto? draggedField;
    private FormFieldSidebar.SectionLayoutDefinition? draggedSectionLayout;
    private bool isDraggingFromSidebar = false;
    private bool isDraggingSectionFromSidebar = false;
    private int? dropTargetSectionId = null;
    private int dropTargetColumnIndex = 0;

    // Sidebar reference
    private FormFieldSidebar? fieldSidebar;

    // Helper for select options editing
    private string selectOptionsText = "";

    // Authentication
    private int currentUserId = 0;
    private int currentCompanyId = 0;
    private FormModuleContext currentModule = FormModuleContext.Estimate;

    // Counters for generating unique keys
    private int fieldKeyCounter = 0;
    private int sectionCounter = 0;

    // Check if template has content (sections with fields)
    private bool HasContent => sections.Any(s => s.Fields.Any());

    // Check if page size can be changed
    private bool CanChangePageSize => !HasContent;

    protected override async Task OnInitializedAsync()
    {
        await InitializeAuthState();

        if (currentCompanyId == 0)
        {
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        // Determine module from URL
        DetermineCurrentModule();

        // Load page size presets
        pageSizePresets = TemplateService.GetPageSizePresets().ToList();

        await LoadTemplate();
    }

    private async Task InitializeAuthState()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst("user_id") ?? user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }

            var companyIdClaim = user.FindFirst("company_id") ?? user.FindFirst("CompanyId");
            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
            {
                currentCompanyId = companyId;
            }
        }
    }

    private void DetermineCurrentModule()
    {
        var path = Navigation.Uri.ToLower();
        if (path.Contains("/estimate/"))
            currentModule = FormModuleContext.Estimate;
        else if (path.Contains("/fabmate/"))
            currentModule = FormModuleContext.FabMate;
        else if (path.Contains("/qdocs/"))
            currentModule = FormModuleContext.QDocs;
        else if (path.Contains("/assets/"))
            currentModule = FormModuleContext.Assets;
    }

    private async Task LoadTemplate()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            // Handle ID=0 as "create new template"
            if (Id == 0)
            {
                await CreateNewTemplate();
                return;
            }

            template = await TemplateService.GetTemplateWithSectionsAsync(Id, currentCompanyId);

            if (template != null)
            {
                sections = template.Sections.OrderBy(s => s.DisplayOrder).ToList();

                // Find max counters for generating new unique keys
                foreach (var section in sections)
                {
                    // Track section counter
                    if (section.Id > sectionCounter)
                        sectionCounter = section.Id;

                    foreach (var field in section.Fields)
                    {
                        if (field.FieldKey.StartsWith("field_") &&
                            int.TryParse(field.FieldKey.Replace("field_", ""), out var num))
                        {
                            if (num >= fieldKeyCounter)
                                fieldKeyCounter = num + 1;
                        }
                    }
                }
                sectionCounter++;
                fieldKeyCounter++;

                // Initialize page size selection from template
                InitializePageSizeFromTemplate();
            }
            else
            {
                errorMessage = "Template not found.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading template {TemplateId}", Id);
            errorMessage = "Error loading template. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void InitializePageSizeFromTemplate()
    {
        if (template == null) return;

        selectedOrientation = template.PageOrientation;

        // Find matching preset
        var matchingPreset = pageSizePresets.FirstOrDefault(p =>
            (selectedOrientation == "Portrait" && p.WidthMm == template.PageWidthMm && p.HeightMm == template.PageHeightMm) ||
            (selectedOrientation == "Landscape" && p.HeightMm == template.PageWidthMm && p.WidthMm == template.PageHeightMm));

        selectedPageSize = matchingPreset?.Name ?? "A4";
    }

    private async Task CreateNewTemplate()
    {
        try
        {
            var request = new CreateFormTemplateRequest
            {
                Name = "New Form Template",
                Description = null,
                ModuleContext = currentModule,
                FormType = null,
                NumberPrefix = "FORM"
            };

            var newTemplate = await TemplateService.CreateTemplateAsync(request, currentCompanyId, currentUserId);

            if (newTemplate != null)
            {
                Logger.LogInformation("Created new form template {TemplateId} for company {CompanyId}", newTemplate.Id, currentCompanyId);
                // Navigate to the new template's designer page (replacing the /0/ URL)
                Navigation.NavigateTo($"/{TenantSlug}/{GetModulePath()}/forms/templates/{newTemplate.Id}/design", replace: true);
            }
            else
            {
                errorMessage = "Error creating new template.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating new form template");
            errorMessage = "Error creating template. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    #region Drag and Drop

    // Field drag handlers
    private void StartDragField(FormTemplateFieldDto field)
    {
        draggedField = field;
        draggedNewFieldType = null;
        isDraggingFromSidebar = false;
    }

    private void HandleFieldDragEnd()
    {
        draggedField = null;
        StateHasChanged();
    }

    // Sidebar event handlers
    private void HandleSidebarVisibilityChanged(bool isVisible)
    {
        sidebarVisible = isVisible;
        StateHasChanged();
    }

    private void HandleSidebarDragStart(FormFieldSidebar.FieldDefinition field)
    {
        // Convert sidebar field definition to form field data type
        draggedNewFieldType = field.DataType;
        draggedField = null;
        isDraggingFromSidebar = true;
        isDraggingSectionFromSidebar = false;
        StateHasChanged();
    }

    private void HandleSidebarDragEnd()
    {
        // Only reset if drop didn't handle it
        if (isDraggingFromSidebar)
        {
            draggedNewFieldType = null;
            isDraggingFromSidebar = false;
            StateHasChanged();
        }
    }

    // Section drag handlers from sidebar
    private void HandleSectionDragStart(FormFieldSidebar.SectionLayoutDefinition sectionLayout)
    {
        draggedSectionLayout = sectionLayout;
        isDraggingSectionFromSidebar = true;
        isDraggingFromSidebar = false;
        StateHasChanged();
    }

    private void HandleSectionDragEnd()
    {
        if (isDraggingSectionFromSidebar)
        {
            draggedSectionLayout = null;
            isDraggingSectionFromSidebar = false;
            StateHasChanged();
        }
    }

    // Canvas drop handler for sections
    private void HandleCanvasDragOver(DragEventArgs e)
    {
        // Allow drop
    }

    private void HandleCanvasDrop(DragEventArgs e)
    {
        if (isDraggingSectionFromSidebar && draggedSectionLayout != null)
        {
            // Adding a new section from sidebar
            AddNewSectionFromLayout(draggedSectionLayout);
        }

        draggedSectionLayout = null;
        isDraggingSectionFromSidebar = false;
        draggedNewFieldType = null;
        isDraggingFromSidebar = false;
    }

    // Add a section from dragged layout
    private void AddNewSectionFromLayout(FormFieldSidebar.SectionLayoutDefinition layout)
    {
        // Prevent adding duplicate header/footer
        if (layout.SectionType == FormSectionType.Header && HasHeader)
        {
            return; // Header already exists
        }
        if (layout.SectionType == FormSectionType.Footer && HasFooter)
        {
            return; // Footer already exists
        }

        // Determine display order based on section type
        int displayOrder;
        if (layout.SectionType == FormSectionType.Header)
        {
            displayOrder = -1000; // Header always first
        }
        else if (layout.SectionType == FormSectionType.Footer)
        {
            displayOrder = 1000; // Footer always last
        }
        else
        {
            // Regular sections: place after last non-footer section
            var regularSections = sections.Where(s => !s.IsFooter);
            displayOrder = regularSections.Select(s => s.DisplayOrder).DefaultIfEmpty(0).Max() + 1;
        }

        // Normalize layout type (remove header-/footer- prefix for storage)
        var normalizedLayoutType = layout.LayoutType
            .Replace("header-", "")
            .Replace("footer-", "");

        var newSection = new FormTemplateSectionDto
        {
            Id = -(sectionCounter++), // Negative ID indicates new unsaved section
            FormTemplateId = Id,
            Name = layout.SectionType switch
            {
                FormSectionType.Header => "Page Header",
                FormSectionType.Footer => "Page Footer",
                _ => $"Section {sections.Count(s => !s.IsHeader && !s.IsFooter) + 1}"
            },
            LayoutType = normalizedLayoutType,
            DisplayOrder = displayOrder,
            KeepTogether = true,
            IsHeader = layout.SectionType == FormSectionType.Header,
            IsFooter = layout.SectionType == FormSectionType.Footer,
            Fields = new List<FormTemplateFieldDto>()
        };

        sections.Add(newSection);
        selectedSection = newSection;
        selectedField = null;
        MarkUnsaved();
    }

    // Computed properties for header/footer existence
    private bool HasHeader => sections.Any(s => s.IsHeader);
    private bool HasFooter => sections.Any(s => s.IsFooter);

    // Check if a section can be moved up (not header, and not first regular section)
    private bool CanMoveSectionUp(FormTemplateSectionDto section)
    {
        if (section.IsHeader || section.IsFooter) return false;
        var regularSections = sections.Where(s => !s.IsHeader && !s.IsFooter).OrderBy(s => s.DisplayOrder).ToList();
        return regularSections.IndexOf(section) > 0;
    }

    // Check if a section can be moved down (not footer, and not last regular section)
    private bool CanMoveSectionDown(FormTemplateSectionDto section)
    {
        if (section.IsHeader || section.IsFooter) return false;
        var regularSections = sections.Where(s => !s.IsHeader && !s.IsFooter).OrderBy(s => s.DisplayOrder).ToList();
        return regularSections.IndexOf(section) < regularSections.Count - 1;
    }

    // Section column drop handler - for dropping fields into specific columns
    private void HandleColumnDragOver(int sectionId, int columnIndex)
    {
        dropTargetSectionId = sectionId;
        dropTargetColumnIndex = columnIndex;
    }

    private void HandleColumnDragLeave()
    {
        dropTargetSectionId = null;
        dropTargetColumnIndex = 0;
    }

    private void HandleColumnDrop(int sectionId, int columnIndex)
    {
        var section = sections.FirstOrDefault(s => s.Id == sectionId);
        if (section == null) return;

        if (draggedNewFieldType.HasValue)
        {
            // Adding new field from sidebar to this column
            AddNewFieldToSection(draggedNewFieldType.Value, section, columnIndex);
        }
        else if (draggedField != null)
        {
            // Moving existing field to this column
            MoveFieldToSection(draggedField, section, columnIndex);
        }

        // Reset drag state
        draggedNewFieldType = null;
        draggedField = null;
        isDraggingFromSidebar = false;
        dropTargetSectionId = null;
        dropTargetColumnIndex = 0;
    }

    private void AddNewFieldToSection(FormFieldDataType fieldType, FormTemplateSectionDto section, int columnIndex)
    {
        var newKey = $"field_{fieldKeyCounter++}";
        var displayName = GetDefaultDisplayName(fieldType);
        var maxRowInColumn = section.Fields
            .Where(f => f.ColumnIndex == columnIndex)
            .Select(f => f.RowIndex)
            .DefaultIfEmpty(-1).Max();

        var newField = new FormTemplateFieldDto
        {
            Id = -fieldKeyCounter, // Negative ID indicates new unsaved field
            FormTemplateId = Id,
            FormTemplateSectionId = section.Id > 0 ? section.Id : null,
            FieldKey = newKey,
            DisplayName = displayName,
            DataType = fieldType,
            ColumnIndex = columnIndex,
            RowIndex = maxRowInColumn + 1,
            DisplayOrder = section.Fields.Count + 1,
            IsVisible = true,
            MaxPhotos = fieldType == FormFieldDataType.Photo ? 5 : null,
            DecimalPlaces = fieldType == FormFieldDataType.Number || fieldType == FormFieldDataType.Currency ? 2 : null
        };

        section.Fields.Add(newField);
        selectedField = newField;
        selectedSection = section;
        UpdateSelectOptionsText();
        MarkUnsaved();
    }

    private void MoveFieldToSection(FormTemplateFieldDto field, FormTemplateSectionDto targetSection, int columnIndex)
    {
        // Remove from current section
        foreach (var section in sections)
        {
            section.Fields.Remove(field);
        }

        // Update field properties
        field.FormTemplateSectionId = targetSection.Id > 0 ? targetSection.Id : null;
        field.ColumnIndex = columnIndex;
        field.RowIndex = targetSection.Fields
            .Where(f => f.ColumnIndex == columnIndex)
            .Select(f => f.RowIndex)
            .DefaultIfEmpty(-1).Max() + 1;

        // Add to target section
        targetSection.Fields.Add(field);
        MarkUnsaved();
    }

    private string GetDefaultDisplayName(FormFieldDataType fieldType)
    {
        return fieldType switch
        {
            FormFieldDataType.Text => "Text Field",
            FormFieldDataType.MultilineText => "Notes",
            FormFieldDataType.Number => "Number",
            FormFieldDataType.Currency => "Amount",
            FormFieldDataType.Percentage => "Percentage",
            FormFieldDataType.Date => "Date",
            FormFieldDataType.DateTime => "Date & Time",
            FormFieldDataType.Checkbox => "Checkbox",
            FormFieldDataType.Choice => "Select Option",
            FormFieldDataType.MultiChoice => "Select Multiple",
            FormFieldDataType.Signature => "Signature",
            FormFieldDataType.Photo => "Photo",
            FormFieldDataType.Location => "Location",
            FormFieldDataType.PassFail => "Pass/Fail Check",
            FormFieldDataType.Computed => "Calculated Value",
            FormFieldDataType.WorksheetLink => "Worksheet Link",
            // Header/Footer special fields
            FormFieldDataType.PageNumber => "Page Number",
            FormFieldDataType.CompanyLogo => "Company Logo",
            FormFieldDataType.DocumentTitle => "Document Title",
            FormFieldDataType.CurrentDate => "Current Date",
            FormFieldDataType.FormNumber => "Form Number",
            _ => "Field"
        };
    }

    #endregion

    #region Field Management

    private void SelectField(FormTemplateFieldDto field, FormTemplateSectionDto? section = null)
    {
        selectedField = field;
        selectedSection = section;
        UpdateSelectOptionsText();
    }

    private void DeleteField(FormTemplateFieldDto field)
    {
        // Remove from its section
        foreach (var section in sections)
        {
            if (section.Fields.Remove(field))
                break;
        }

        if (selectedField?.Id == field.Id)
        {
            selectedField = null;
        }
        MarkUnsaved();
    }

    private void UpdateSelectOptionsText()
    {
        if (selectedField?.SelectOptions != null && selectedField.SelectOptions.Any())
        {
            selectOptionsText = string.Join("\n", selectedField.SelectOptions);
        }
        else
        {
            selectOptionsText = "";
        }
    }

    private void UpdateSelectOptions()
    {
        if (selectedField != null)
        {
            selectedField.SelectOptions = selectOptionsText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            MarkUnsaved();
        }
    }

    #endregion

    #region Section Management

    private void SelectSection(FormTemplateSectionDto section)
    {
        selectedSection = section;
        selectedField = null;
        showSectionPropertiesPanel = true;
    }

    private void OpenAddSectionDialog()
    {
        newSectionName = "";
        newSectionLayout = "1-col";
        showAddSectionDialog = true;
    }

    private void CreateSection()
    {
        if (string.IsNullOrWhiteSpace(newSectionName))
            return;

        var maxOrder = sections.Select(s => s.DisplayOrder).DefaultIfEmpty(0).Max();

        var newSection = new FormTemplateSectionDto
        {
            Id = -(sectionCounter++),
            FormTemplateId = Id,
            Name = newSectionName.Trim(),
            LayoutType = newSectionLayout,
            DisplayOrder = maxOrder + 1,
            KeepTogether = true,
            Fields = new List<FormTemplateFieldDto>()
        };

        sections.Add(newSection);
        selectedSection = newSection;
        selectedField = null;
        showAddSectionDialog = false;
        MarkUnsaved();
    }

    private void DeleteSection(FormTemplateSectionDto section)
    {
        sections.Remove(section);
        if (selectedSection?.Id == section.Id)
        {
            selectedSection = null;
            showSectionPropertiesPanel = false;
        }
        MarkUnsaved();
    }

    private void MoveSectionUp(FormTemplateSectionDto section)
    {
        var index = sections.IndexOf(section);
        if (index > 0)
        {
            sections.RemoveAt(index);
            sections.Insert(index - 1, section);
            UpdateSectionDisplayOrders();
            MarkUnsaved();
        }
    }

    private void MoveSectionDown(FormTemplateSectionDto section)
    {
        var index = sections.IndexOf(section);
        if (index < sections.Count - 1)
        {
            sections.RemoveAt(index);
            sections.Insert(index + 1, section);
            UpdateSectionDisplayOrders();
            MarkUnsaved();
        }
    }

    private void UpdateSectionDisplayOrders()
    {
        for (int i = 0; i < sections.Count; i++)
        {
            sections[i].DisplayOrder = i + 1;
        }
    }

    private int GetSectionColumnCount(FormTemplateSectionDto section)
    {
        return section.LayoutType switch
        {
            "1-col" => 1,
            "2-col-equal" or "2-col-left" or "2-col-right" => 2,
            "3-col-equal" => 3,
            _ => 1
        };
    }

    private string GetSectionGridTemplateColumns(FormTemplateSectionDto section)
    {
        return section.LayoutType switch
        {
            "1-col" => "1fr",
            "2-col-equal" => "1fr 1fr",
            "2-col-left" => "2fr 1fr",
            "2-col-right" => "1fr 2fr",
            "3-col-equal" => "1fr 1fr 1fr",
            _ => "1fr"
        };
    }

    private IEnumerable<FormTemplateFieldDto> GetFieldsInColumn(FormTemplateSectionDto section, int columnIndex)
    {
        return section.Fields
            .Where(f => f.ColumnIndex == columnIndex)
            .OrderBy(f => f.RowIndex)
            .ThenBy(f => f.DisplayOrder);
    }

    private void CloseSectionPropertiesPanel()
    {
        showSectionPropertiesPanel = false;
        selectedSection = null;
    }

    private void HandleSectionUpdated(UpdateFormTemplateSectionRequest request)
    {
        if (selectedSection == null) return;

        // Apply updates to the selected section
        selectedSection.Name = request.Name;
        selectedSection.LayoutType = request.LayoutType;
        selectedSection.PageBreakBefore = request.PageBreakBefore;
        selectedSection.KeepTogether = request.KeepTogether;
        selectedSection.IsCollapsible = request.IsCollapsible;
        selectedSection.IsCollapsedByDefault = request.IsCollapsedByDefault;
        selectedSection.BackgroundColor = request.BackgroundColor;
        selectedSection.BorderColor = request.BorderColor;
        selectedSection.HeaderBackgroundColor = request.HeaderBackgroundColor;
        selectedSection.HeaderTextColor = request.HeaderTextColor;
        selectedSection.BorderWidth = request.BorderWidth;
        selectedSection.BorderRadius = request.BorderRadius;
        selectedSection.Padding = request.Padding;

        MarkUnsaved();
    }

    #endregion

    #region Page Size

    // Gets the combined page size and orientation options for toolbar dropdown
    private List<(string Key, string Display, decimal WidthMm, decimal HeightMm, string Orientation)> GetPageSizeOptions()
    {
        var options = new List<(string Key, string Display, decimal WidthMm, decimal HeightMm, string Orientation)>();

        foreach (var preset in pageSizePresets)
        {
            // Portrait option
            options.Add((
                $"{preset.Name}-Portrait",
                $"{preset.Name} Portrait ({preset.WidthMm}×{preset.HeightMm}mm)",
                preset.WidthMm,
                preset.HeightMm,
                "Portrait"
            ));

            // Landscape option (swapped dimensions)
            options.Add((
                $"{preset.Name}-Landscape",
                $"{preset.Name} Landscape ({preset.HeightMm}×{preset.WidthMm}mm)",
                preset.HeightMm,
                preset.WidthMm,
                "Landscape"
            ));
        }

        return options;
    }

    private string GetCurrentPageSizeKey()
    {
        if (template == null) return "A4-Portrait";

        var preset = pageSizePresets.FirstOrDefault(p =>
            (template.PageOrientation == "Portrait" && p.WidthMm == template.PageWidthMm && p.HeightMm == template.PageHeightMm) ||
            (template.PageOrientation == "Landscape" && p.HeightMm == template.PageWidthMm && p.WidthMm == template.PageHeightMm));

        return $"{preset?.Name ?? "A4"}-{template.PageOrientation}";
    }

    private async Task ApplyPageSizeFromKeyAsync(string key)
    {
        Logger.LogInformation("ApplyPageSizeFromKeyAsync called with key: {Key}", key);

        if (template == null)
        {
            Logger.LogWarning("ApplyPageSizeFromKeyAsync: template is null");
            return;
        }

        if (!CanChangePageSize)
        {
            Logger.LogWarning("ApplyPageSizeFromKeyAsync: Cannot change page size - form has content. HasContent={HasContent}", HasContent);
            return;
        }

        // Split on last hyphen to support preset names with hyphens
        var lastHyphenIndex = key.LastIndexOf('-');
        if (lastHyphenIndex <= 0)
        {
            Logger.LogWarning("ApplyPageSizeFromKeyAsync: Invalid key format - no hyphen found");
            return;
        }

        var presetName = key.Substring(0, lastHyphenIndex);
        var orientation = key.Substring(lastHyphenIndex + 1);
        Logger.LogInformation("ApplyPageSizeFromKeyAsync: presetName={PresetName}, orientation={Orientation}", presetName, orientation);

        var preset = pageSizePresets.FirstOrDefault(p => p.Name == presetName);
        if (preset == null)
        {
            Logger.LogWarning("ApplyPageSizeFromKeyAsync: Preset '{PresetName}' not found. Available presets: {Presets}",
                presetName, string.Join(", ", pageSizePresets.Select(p => p.Name)));
            return;
        }

        // Apply orientation - swap width/height for landscape
        var oldWidth = template.PageWidthMm;
        var oldHeight = template.PageHeightMm;
        var oldOrientation = template.PageOrientation;

        if (orientation == "Landscape")
        {
            template.PageWidthMm = preset.HeightMm;
            template.PageHeightMm = preset.WidthMm;
        }
        else
        {
            template.PageWidthMm = preset.WidthMm;
            template.PageHeightMm = preset.HeightMm;
        }
        template.PageOrientation = orientation;

        Logger.LogInformation("ApplyPageSizeFromKeyAsync: Changed from {OldW}x{OldH} {OldO} to {NewW}x{NewH} {NewO}",
            oldWidth, oldHeight, oldOrientation,
            template.PageWidthMm, template.PageHeightMm, template.PageOrientation);

        hasUnsavedChanges = true;
        await InvokeAsync(StateHasChanged);

        Logger.LogInformation("ApplyPageSizeFromKeyAsync: StateHasChanged called");
    }

    private string GetCurrentPageSizeDisplay()
    {
        if (template == null) return "";

        var preset = pageSizePresets.FirstOrDefault(p =>
            (template.PageOrientation == "Portrait" && p.WidthMm == template.PageWidthMm && p.HeightMm == template.PageHeightMm) ||
            (template.PageOrientation == "Landscape" && p.HeightMm == template.PageWidthMm && p.WidthMm == template.PageHeightMm));

        return $"{preset?.Name ?? "Custom"} ({template.PageOrientation})";
    }

    private decimal GetCurrentPageWidthMm()
    {
        return template?.PageWidthMm ?? 210;
    }

    // Returns the inline style for the form paper element
    private string GetPaperStyle()
    {
        var widthPx = (int)((template?.PageWidthMm ?? 210) * 3);
        var heightPx = (int)((template?.PageHeightMm ?? 297) * 3);
        Logger.LogInformation("GetPaperStyle: width={Width}px, height={Height}px", widthPx, heightPx);
        return $"width: {widthPx}px; min-height: {heightPx}px;";
    }

    #endregion

    #region Page Margins

    private void ToggleMarginControls()
    {
        showMarginControls = !showMarginControls;
    }

    private void UpdateMargin(string side, ChangeEventArgs e)
    {
        if (template == null) return;

        if (decimal.TryParse(e.Value?.ToString(), out var value))
        {
            // Clamp value between 0 and 100mm
            value = Math.Max(0, Math.Min(100, value));

            switch (side.ToLower())
            {
                case "top":
                    template.MarginTopMm = value;
                    break;
                case "right":
                    template.MarginRightMm = value;
                    break;
                case "bottom":
                    template.MarginBottomMm = value;
                    break;
                case "left":
                    template.MarginLeftMm = value;
                    break;
            }

            MarkUnsaved();
        }
    }

    private string GetHorizontalRulerStyle()
    {
        var widthPx = (int)((template?.PageWidthMm ?? 210) * 3);
        return $"width: {widthPx}px;";
    }

    private string GetVerticalRulerStyle()
    {
        var heightPx = (int)((template?.PageHeightMm ?? 297) * 3);
        return $"height: {heightPx}px;";
    }

    private string GetMarginGuideStyle()
    {
        var topPx = (int)((template?.MarginTopMm ?? 20) * 3);
        var rightPx = (int)((template?.MarginRightMm ?? 15) * 3);
        var bottomPx = (int)((template?.MarginBottomMm ?? 20) * 3);
        var leftPx = (int)((template?.MarginLeftMm ?? 15) * 3);

        return $"top: {topPx}px; right: {rightPx}px; bottom: {bottomPx}px; left: {leftPx}px;";
    }

    #endregion

    #region Zoom

    private void ZoomIn()
    {
        if (zoomLevel < ZoomMax)
        {
            zoomLevel = Math.Round(Math.Min(ZoomMax, zoomLevel + ZoomStep), 1);
            StateHasChanged();
        }
    }

    private void ZoomOut()
    {
        if (zoomLevel > ZoomMin)
        {
            zoomLevel = Math.Round(Math.Max(ZoomMin, zoomLevel - ZoomStep), 1);
            StateHasChanged();
        }
    }

    private void ResetZoom()
    {
        zoomLevel = 1.0;
        StateHasChanged();
    }

    private string GetZoomStyle()
    {
        return $"transform: scale({zoomLevel}); transform-origin: top center;";
    }

    #endregion

    #region Section Helpers

    private string GetSectionStyles(FormTemplateSectionDto section)
    {
        var styles = new List<string>();

        if (!string.IsNullOrWhiteSpace(section.BackgroundColor))
            styles.Add($"background-color: {section.BackgroundColor}");

        if (!string.IsNullOrWhiteSpace(section.BorderColor) && (section.BorderWidth ?? 0) > 0)
        {
            styles.Add($"border: {section.BorderWidth}px solid {section.BorderColor}");
        }

        if ((section.BorderRadius ?? 0) > 0)
            styles.Add($"border-radius: {section.BorderRadius}px");

        if ((section.Padding ?? 0) > 0)
            styles.Add($"padding: {section.Padding}px");

        return string.Join("; ", styles);
    }

    private string GetLayoutDisplayName(string layoutType)
    {
        return layoutType switch
        {
            "1-col" => "Single",
            "2-col-equal" => "2 Equal",
            "2-col-left" => "Wide Left",
            "2-col-right" => "Wide Right",
            "3-col-equal" => "3 Equal",
            _ => layoutType
        };
    }

    #endregion

    #region Helpers

    private void MarkUnsaved()
    {
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private string GetFieldWidthClass(FormTemplateFieldDto field)
    {
        return field.Width switch
        {
            "half" => "width-half",
            "third" => "width-third",
            _ => "width-full"
        };
    }

    private string GetFieldIcon(FormFieldDataType dataType)
    {
        return dataType switch
        {
            FormFieldDataType.Text => "fas fa-font",
            FormFieldDataType.MultilineText => "fas fa-align-left",
            FormFieldDataType.Number => "fas fa-hashtag",
            FormFieldDataType.Currency => "fas fa-dollar-sign",
            FormFieldDataType.Percentage => "fas fa-percent",
            FormFieldDataType.Date => "fas fa-calendar",
            FormFieldDataType.DateTime => "fas fa-clock",
            FormFieldDataType.Checkbox => "fas fa-check-square",
            FormFieldDataType.Choice => "fas fa-list",
            FormFieldDataType.MultiChoice => "fas fa-tasks",
            FormFieldDataType.Signature => "fas fa-signature",
            FormFieldDataType.Photo => "fas fa-camera",
            FormFieldDataType.Location => "fas fa-map-marker-alt",
            FormFieldDataType.PassFail => "fas fa-thumbs-up",
            FormFieldDataType.Computed => "fas fa-calculator",
            FormFieldDataType.WorksheetLink => "fas fa-table",
            // Header/Footer special fields
            FormFieldDataType.PageNumber => "fas fa-file-alt",
            FormFieldDataType.CompanyLogo => "fas fa-building",
            FormFieldDataType.DocumentTitle => "fas fa-heading",
            FormFieldDataType.CurrentDate => "fas fa-calendar-day",
            FormFieldDataType.FormNumber => "fas fa-hashtag",
            _ => "fas fa-question"
        };
    }

    private RenderFragment RenderFieldPreview(FormTemplateFieldDto field) => builder =>
    {
        switch (field.DataType)
        {
            case FormFieldDataType.Text:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "text");
                builder.AddAttribute(2, "class", "form-control form-control-sm");
                builder.AddAttribute(3, "placeholder", field.Placeholder ?? "Enter text...");
                builder.AddAttribute(4, "disabled", true);
                builder.CloseElement();
                break;

            case FormFieldDataType.MultilineText:
                builder.OpenElement(0, "textarea");
                builder.AddAttribute(1, "class", "form-control form-control-sm");
                builder.AddAttribute(2, "placeholder", field.Placeholder ?? "Enter text...");
                builder.AddAttribute(3, "rows", "2");
                builder.AddAttribute(4, "disabled", true);
                builder.CloseElement();
                break;

            case FormFieldDataType.Number:
            case FormFieldDataType.Currency:
            case FormFieldDataType.Percentage:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "number");
                builder.AddAttribute(2, "class", "form-control form-control-sm");
                builder.AddAttribute(3, "placeholder", field.DataType == FormFieldDataType.Currency ? "$0.00" : "0");
                builder.AddAttribute(4, "disabled", true);
                builder.CloseElement();
                break;

            case FormFieldDataType.Date:
            case FormFieldDataType.DateTime:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", field.DataType == FormFieldDataType.Date ? "date" : "datetime-local");
                builder.AddAttribute(2, "class", "form-control form-control-sm");
                builder.AddAttribute(3, "disabled", true);
                builder.CloseElement();
                break;

            case FormFieldDataType.Checkbox:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "form-check");
                builder.OpenElement(2, "input");
                builder.AddAttribute(3, "type", "checkbox");
                builder.AddAttribute(4, "class", "form-check-input");
                builder.AddAttribute(5, "disabled", true);
                builder.CloseElement();
                builder.OpenElement(6, "label");
                builder.AddAttribute(7, "class", "form-check-label");
                builder.AddContent(8, field.DisplayName);
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Choice:
                builder.OpenElement(0, "select");
                builder.AddAttribute(1, "class", "form-select form-select-sm");
                builder.AddAttribute(2, "disabled", true);
                builder.OpenElement(3, "option");
                builder.AddContent(4, "Select...");
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.MultiChoice:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "multi-choice-preview");
                builder.AddContent(2, "[ ] Option 1  [ ] Option 2  [ ] Option 3");
                builder.CloseElement();
                break;

            case FormFieldDataType.Signature:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "signature-preview");
                builder.AddAttribute(2, "style", "border: 1px dashed #ccc; padding: 20px; text-align: center; color: #999;");
                builder.AddContent(3, "Signature area");
                builder.CloseElement();
                break;

            case FormFieldDataType.Photo:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "photo-preview");
                builder.AddAttribute(2, "style", "border: 1px dashed #ccc; padding: 20px; text-align: center; color: #999;");
                builder.OpenElement(3, "i");
                builder.AddAttribute(4, "class", "fas fa-camera fa-2x mb-2");
                builder.CloseElement();
                builder.OpenElement(5, "div");
                builder.AddContent(6, $"Max {field.MaxPhotos ?? 5} photos");
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Location:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "location-preview");
                builder.AddAttribute(2, "style", "border: 1px dashed #ccc; padding: 10px; text-align: center; color: #999;");
                builder.OpenElement(3, "i");
                builder.AddAttribute(4, "class", "fas fa-map-marker-alt me-2");
                builder.CloseElement();
                builder.AddContent(5, "GPS Location");
                builder.CloseElement();
                break;

            case FormFieldDataType.PassFail:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "d-flex gap-2");
                builder.OpenElement(2, "button");
                builder.AddAttribute(3, "class", "btn btn-outline-success btn-sm");
                builder.AddAttribute(4, "disabled", true);
                builder.OpenElement(5, "i");
                builder.AddAttribute(6, "class", "fas fa-check me-1");
                builder.CloseElement();
                builder.AddContent(7, "Pass");
                builder.CloseElement();
                builder.OpenElement(8, "button");
                builder.AddAttribute(9, "class", "btn btn-outline-danger btn-sm");
                builder.AddAttribute(10, "disabled", true);
                builder.OpenElement(11, "i");
                builder.AddAttribute(12, "class", "fas fa-times me-1");
                builder.CloseElement();
                builder.AddContent(13, "Fail");
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Computed:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "computed-preview");
                builder.AddAttribute(2, "style", "background: #f8f9fa; padding: 8px; border-radius: 4px; font-family: monospace;");
                builder.AddContent(3, field.Formula ?? "= formula");
                builder.CloseElement();
                break;

            case FormFieldDataType.WorksheetLink:
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "btn btn-outline-secondary btn-sm");
                builder.AddAttribute(2, "disabled", true);
                builder.OpenElement(3, "i");
                builder.AddAttribute(4, "class", "fas fa-table me-1");
                builder.CloseElement();
                builder.AddContent(5, "Open Worksheet");
                builder.CloseElement();
                break;

            // Header/Footer special fields
            case FormFieldDataType.CompanyLogo:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "logo-preview");
                builder.AddAttribute(2, "style", "border: 1px dashed #8b5cf6; padding: 20px; text-align: center; color: #8b5cf6; background: rgba(139, 92, 246, 0.05); border-radius: 4px;");
                builder.OpenElement(3, "i");
                builder.AddAttribute(4, "class", "fas fa-building fa-2x mb-2");
                builder.AddAttribute(5, "style", "display: block;");
                builder.CloseElement();
                builder.OpenElement(6, "div");
                builder.AddAttribute(7, "style", "font-size: 12px;");
                builder.AddContent(8, "Company Logo");
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.PageNumber:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "page-number-preview");
                builder.AddAttribute(2, "style", "text-align: center; color: #6b7280; font-size: 0.875rem; padding: 8px; background: #f9fafb; border-radius: 4px;");
                builder.AddContent(3, "Page 1 of 1");
                builder.CloseElement();
                break;

            case FormFieldDataType.DocumentTitle:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "document-title-preview");
                builder.AddAttribute(2, "style", "font-weight: 600; font-size: 1.125rem; text-align: center; padding: 8px;");
                builder.AddContent(3, template?.Name ?? "Document Title");
                builder.CloseElement();
                break;

            case FormFieldDataType.CurrentDate:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "current-date-preview");
                builder.AddAttribute(2, "style", "color: #6b7280; padding: 8px; background: #f9fafb; border-radius: 4px;");
                builder.AddContent(3, DateTime.Now.ToString("dd MMM yyyy"));
                builder.CloseElement();
                break;

            case FormFieldDataType.FormNumber:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "form-number-preview");
                builder.AddAttribute(2, "style", "font-family: monospace; color: #6b7280; padding: 8px; background: #f9fafb; border-radius: 4px;");
                builder.AddContent(3, $"{template?.NumberPrefix ?? "FORM"}-001");
                builder.CloseElement();
                break;

            default:
                builder.OpenElement(0, "span");
                builder.AddContent(1, "Field preview");
                builder.CloseElement();
                break;
        }
    };

    private string GetModulePath()
    {
        return currentModule switch
        {
            FormModuleContext.Estimate => "estimate",
            FormModuleContext.FabMate => "fabmate",
            FormModuleContext.QDocs => "qdocs",
            FormModuleContext.Assets => "assets",
            _ => "estimate"
        };
    }

    #endregion

    #region Save/Navigation

    private async Task SaveTemplate()
    {
        if (template == null) return;

        try
        {
            // Build update request with sections
            var updateRequest = new UpdateFormTemplateRequest
            {
                Name = template.Name,
                Description = template.Description,
                ModuleContext = template.ModuleContext,
                FormType = template.FormType,
                IsCompanyDefault = template.IsCompanyDefault,
                IsPublished = template.IsPublished,
                NumberPrefix = template.NumberPrefix,
                ShowSectionHeaders = template.ShowSectionHeaders,
                AllowNotes = template.AllowNotes,
                // Page size
                PageWidthMm = template.PageWidthMm,
                PageHeightMm = template.PageHeightMm,
                PageOrientation = template.PageOrientation,
                // Page margins
                MarginTopMm = template.MarginTopMm,
                MarginRightMm = template.MarginRightMm,
                MarginBottomMm = template.MarginBottomMm,
                MarginLeftMm = template.MarginLeftMm,
                // Sections with their fields
                Sections = sections.Select(s => new UpdateFormTemplateSectionRequest
                {
                    Id = s.Id > 0 ? s.Id : null, // New sections have negative IDs
                    Name = s.Name,
                    LayoutType = s.LayoutType,
                    DisplayOrder = s.DisplayOrder,
                    PageBreakBefore = s.PageBreakBefore,
                    KeepTogether = s.KeepTogether,
                    BackgroundColor = s.BackgroundColor,
                    BorderColor = s.BorderColor,
                    HeaderBackgroundColor = s.HeaderBackgroundColor,
                    HeaderTextColor = s.HeaderTextColor,
                    BorderWidth = s.BorderWidth,
                    BorderRadius = s.BorderRadius,
                    Padding = s.Padding,
                    IsCollapsible = s.IsCollapsible,
                    Fields = s.Fields.Select(f => new UpdateFormTemplateFieldRequest
                    {
                        Id = f.Id > 0 ? f.Id : null, // New fields have negative IDs
                        FieldKey = f.FieldKey,
                        DisplayName = f.DisplayName,
                        DataType = f.DataType,
                        DisplayOrder = f.DisplayOrder,
                        ColumnIndex = f.ColumnIndex,
                        RowIndex = f.RowIndex,
                        IsRequired = f.IsRequired,
                        IsVisible = f.IsVisible,
                        IsReadOnly = f.IsReadOnly,
                        DefaultValue = f.DefaultValue,
                        Placeholder = f.Placeholder,
                        HelpText = f.HelpText,
                        ValidationRegex = f.ValidationRegex,
                        ValidationMessage = f.ValidationMessage,
                        SelectOptions = f.SelectOptions,
                        Formula = f.Formula,
                        MinValue = f.MinValue,
                        MaxValue = f.MaxValue,
                        DecimalPlaces = f.DecimalPlaces,
                        CurrencySymbol = f.CurrencySymbol,
                        LinkedWorksheetTemplateId = f.LinkedWorksheetTemplateId,
                        MaxPhotos = f.MaxPhotos,
                        RequirePhotoLocation = f.RequirePhotoLocation
                    }).ToList()
                }).ToList()
            };

            template = await TemplateService.UpdateTemplateAsync(Id, updateRequest, currentCompanyId, currentUserId);

            if (template != null)
            {
                sections = template.Sections.OrderBy(s => s.DisplayOrder).ToList();
                hasUnsavedChanges = false;
            }

            Logger.LogInformation("Saved form template {TemplateId} for company {CompanyId}", Id, currentCompanyId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving template {TemplateId}", Id);
            errorMessage = "Error saving template. Please try again.";
        }
    }

    private async Task PublishTemplate()
    {
        if (template == null) return;

        try
        {
            // Save first if there are unsaved changes
            if (hasUnsavedChanges)
            {
                await SaveTemplate();
            }

            template = await TemplateService.PublishTemplateAsync(Id, currentCompanyId, currentUserId);

            Logger.LogInformation("Published form template {TemplateId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error publishing template {TemplateId}", Id);
            errorMessage = "Error publishing template. Please try again.";
        }
    }

    private async Task UnpublishTemplate()
    {
        if (template == null) return;

        try
        {
            template = await TemplateService.UnpublishTemplateAsync(Id, currentCompanyId, currentUserId);

            Logger.LogInformation("Unpublished form template {TemplateId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error unpublishing template {TemplateId}", Id);
            errorMessage = "Error unpublishing template. Please try again.";
        }
    }

    private void GoBackToLibrary()
    {
        Navigation.NavigateTo($"/{TenantSlug}/{GetModulePath()}/forms");
    }

    private void PreviewTemplate()
    {
        // Navigate to preview mode - can be implemented as a modal or separate page
        Navigation.NavigateTo($"/{TenantSlug}/{GetModulePath()}/forms/templates/{Id}/preview");
    }

    #endregion

    #region IToolbarActionProvider Implementation

    public ToolbarActionGroup GetActions()
    {
        var isSystemTemplate = template?.IsSystemTemplate ?? false;
        var isPublished = template?.IsPublished ?? false;

        return new ToolbarActionGroup
        {
            PrimaryActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "Back",
                    Text = "Back to Library",
                    Icon = "fas fa-arrow-left",
                    ActionFunc = async () =>
                    {
                        GoBackToLibrary();
                        await Task.CompletedTask;
                    },
                    Style = ToolbarActionStyle.Secondary
                },
                new()
                {
                    Label = "Save",
                    Text = "Save",
                    Icon = "fas fa-save",
                    ActionFunc = SaveTemplate,
                    IsDisabled = !hasUnsavedChanges || isSystemTemplate,
                    Style = ToolbarActionStyle.Primary
                }
            },
            MenuActions = BuildMenuActions(isSystemTemplate, isPublished)
        };
    }

    private List<ToolbarAction> BuildMenuActions(bool isSystemTemplate, bool isPublished)
    {
        var actions = new List<ToolbarAction>
        {
            new()
            {
                Label = isPublished ? "Unpublish" : "Publish",
                Text = isPublished ? "Unpublish Template" : "Publish Template",
                Icon = isPublished ? "fas fa-eye-slash" : "fas fa-eye",
                ActionFunc = isPublished ? UnpublishTemplate : PublishTemplate,
                IsDisabled = isSystemTemplate
            },
            new()
            {
                Label = "Preview",
                Text = "Preview Form",
                Icon = "fas fa-play",
                ActionFunc = async () =>
                {
                    PreviewTemplate();
                    await Task.CompletedTask;
                }
            }
        };

        // Add page size options
        var pageSizeOptions = GetPageSizeOptions();
        var currentKey = GetCurrentPageSizeKey();

        foreach (var option in pageSizeOptions)
        {
            var isCurrentSize = option.Key == currentKey;
            var optionKey = option.Key; // Capture for closure

            actions.Add(new ToolbarAction
            {
                Label = option.Key,
                Text = $"{(isCurrentSize ? "✓ " : "")}{option.Display}",
                Icon = "fas fa-file-alt",
                ActionFunc = async () => await ApplyPageSizeFromKeyAsync(optionKey),
                IsDisabled = isSystemTemplate || !CanChangePageSize
            });
        }

        return actions;
    }

    #endregion
}
