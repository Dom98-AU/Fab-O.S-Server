using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace FabOS.WebServer.Components.Pages;

public partial class MeasurementDetail : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<MeasurementDetail> Logger { get; set; } = default!;
    [Inject] private ITakeoffCatalogueService CatalogueService { get; set; } = default!;
    [Inject] private ISurfaceCoatingService CoatingService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private TraceTakeoffMeasurement? measurement = null;
    private List<SurfaceCoating> surfaceCoatings = new();
    private bool isLoading = true;
    private string errorMessage = "";
    private bool isEditMode = false;
    private bool showDeleteModal = false;
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // Edit model
    private MeasurementEditModel editModel = new();

    // Section collapse management
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true },
        { "item", true },
        { "properties", true },
        { "location", false },
        { "audit", false }
    };

    protected override async Task OnInitializedAsync()
    {
        // Get current user ID and company ID from authentication
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

        if (currentUserId == 0 || currentCompanyId == 0)
        {
            Logger.LogWarning("User is not authenticated or missing required claims");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = "";

            // Load measurement using authenticated user's company
            measurement = await CatalogueService.GetMeasurementByIdAsync(Id, companyId: currentCompanyId);

            if (measurement != null)
            {
                // Initialize edit model
                editModel = new MeasurementEditModel
                {
                    SurfaceCoatingId = measurement.SurfaceCoatingId?.ToString() ?? "",
                    Status = measurement.Status ?? "",
                    Notes = measurement.Notes ?? "",
                    Label = measurement.Label ?? "",
                    Description = measurement.Description ?? "",
                    Color = measurement.Color ?? "#667eea"
                };
            }
            else
            {
                errorMessage = "Measurement not found.";
            }

            // Load surface coatings for authenticated user's company
            surfaceCoatings = await CoatingService.GetActiveCoatingsAsync(companyId: currentCompanyId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading measurement {Id}", Id);
            errorMessage = "Failed to load measurement details.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private bool IsSectionExpanded(string sectionKey)
    {
        return sectionStates.ContainsKey(sectionKey) && sectionStates[sectionKey];
    }

    private void ToggleSection(string sectionKey)
    {
        if (sectionStates.ContainsKey(sectionKey))
        {
            sectionStates[sectionKey] = !sectionStates[sectionKey];
        }
        else
        {
            sectionStates[sectionKey] = true;
        }
    }

    private async Task ToggleEditModeAsync()
    {
        if (isEditMode)
        {
            // Cancel edit - reload data
            await LoadDataAsync();
        }

        isEditMode = !isEditMode;
        StateHasChanged();
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            if (measurement == null) return;

            var surfaceCoatingId = string.IsNullOrEmpty(editModel.SurfaceCoatingId) ? (int?)null : int.Parse(editModel.SurfaceCoatingId);
            var status = string.IsNullOrEmpty(editModel.Status) ? null : editModel.Status;

            var updated = await CatalogueService.UpdateMeasurementDetailsAsync(
                measurementId: Id,
                surfaceCoatingId: surfaceCoatingId,
                status: status,
                notes: editModel.Notes,
                label: editModel.Label,
                description: editModel.Description,
                color: editModel.Color,
                userId: currentUserId, // Use authenticated user ID
                companyId: currentCompanyId
            );

            if (updated != null)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Measurement updated successfully!");
                isEditMode = false;
                await LoadDataAsync();
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", "Failed to update measurement.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving measurement changes");
            await JSRuntime.InvokeVoidAsync("alert", "An error occurred while saving.");
        }
    }

    private void ShowDeleteConfirmation()
    {
        showDeleteModal = true;
    }

    private void CloseDeleteModal()
    {
        showDeleteModal = false;
    }

    private async Task ConfirmDelete()
    {
        try
        {
            var deletedAnnotationIds = await CatalogueService.DeleteMeasurementAsync(Id, companyId: currentCompanyId, userId: currentUserId);

            if (deletedAnnotationIds != null)
            {
                Logger.LogInformation("Measurement {Id} deleted successfully by user {UserId}. Deleted {Count} PDF annotations.",
                    Id, currentUserId, deletedAnnotationIds.Count);

                // Navigate back to measurements list (or drawing detail)
                if (measurement?.PackageDrawingId != null)
                {
                    Navigation.NavigateTo($"/{TenantSlug}/trace/packages/{measurement.PackageDrawing?.PackageId}/drawings/{measurement.PackageDrawingId}/measurements");
                }
                else
                {
                    Navigation.NavigateTo($"/{TenantSlug}/trace/measurements");
                }
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", "Failed to delete measurement.");
                showDeleteModal = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting measurement {Id}", Id);
            await JSRuntime.InvokeVoidAsync("alert", "An error occurred while deleting.");
            showDeleteModal = false;
        }
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();

        if (isEditMode)
        {
            group.PrimaryActions.Add(new ToolbarAction
            {
                Text = "Save",
                Label = "Save",
                Icon = "fas fa-save",
                Action = EventCallback.Factory.Create(this, SaveChangesAsync),
                Style = ToolbarActionStyle.Primary
            });

            group.PrimaryActions.Add(new ToolbarAction
            {
                Text = "Cancel",
                Label = "Cancel",
                Icon = "fas fa-times",
                Action = EventCallback.Factory.Create(this, ToggleEditModeAsync),
                Style = ToolbarActionStyle.Secondary
            });
        }
        else
        {
            group.PrimaryActions.Add(new ToolbarAction
            {
                Text = "Edit",
                Label = "Edit",
                Icon = "fas fa-edit",
                Action = EventCallback.Factory.Create(this, ToggleEditModeAsync),
                Style = ToolbarActionStyle.Primary
            });

            group.MenuActions.Add(new ToolbarAction
            {
                Text = "Delete",
                Label = "Delete",
                Icon = "fas fa-trash",
                Action = EventCallback.Factory.Create(this, ShowDeleteConfirmation),
                Style = ToolbarActionStyle.Danger
            });
        }

        return group;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    // Edit model class
    private class MeasurementEditModel
    {
        public string SurfaceCoatingId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public string Color { get; set; } = "#667eea";
    }
}
