using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class EstimationCard : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext Context { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IEstimationCalculationService CalculationService { get; set; } = default!;
    [Inject] private ILogger<EstimationCard> Logger { get; set; } = default!;

    private bool isLoading = true;
    private bool isEditMode = false;
    private string? errorMessage;

    private Estimation? estimation;
    private EstimationRevision? currentRevision;
    private List<Customer> customers = new();

    // Authentication state
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // Section collapse management
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true }  // General section expanded by default
    };

    protected override async Task OnInitializedAsync()
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

        if (currentUserId == 0 || currentCompanyId == 0)
        {
            Logger.LogWarning("User is not authenticated or missing required claims for EstimationCard page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadEstimation();
        await LoadCustomers();
    }

    private async Task LoadEstimation()
    {
        try
        {
            isLoading = true;

            estimation = await Context.Estimations
                .Include(e => e.Customer)
                .Include(e => e.Revisions.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(e => e.Id == Id && e.CompanyId == currentCompanyId && !e.IsDeleted);

            if (estimation == null)
            {
                errorMessage = "Estimation not found.";
                return;
            }

            // Get current revision
            currentRevision = estimation.Revisions
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.RevisionLetter == estimation.CurrentRevisionLetter)
                .ThenByDescending(r => r.RevisionLetter)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading estimation {Id}", Id);
            errorMessage = "Error loading estimation. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadCustomers()
    {
        try
        {
            customers = await Context.Customers
                .Where(c => c.CompanyId == currentCompanyId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading customers");
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations");
    }

    private async Task SaveChanges()
    {
        if (estimation == null)
            return;

        try
        {
            estimation.ModifiedBy = currentUserId;
            estimation.ModifiedDate = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            Logger.LogInformation("Updated estimation {Id}", estimation.Id);
            isEditMode = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving estimation {Id}", estimation?.Id);
            errorMessage = "Error saving estimation. Please try again.";
        }
    }

    private async Task DeleteEstimation()
    {
        if (estimation == null)
            return;

        try
        {
            estimation.IsDeleted = true;
            estimation.ModifiedBy = currentUserId;
            estimation.ModifiedDate = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            Logger.LogInformation("Deleted estimation {Id}", estimation.Id);
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting estimation {Id}", estimation?.Id);
            errorMessage = "Error deleting estimation. Please try again.";
        }
    }


    private void ToggleSection(string sectionName)
    {
        if (sectionStates.ContainsKey(sectionName))
        {
            sectionStates[sectionName] = !sectionStates[sectionName];
            StateHasChanged();
        }
    }

    private bool IsSectionExpanded(string sectionName)
    {
        return sectionStates.ContainsKey(sectionName) && sectionStates[sectionName];
    }

    private string GetStatusBadgeClass(string? status)
    {
        return status?.ToLower() switch
        {
            "draft" => "status-draft",
            "inprogress" => "status-inprogress",
            "submittedforreview" => "status-submittedforreview",
            "inreview" => "status-inreview",
            "approved" => "status-approved",
            "rejected" => "status-rejected",
            "sent" => "status-sent",
            "accepted" => "status-accepted",
            "customerrejected" => "status-customerrejected",
            _ => "status-default"
        };
    }

    #region IToolbarActionProvider Implementation

    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();

        group.PrimaryActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Back",
                Text = "Back to List",
                Icon = "fas fa-arrow-left",
                ActionFunc = async () => { GoBack(); await Task.CompletedTask; },
                Style = ToolbarActionStyle.Secondary
            },
            new()
            {
                Label = isEditMode ? "Save" : "Edit",
                Text = isEditMode ? "Save" : "Edit",
                Icon = isEditMode ? "fas fa-save" : "fas fa-edit",
                ActionFunc = isEditMode ? SaveChanges : async () => { isEditMode = true; StateHasChanged(); await Task.CompletedTask; },
                Style = ToolbarActionStyle.Primary
            }
        };

        group.MenuActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Delete",
                Text = "Delete Estimation",
                Icon = "fas fa-trash",
                ActionFunc = DeleteEstimation,
                Style = ToolbarActionStyle.Danger
            },
            new()
            {
                Label = "Duplicate",
                Text = "Duplicate Estimation",
                Icon = "fas fa-copy",
                ActionFunc = async () => { /* TODO: Duplicate */ await Task.CompletedTask; }
            },
            new()
            {
                Label = "Export",
                Text = "Export to Excel",
                Icon = "fas fa-file-excel",
                ActionFunc = async () => { /* TODO: Export */ await Task.CompletedTask; }
            }
        };

        group.RelatedActions = new List<ToolbarAction>
        {
            new()
            {
                Label = "Revisions",
                Text = "View Revisions",
                Icon = "fas fa-history",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{Id}/revisions");
                    await Task.CompletedTask;
                }
            },
            new()
            {
                Label = "Templates",
                Text = "Worksheet Templates",
                Icon = "fas fa-table",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/templates");
                    await Task.CompletedTask;
                }
            }
        };

        if (estimation?.CustomerId > 0)
        {
            group.RelatedActions.Add(new()
            {
                Label = "Customer",
                Text = "View Customer",
                Icon = "fas fa-building",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/trace/customers/{estimation.CustomerId}");
                    await Task.CompletedTask;
                }
            });
        }

        return group;
    }

    #endregion
}
