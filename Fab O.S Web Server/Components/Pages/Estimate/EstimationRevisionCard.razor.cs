using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Components.Shared.Interfaces;
using System.Security.Claims;
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class EstimationRevisionCard : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int EstimationId { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<EstimationRevisionCard> Logger { get; set; } = default!;

    private EstimationRevision? revision;
    private Estimation? estimation;
    private bool isLoading = true;
    private bool isEditMode = false;
    private bool isSaving = false;
    private string? errorMessage;
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // Section expansion tracking
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true },
        { "cost", true }
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
            Logger.LogWarning("User is not authenticated or missing required claims for EstimationRevisionCard page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadRevision();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Id != revision?.Id || EstimationId != estimation?.Id)
        {
            await LoadRevision();
        }
    }

    private async Task LoadRevision()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            // Load estimation
            estimation = await DbContext.Estimations
                .FirstOrDefaultAsync(e => e.Id == EstimationId && e.CompanyId == currentCompanyId && !e.IsDeleted);

            if (estimation == null)
            {
                errorMessage = "Estimation not found";
                return;
            }

            if (Id == 0)
            {
                // New revision - get next revision letter
                var lastLetter = estimation.Revisions
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.RevisionLetter)
                    .Select(r => r.RevisionLetter)
                    .FirstOrDefault() ?? "@";

                var nextLetter = ((char)(lastLetter[0] + 1)).ToString();

                revision = new EstimationRevision
                {
                    CompanyId = currentCompanyId,
                    EstimationId = EstimationId,
                    RevisionLetter = nextLetter,
                    Status = "Draft",
                    ValidUntilDate = DateTime.UtcNow.AddDays(30),
                    OverheadPercentage = 15m,
                    MarginPercentage = 20m,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };
                isEditMode = true;
            }
            else
            {
                // Load existing revision
                revision = await DbContext.EstimationRevisions
                    .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .FirstOrDefaultAsync(r => r.Id == Id && r.EstimationId == EstimationId && r.CompanyId == currentCompanyId && !r.IsDeleted);

                if (revision == null)
                {
                    errorMessage = "Revision not found";
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading revision {Id} for estimation {EstimationId}", Id, EstimationId);
            errorMessage = $"Error loading revision: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveRevision()
    {
        if (!isEditMode || revision == null || isSaving) return;

        try
        {
            isSaving = true;
            StateHasChanged();

            if (Id == 0)
            {
                // Create new revision
                // Supersede previous revision
                var lastLetter = estimation!.Revisions
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.RevisionLetter)
                    .Select(r => r.RevisionLetter)
                    .FirstOrDefault() ?? "@";

                if (lastLetter != "@")
                {
                    var previousRevision = estimation.Revisions
                        .FirstOrDefault(r => r.RevisionLetter == lastLetter && !r.IsDeleted);

                    if (previousRevision != null)
                    {
                        previousRevision.SupersededBy = revision.RevisionLetter;
                        previousRevision.ModifiedDate = DateTime.UtcNow;
                    }
                }

                revision.SupersedesLetter = lastLetter != "@" ? lastLetter : null;
                DbContext.EstimationRevisions.Add(revision);

                // Update estimation current revision
                estimation.CurrentRevisionLetter = revision.RevisionLetter;
                estimation.ModifiedDate = DateTime.UtcNow;

                await DbContext.SaveChangesAsync();

                Logger.LogInformation("Created revision {Letter} for estimation {EstimationId}", revision.RevisionLetter, EstimationId);

                Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions/{revision.Id}", replace: true);
            }
            else
            {
                // Update existing revision
                revision.ModifiedBy = currentUserId;
                revision.ModifiedDate = DateTime.UtcNow;
                await DbContext.SaveChangesAsync();

                Logger.LogInformation("Updated revision {Id}", Id);

                await LoadRevision();
                isEditMode = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving revision {Id}", Id);
            errorMessage = $"Error saving revision: {ex.Message}";
            StateHasChanged();
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    private async Task SubmitRevision()
    {
        if (revision == null || Id == 0 || revision.Status != "Draft") return;

        try
        {
            revision.Status = "SubmittedForReview";
            revision.SubmittedBy = currentUserId;
            revision.SubmittedDate = DateTime.UtcNow;
            revision.ModifiedDate = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            Logger.LogInformation("Submitted revision {Id} for review", Id);

            await LoadRevision();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error submitting revision {Id}", Id);
            errorMessage = "Error submitting revision. Please try again.";
        }
    }

    private async Task DeleteRevision()
    {
        if (revision == null || Id == 0) return;

        try
        {
            revision.IsDeleted = true;
            revision.ModifiedBy = currentUserId;
            revision.ModifiedDate = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            Logger.LogInformation("Deleted revision {Id}", Id);

            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting revision {Id}", Id);
            errorMessage = $"Error deleting revision: {ex.Message}";
            StateHasChanged();
        }
    }

    private void StartEdit()
    {
        if (revision?.Status == "Draft")
        {
            isEditMode = true;
            StateHasChanged();
        }
    }

    private void CancelEdit()
    {
        isEditMode = false;
        _ = LoadRevision(); // Reload to reset changes
    }

    private bool IsSectionExpanded(string sectionName)
    {
        return sectionStates.ContainsKey(sectionName) && sectionStates[sectionName];
    }

    private void ToggleSection(string sectionName)
    {
        if (sectionStates.ContainsKey(sectionName))
        {
            sectionStates[sectionName] = !sectionStates[sectionName];
        }
        else
        {
            sectionStates[sectionName] = true;
        }
        StateHasChanged();
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

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var actionGroup = new ToolbarActionGroup();

        if (revision != null)
        {
            // Primary Actions
            if (Id == 0)
            {
                actionGroup.PrimaryActions.Add(new ToolbarAction
                {
                    Label = "Save",
                    Text = "Save",
                    Icon = "fas fa-save",
                    ActionFunc = SaveRevision,
                    Style = ToolbarActionStyle.Primary,
                    IsDisabled = isSaving
                });
            }
            else
            {
                if (isEditMode)
                {
                    actionGroup.PrimaryActions.Add(new ToolbarAction
                    {
                        Label = "Save",
                        Text = "Save",
                        Icon = "fas fa-save",
                        ActionFunc = SaveRevision,
                        Style = ToolbarActionStyle.Primary,
                        IsDisabled = isSaving
                    });
                    actionGroup.PrimaryActions.Add(new ToolbarAction
                    {
                        Text = "Cancel",
                        Icon = "fas fa-times",
                        ActionFunc = () => { CancelEdit(); return Task.CompletedTask; }
                    });
                }
                else
                {
                    actionGroup.PrimaryActions.Add(new ToolbarAction
                    {
                        Text = "Back",
                        Icon = "fas fa-arrow-left",
                        ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions"); return Task.CompletedTask; },
                        Style = ToolbarActionStyle.Secondary
                    });

                    if (revision.Status == "Draft")
                    {
                        actionGroup.PrimaryActions.Add(new ToolbarAction
                        {
                            Text = "Edit",
                            Icon = "fas fa-edit",
                            ActionFunc = () => { StartEdit(); return Task.CompletedTask; }
                        });
                        actionGroup.PrimaryActions.Add(new ToolbarAction
                        {
                            Text = "Submit for Review",
                            Icon = "fas fa-paper-plane",
                            ActionFunc = SubmitRevision,
                            Style = ToolbarActionStyle.Primary
                        });
                    }
                }
            }

            // Menu Actions
            if (Id > 0 && !isEditMode)
            {
                if (revision.Status == "Draft")
                {
                    actionGroup.MenuActions.Add(new ToolbarAction
                    {
                        Text = "Delete",
                        Icon = "fas fa-trash",
                        ActionFunc = DeleteRevision,
                        Style = ToolbarActionStyle.Danger,
                        Tooltip = "Delete this revision"
                    });
                }
            }

            // Related Actions
            actionGroup.RelatedActions.Add(new ToolbarAction
            {
                Text = "Back to Estimation",
                Icon = "fas fa-file-alt",
                ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}"); return Task.CompletedTask; }
            });

            actionGroup.RelatedActions.Add(new ToolbarAction
            {
                Text = "All Revisions",
                Icon = "fas fa-list",
                ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions"); return Task.CompletedTask; }
            });

            if (Id > 0)
            {
                actionGroup.RelatedActions.Add(new ToolbarAction
                {
                    Text = "Packages",
                    Icon = "fas fa-box",
                    ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{EstimationId}/revisions/{Id}/packages"); return Task.CompletedTask; },
                    Tooltip = "View packages in this revision"
                });
            }
        }

        return actionGroup;
    }
}
