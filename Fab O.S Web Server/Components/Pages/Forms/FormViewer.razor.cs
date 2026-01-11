using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Models.Entities.Forms;
using FabOS.WebServer.Models.DTOs.Forms;
using FabOS.WebServer.Services.Interfaces.Forms;
using System.Security.Claims;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Forms;

public partial class FormViewer : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IFormInstanceService InstanceService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<FormViewer> Logger { get; set; } = default!;

    // State
    private bool isLoading = true;
    private bool isSaving = false;
    private bool hasUnsavedChanges = false;
    private string? errorMessage;
    private string? successMessage;

    // Data
    private FormInstanceDto? formInstance;
    private Dictionary<string, FormInstanceValueDto> fieldValues = new();

    // Dialog state
    private bool showRejectDialog = false;
    private string rejectionReason = "";
    private bool showSignatureModal = false;
    private string? signatureFieldKey;

    // Authentication
    private int currentUserId = 0;
    private int currentCompanyId = 0;
    private FormModuleContext currentModule = FormModuleContext.Estimate;

    // Computed properties
    private bool IsReadOnly => formInstance?.Status == FormInstanceStatus.Approved ||
                               formInstance?.Status == FormInstanceStatus.Submitted ||
                               formInstance?.Status == FormInstanceStatus.UnderReview;

    private bool CanSubmit => formInstance?.Status == FormInstanceStatus.Draft ||
                              formInstance?.Status == FormInstanceStatus.Rejected;

    private bool CanApprove => formInstance?.Status == FormInstanceStatus.Submitted ||
                               formInstance?.Status == FormInstanceStatus.UnderReview;

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

        await LoadFormInstance();
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

    private async Task LoadFormInstance()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            formInstance = await InstanceService.GetInstanceByIdAsync(Id, currentCompanyId);

            if (formInstance != null)
            {
                // Build field values dictionary for quick lookup
                fieldValues = formInstance.Values.ToDictionary(v => v.FieldKey, v => v);
            }
            else
            {
                errorMessage = "Form not found.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading form instance {InstanceId}", Id);
            errorMessage = "Error loading form. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    #region Field Rendering

    private List<string> GetSections()
    {
        if (formInstance?.Template?.Fields == null)
            return new List<string>();

        return formInstance.Template.Fields
            .Select(f => f.SectionName ?? "")
            .Distinct()
            .OrderBy(s => formInstance.Template.Fields.FirstOrDefault(f => f.SectionName == s)?.SectionOrder ?? 0)
            .ToList();
    }

    private IEnumerable<FormTemplateFieldDto> GetFieldsInSection(string sectionName)
    {
        if (formInstance?.Template?.Fields == null)
            return Enumerable.Empty<FormTemplateFieldDto>();

        return formInstance.Template.Fields
            .Where(f => (f.SectionName ?? "") == sectionName && f.IsVisible)
            .OrderBy(f => f.DisplayOrder);
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

    private FormInstanceValueDto GetOrCreateValue(FormTemplateFieldDto field)
    {
        if (fieldValues.TryGetValue(field.FieldKey, out var value))
            return value;

        // Create a new value entry
        value = new FormInstanceValueDto
        {
            FormInstanceId = Id,
            FormTemplateFieldId = field.Id,
            FieldKey = field.FieldKey,
            DataType = field.DataType,
            DisplayName = field.DisplayName
        };
        fieldValues[field.FieldKey] = value;
        return value;
    }

    private RenderFragment RenderField(FormTemplateFieldDto field) => builder =>
    {
        var value = GetOrCreateValue(field);
        var isDisabled = IsReadOnly || field.IsReadOnly;

        switch (field.DataType)
        {
            case FormFieldDataType.Text:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "text");
                builder.AddAttribute(2, "class", "form-control");
                builder.AddAttribute(3, "placeholder", field.Placeholder ?? "");
                builder.AddAttribute(4, "value", value.TextValue ?? "");
                builder.AddAttribute(5, "disabled", isDisabled);
                builder.AddAttribute(6, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    value.TextValue = e.Value?.ToString();
                    MarkUnsaved();
                }));
                builder.CloseElement();
                break;

            case FormFieldDataType.MultilineText:
                builder.OpenElement(0, "textarea");
                builder.AddAttribute(1, "class", "form-control");
                builder.AddAttribute(2, "rows", "3");
                builder.AddAttribute(3, "placeholder", field.Placeholder ?? "");
                builder.AddAttribute(4, "disabled", isDisabled);
                builder.AddAttribute(5, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    value.TextValue = e.Value?.ToString();
                    MarkUnsaved();
                }));
                builder.AddContent(6, value.TextValue ?? "");
                builder.CloseElement();
                break;

            case FormFieldDataType.Number:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "number");
                builder.AddAttribute(2, "class", "form-control");
                builder.AddAttribute(3, "step", field.DecimalPlaces.HasValue ? Math.Pow(0.1, field.DecimalPlaces.Value).ToString() : "any");
                builder.AddAttribute(4, "value", value.NumberValue?.ToString() ?? "");
                builder.AddAttribute(5, "disabled", isDisabled);
                builder.AddAttribute(6, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    if (decimal.TryParse(e.Value?.ToString(), out var num))
                        value.NumberValue = num;
                    else
                        value.NumberValue = null;
                    MarkUnsaved();
                }));
                builder.CloseElement();
                break;

            case FormFieldDataType.Currency:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "input-group");
                builder.OpenElement(2, "span");
                builder.AddAttribute(3, "class", "input-group-text");
                builder.AddContent(4, field.CurrencySymbol ?? "$");
                builder.CloseElement();
                builder.OpenElement(5, "input");
                builder.AddAttribute(6, "type", "number");
                builder.AddAttribute(7, "class", "form-control");
                builder.AddAttribute(8, "step", "0.01");
                builder.AddAttribute(9, "value", value.NumberValue?.ToString("F2") ?? "");
                builder.AddAttribute(10, "disabled", isDisabled);
                builder.AddAttribute(11, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    if (decimal.TryParse(e.Value?.ToString(), out var num))
                        value.NumberValue = num;
                    else
                        value.NumberValue = null;
                    MarkUnsaved();
                }));
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Percentage:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "input-group");
                builder.OpenElement(2, "input");
                builder.AddAttribute(3, "type", "number");
                builder.AddAttribute(4, "class", "form-control");
                builder.AddAttribute(5, "step", "0.1");
                builder.AddAttribute(6, "value", value.NumberValue?.ToString() ?? "");
                builder.AddAttribute(7, "disabled", isDisabled);
                builder.AddAttribute(8, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    if (decimal.TryParse(e.Value?.ToString(), out var num))
                        value.NumberValue = num;
                    else
                        value.NumberValue = null;
                    MarkUnsaved();
                }));
                builder.CloseElement();
                builder.OpenElement(9, "span");
                builder.AddAttribute(10, "class", "input-group-text");
                builder.AddContent(11, "%");
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Date:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "date");
                builder.AddAttribute(2, "class", "form-control");
                builder.AddAttribute(3, "value", value.DateValue?.ToString("yyyy-MM-dd") ?? "");
                builder.AddAttribute(4, "disabled", isDisabled);
                builder.AddAttribute(5, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    if (DateTime.TryParse(e.Value?.ToString(), out var date))
                        value.DateValue = date;
                    else
                        value.DateValue = null;
                    MarkUnsaved();
                }));
                builder.CloseElement();
                break;

            case FormFieldDataType.DateTime:
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "datetime-local");
                builder.AddAttribute(2, "class", "form-control");
                builder.AddAttribute(3, "value", value.DateValue?.ToString("yyyy-MM-ddTHH:mm") ?? "");
                builder.AddAttribute(4, "disabled", isDisabled);
                builder.AddAttribute(5, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    if (DateTime.TryParse(e.Value?.ToString(), out var date))
                        value.DateValue = date;
                    else
                        value.DateValue = null;
                    MarkUnsaved();
                }));
                builder.CloseElement();
                break;

            case FormFieldDataType.Checkbox:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "form-check");
                builder.OpenElement(2, "input");
                builder.AddAttribute(3, "type", "checkbox");
                builder.AddAttribute(4, "class", "form-check-input");
                builder.AddAttribute(5, "checked", value.BoolValue ?? false);
                builder.AddAttribute(6, "disabled", isDisabled);
                builder.AddAttribute(7, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    value.BoolValue = (bool)(e.Value ?? false);
                    MarkUnsaved();
                }));
                builder.CloseElement();
                builder.OpenElement(8, "label");
                builder.AddAttribute(9, "class", "form-check-label");
                builder.AddContent(10, field.DisplayName);
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Choice:
                builder.OpenElement(0, "select");
                builder.AddAttribute(1, "class", "form-select");
                builder.AddAttribute(2, "disabled", isDisabled);
                builder.AddAttribute(3, "value", value.TextValue ?? "");
                builder.AddAttribute(4, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                    value.TextValue = e.Value?.ToString();
                    MarkUnsaved();
                }));
                builder.OpenElement(5, "option");
                builder.AddAttribute(6, "value", "");
                builder.AddContent(7, "Select...");
                builder.CloseElement();
                if (field.SelectOptions != null)
                {
                    var seq = 8;
                    foreach (var option in field.SelectOptions)
                    {
                        builder.OpenElement(seq++, "option");
                        builder.AddAttribute(seq++, "value", option);
                        builder.AddContent(seq++, option);
                        builder.CloseElement();
                    }
                }
                builder.CloseElement();
                break;

            case FormFieldDataType.MultiChoice:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "multi-choice-container");
                if (field.SelectOptions != null)
                {
                    var selectedValues = ParseMultiChoice(value.TextValue);
                    var seq = 2;
                    foreach (var option in field.SelectOptions)
                    {
                        var localOption = option;
                        builder.OpenElement(seq++, "div");
                        builder.AddAttribute(seq++, "class", "form-check");
                        builder.OpenElement(seq++, "input");
                        builder.AddAttribute(seq++, "type", "checkbox");
                        builder.AddAttribute(seq++, "class", "form-check-input");
                        builder.AddAttribute(seq++, "checked", selectedValues.Contains(option));
                        builder.AddAttribute(seq++, "disabled", isDisabled);
                        builder.AddAttribute(seq++, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => {
                            ToggleMultiChoice(value, localOption, (bool)(e.Value ?? false));
                        }));
                        builder.CloseElement();
                        builder.OpenElement(seq++, "label");
                        builder.AddAttribute(seq++, "class", "form-check-label");
                        builder.AddContent(seq++, option);
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                }
                builder.CloseElement();
                break;

            case FormFieldDataType.PassFail:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "pass-fail-buttons");
                builder.OpenElement(2, "button");
                builder.AddAttribute(3, "type", "button");
                builder.AddAttribute(4, "class", $"btn btn-outline-success {(value.PassFailValue == true ? "active" : "")}");
                builder.AddAttribute(5, "disabled", isDisabled);
                builder.AddAttribute(6, "onclick", EventCallback.Factory.Create(this, () => {
                    value.PassFailValue = true;
                    MarkUnsaved();
                }));
                builder.OpenElement(7, "i");
                builder.AddAttribute(8, "class", "fas fa-check me-1");
                builder.CloseElement();
                builder.AddContent(9, "Pass");
                builder.CloseElement();
                builder.OpenElement(10, "button");
                builder.AddAttribute(11, "type", "button");
                builder.AddAttribute(12, "class", $"btn btn-outline-danger {(value.PassFailValue == false ? "active" : "")}");
                builder.AddAttribute(13, "disabled", isDisabled);
                builder.AddAttribute(14, "onclick", EventCallback.Factory.Create(this, () => {
                    value.PassFailValue = false;
                    MarkUnsaved();
                }));
                builder.OpenElement(15, "i");
                builder.AddAttribute(16, "class", "fas fa-times me-1");
                builder.CloseElement();
                builder.AddContent(17, "Fail");
                builder.CloseElement();
                builder.CloseElement();
                break;

            case FormFieldDataType.Signature:
                var hasSignature = !string.IsNullOrEmpty(value.SignatureDataUrl);
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", $"signature-area {(hasSignature ? "has-signature" : "")}");
                if (!isDisabled)
                {
                    builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => OpenSignatureModal(field.FieldKey)));
                }
                if (hasSignature)
                {
                    builder.OpenElement(3, "img");
                    builder.AddAttribute(4, "src", value.SignatureDataUrl);
                    builder.AddAttribute(5, "alt", "Signature");
                    builder.CloseElement();
                }
                else
                {
                    builder.OpenElement(6, "span");
                    builder.AddAttribute(7, "class", "text-muted");
                    builder.OpenElement(8, "i");
                    builder.AddAttribute(9, "class", "fas fa-signature me-2");
                    builder.CloseElement();
                    builder.AddContent(10, isDisabled ? "No signature" : "Click to sign");
                    builder.CloseElement();
                }
                builder.CloseElement();
                break;

            case FormFieldDataType.Photo:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "photo-upload-area");
                // Display existing photos
                if (value.Attachments != null)
                {
                    var seq = 2;
                    foreach (var attachment in value.Attachments)
                    {
                        builder.OpenElement(seq++, "img");
                        builder.AddAttribute(seq++, "src", attachment.ThumbnailPath ?? attachment.StoragePath);
                        builder.AddAttribute(seq++, "class", "photo-thumbnail");
                        builder.AddAttribute(seq++, "alt", attachment.FileName);
                        builder.CloseElement();
                    }
                }
                // Add photo button
                if (!isDisabled)
                {
                    var photoCount = value.Attachments?.Count ?? 0;
                    var maxPhotos = field.MaxPhotos ?? 5;
                    if (photoCount < maxPhotos)
                    {
                        builder.OpenElement(100, "div");
                        builder.AddAttribute(101, "class", "photo-add-button");
                        builder.OpenElement(102, "i");
                        builder.AddAttribute(103, "class", "fas fa-camera");
                        builder.CloseElement();
                        builder.OpenElement(104, "small");
                        builder.AddContent(105, "Add Photo");
                        builder.CloseElement();
                        builder.CloseElement();
                    }
                }
                builder.CloseElement();
                break;

            case FormFieldDataType.Location:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "location-display");
                if (value.JsonValue != null)
                {
                    builder.OpenElement(2, "i");
                    builder.AddAttribute(3, "class", "fas fa-map-marker-alt text-success");
                    builder.CloseElement();
                    builder.OpenElement(4, "span");
                    builder.AddContent(5, value.JsonValue.ToString());
                    builder.CloseElement();
                }
                else
                {
                    builder.OpenElement(6, "i");
                    builder.AddAttribute(7, "class", "fas fa-map-marker-alt text-muted");
                    builder.CloseElement();
                    if (!isDisabled)
                    {
                        builder.OpenElement(8, "button");
                        builder.AddAttribute(9, "type", "button");
                        builder.AddAttribute(10, "class", "btn btn-link btn-sm");
                        builder.AddAttribute(11, "onclick", EventCallback.Factory.Create(this, () => CaptureLocation(field.FieldKey)));
                        builder.AddContent(12, "Capture Location");
                        builder.CloseElement();
                    }
                    else
                    {
                        builder.OpenElement(13, "span");
                        builder.AddAttribute(14, "class", "text-muted");
                        builder.AddContent(15, "No location");
                        builder.CloseElement();
                    }
                }
                builder.CloseElement();
                break;

            case FormFieldDataType.Computed:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "form-control-plaintext");
                builder.AddAttribute(2, "style", "background: #f8f9fa; padding: 0.5rem; border-radius: 4px;");
                builder.AddContent(3, value.NumberValue?.ToString(field.DecimalPlaces.HasValue ? $"N{field.DecimalPlaces}" : "N2") ?? "—");
                builder.CloseElement();
                break;

            case FormFieldDataType.WorksheetLink:
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "class", "btn btn-outline-secondary");
                builder.OpenElement(3, "i");
                builder.AddAttribute(4, "class", "fas fa-table me-1");
                builder.CloseElement();
                builder.AddContent(5, field.LinkedWorksheetTemplateName ?? "Open Worksheet");
                builder.CloseElement();
                break;

            default:
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "text-muted");
                builder.AddContent(2, "Unsupported field type");
                builder.CloseElement();
                break;
        }
    };

    private HashSet<string> ParseMultiChoice(string? jsonValue)
    {
        if (string.IsNullOrEmpty(jsonValue))
            return new HashSet<string>();

        try
        {
            var values = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonValue);
            return values?.ToHashSet() ?? new HashSet<string>();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private void ToggleMultiChoice(FormInstanceValueDto value, string option, bool selected)
    {
        var values = ParseMultiChoice(value.TextValue);

        if (selected)
            values.Add(option);
        else
            values.Remove(option);

        value.TextValue = System.Text.Json.JsonSerializer.Serialize(values.ToList());
        MarkUnsaved();
    }

    #endregion

    #region Signature

    private void OpenSignatureModal(string fieldKey)
    {
        signatureFieldKey = fieldKey;
        showSignatureModal = true;
    }

    private void CloseSignatureModal()
    {
        showSignatureModal = false;
        signatureFieldKey = null;
    }

    private async Task ClearSignature()
    {
        await JSRuntime.InvokeVoidAsync("signatureCanvas.clear");
    }

    private async Task SaveSignature()
    {
        if (signatureFieldKey == null) return;

        try
        {
            var dataUrl = await JSRuntime.InvokeAsync<string>("signatureCanvas.getDataUrl");

            if (fieldValues.TryGetValue(signatureFieldKey, out var value))
            {
                value.SignatureDataUrl = dataUrl;
                MarkUnsaved();
            }

            CloseSignatureModal();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving signature");
            errorMessage = "Error saving signature.";
        }
    }

    #endregion

    #region Location

    private async Task CaptureLocation(string fieldKey)
    {
        try
        {
            var location = await JSRuntime.InvokeAsync<LocationResult>("navigator.geolocation.getCurrentPosition",
                new object[] { });

            if (fieldValues.TryGetValue(fieldKey, out var value))
            {
                value.JsonValue = $"Lat: {location.Latitude:F6}, Lng: {location.Longitude:F6}";
                MarkUnsaved();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error capturing location");
            errorMessage = "Error capturing location. Please ensure location access is enabled.";
        }
    }

    private class LocationResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    #endregion

    #region Helpers

    private void MarkUnsaved()
    {
        hasUnsavedChanges = true;
        StateHasChanged();
    }

    private string GetStatusBadgeClass(FormInstanceStatus status)
    {
        return status switch
        {
            FormInstanceStatus.Draft => "bg-secondary",
            FormInstanceStatus.Submitted => "bg-info",
            FormInstanceStatus.UnderReview => "bg-warning text-dark",
            FormInstanceStatus.Approved => "bg-success",
            FormInstanceStatus.Rejected => "bg-danger",
            _ => "bg-secondary"
        };
    }

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

    #region Save/Workflow

    private async Task SaveForm()
    {
        if (formInstance == null) return;

        try
        {
            isSaving = true;
            errorMessage = null;

            var request = new UpdateFormInstanceRequest
            {
                Notes = formInstance.Notes,
                Values = fieldValues.Values.Select(v => new FormInstanceValueRequest
                {
                    FieldKey = v.FieldKey,
                    TextValue = v.TextValue,
                    NumberValue = v.NumberValue,
                    DateValue = v.DateValue,
                    BoolValue = v.BoolValue,
                    JsonValue = v.JsonValue,
                    SignatureDataUrl = v.SignatureDataUrl,
                    PassFailValue = v.PassFailValue,
                    PassFailComment = v.PassFailComment
                }).ToList()
            };

            formInstance = await InstanceService.UpdateInstanceAsync(Id, request, currentCompanyId, currentUserId);

            hasUnsavedChanges = false;
            successMessage = "Form saved successfully.";

            Logger.LogInformation("Saved form instance {InstanceId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving form instance {InstanceId}", Id);
            errorMessage = "Error saving form. Please try again.";
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task SubmitForm()
    {
        if (formInstance == null) return;

        try
        {
            isSaving = true;

            // Save first if there are unsaved changes
            if (hasUnsavedChanges)
            {
                await SaveForm();
            }

            formInstance = await InstanceService.SubmitForReviewAsync(Id, currentCompanyId, currentUserId);

            successMessage = "Form submitted for review.";
            Logger.LogInformation("Submitted form instance {InstanceId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error submitting form instance {InstanceId}", Id);
            errorMessage = "Error submitting form. Please try again.";
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task ApproveForm()
    {
        if (formInstance == null) return;

        try
        {
            isSaving = true;

            formInstance = await InstanceService.ApproveAsync(Id, currentCompanyId, currentUserId);

            successMessage = "Form approved.";
            Logger.LogInformation("Approved form instance {InstanceId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error approving form instance {InstanceId}", Id);
            errorMessage = "Error approving form. Please try again.";
        }
        finally
        {
            isSaving = false;
        }
    }

    private void ShowRejectDialog()
    {
        rejectionReason = "";
        showRejectDialog = true;
    }

    private async Task RejectForm()
    {
        if (formInstance == null || string.IsNullOrWhiteSpace(rejectionReason)) return;

        try
        {
            isSaving = true;

            formInstance = await InstanceService.RejectAsync(Id, rejectionReason, currentCompanyId, currentUserId);

            showRejectDialog = false;
            successMessage = "Form rejected.";
            Logger.LogInformation("Rejected form instance {InstanceId}", Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error rejecting form instance {InstanceId}", Id);
            errorMessage = "Error rejecting form. Please try again.";
        }
        finally
        {
            isSaving = false;
        }
    }

    private void GoBackToLibrary()
    {
        Navigation.NavigateTo($"/{TenantSlug}/{GetModulePath()}/forms");
    }

    #endregion

    #region IToolbarActionProvider Implementation

    public ToolbarActionGroup GetActions()
    {
        var isDraft = formInstance?.Status == FormInstanceStatus.Draft ||
                      formInstance?.Status == FormInstanceStatus.Rejected;

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
                    ActionFunc = SaveForm,
                    IsDisabled = !hasUnsavedChanges || IsReadOnly,
                    Style = ToolbarActionStyle.Primary
                }
            },
            MenuActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "Export PDF",
                    Text = "Export to PDF",
                    Icon = "fas fa-file-pdf",
                    ActionFunc = ExportToPdfAsync
                },
                new()
                {
                    Label = "Print",
                    Text = "Print Form",
                    Icon = "fas fa-print",
                    ActionFunc = PrintFormAsync
                }
            }
        };
    }

    #endregion

    #region PDF Export

    private bool isExporting = false;

    private async Task ExportToPdfAsync()
    {
        if (formInstance == null) return;

        try
        {
            isExporting = true;
            errorMessage = null;
            StateHasChanged();

            // Construct the API URL for HTML export
            var apiUrl = $"/api/{TenantSlug}/forms/instances/{Id}/export-html";

            // Call JavaScript to fetch HTML and convert to PDF using Nutrient
            var success = await JSRuntime.InvokeAsync<bool>("formPdfExport.exportToPdf", apiUrl, formInstance.FormNumber ?? $"Form-{Id}");

            if (success)
            {
                successMessage = "PDF exported successfully.";
            }
            else
            {
                errorMessage = "Error exporting PDF. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting form {FormId} to PDF", Id);
            errorMessage = "Error exporting PDF. Please try again.";
        }
        finally
        {
            isExporting = false;
            StateHasChanged();
        }
    }

    private async Task PrintFormAsync()
    {
        if (formInstance == null) return;

        try
        {
            // Construct the API URL for HTML export
            var apiUrl = $"/api/{TenantSlug}/forms/instances/{Id}/export-html";

            // Call JavaScript to open print dialog
            await JSRuntime.InvokeVoidAsync("formPdfExport.printForm", apiUrl);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error printing form {FormId}", Id);
            errorMessage = "Error printing form. Please try again.";
        }
    }

    #endregion
}
