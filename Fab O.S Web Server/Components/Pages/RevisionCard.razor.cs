using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Components.Pages;

public partial class RevisionCard : ComponentBase, IToolbarActionProvider
{
    [Parameter] public int TakeoffId { get; set; }
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ITakeoffRevisionService RevisionService { get; set; } = default!;

    private TakeoffRevision? revision;
    private TraceDrawing? takeoff;
    private bool isLoading = true;
    private bool isEditMode = false;
    private bool isSaving = false;
    private string? errorMessage;

    // Section expansion tracking
    private HashSet<string> expandedSections = new() { "general", "packages" };

    protected override async Task OnInitializedAsync()
    {
        await LoadRevision();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Id != revision?.Id || TakeoffId != takeoff?.Id)
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
            // Load takeoff
            takeoff = await DbContext.TraceDrawings.FindAsync(TakeoffId);
            if (takeoff == null)
            {
                errorMessage = "Takeoff not found";
                return;
            }

            if (Id == 0)
            {
                // New revision
                revision = new TakeoffRevision
                {
                    TakeoffId = TakeoffId,
                    RevisionCode = await RevisionService.GetNextRevisionCodeAsync(TakeoffId),
                    IsActive = true,
                    Description = "",
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    IsDeleted = false
                };
                isEditMode = true;
            }
            else
            {
                // Load existing revision
                revision = await RevisionService.GetRevisionByIdAsync(Id);
                if (revision == null)
                {
                    errorMessage = "Revision not found";
                    return;
                }
            }
        }
        catch (Exception ex)
        {
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
                var newRevision = await RevisionService.CreateRevisionAsync(
                    TakeoffId,
                    revision.Description,
                    userId: null); // TODO: Implement authentication system

                Navigation.NavigateTo($"/takeoffs/{TakeoffId}/revisions/{newRevision.Id}", replace: true);
            }
            else
            {
                // Update existing revision
                var existingRevision = await DbContext.TakeoffRevisions.FindAsync(Id);
                if (existingRevision != null)
                {
                    existingRevision.Description = revision.Description;
                    existingRevision.LastModified = DateTime.UtcNow;
                    await DbContext.SaveChangesAsync();
                }

                await LoadRevision();
                isEditMode = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving revision: {ex.Message}";
            StateHasChanged();
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    private async Task DeleteRevision()
    {
        if (revision == null || Id == 0) return;

        try
        {
            await RevisionService.DeleteRevisionAsync(Id);
            Navigation.NavigateTo($"/takeoffs/{TakeoffId}/revisions");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting revision: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task SetAsActive()
    {
        if (revision == null || Id == 0) return;

        try
        {
            await RevisionService.SetActiveRevisionAsync(Id);
            await LoadRevision();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error setting active revision: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task CopyRevision()
    {
        if (revision == null || Id == 0) return;

        try
        {
            var newRevision = await RevisionService.CopyRevisionAsync(
                Id,
                $"Copy of {revision.RevisionCode}",
                userId: null); // TODO: Implement authentication system

            Navigation.NavigateTo($"/takeoffs/{TakeoffId}/revisions/{newRevision.Id}");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error copying revision: {ex.Message}";
            StateHasChanged();
        }
    }

    private void StartEdit()
    {
        isEditMode = true;
        StateHasChanged();
    }

    private void CancelEdit()
    {
        isEditMode = false;
        _ = LoadRevision(); // Reload to reset changes
    }

    private void NavigateToPackage(int packageId)
    {
        Navigation.NavigateTo($"/packages/{packageId}");
    }

    private bool IsSectionExpanded(string sectionName)
    {
        return expandedSections.Contains(sectionName);
    }

    private void ToggleSection(string sectionName)
    {
        if (expandedSections.Contains(sectionName))
        {
            expandedSections.Remove(sectionName);
        }
        else
        {
            expandedSections.Add(sectionName);
        }
        StateHasChanged();
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
                        Text = "Edit",
                        Icon = "fas fa-edit",
                        ActionFunc = () => { StartEdit(); return Task.CompletedTask; }
                    });
                }
            }

            // Menu Actions
            if (Id > 0)
            {
                if (!revision.IsActive)
                {
                    actionGroup.MenuActions.Add(new ToolbarAction
                    {
                        Text = "Set as Active",
                        Icon = "fas fa-check-circle",
                        ActionFunc = SetAsActive,
                        Tooltip = "Make this the working revision"
                    });
                }

                actionGroup.MenuActions.Add(new ToolbarAction
                {
                    Text = "Copy to New Revision",
                    Icon = "fas fa-copy",
                    ActionFunc = CopyRevision,
                    Tooltip = "Create a new revision based on this one"
                });

                if (!revision.IsActive)
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
                Text = "View Takeoff",
                Icon = "fas fa-file-alt",
                ActionFunc = () => { Navigation.NavigateTo($"/takeoffs/{TakeoffId}"); return Task.CompletedTask; }
            });

            actionGroup.RelatedActions.Add(new ToolbarAction
            {
                Text = "All Revisions",
                Icon = "fas fa-list",
                ActionFunc = () => { Navigation.NavigateTo($"/takeoffs/{TakeoffId}/revisions"); return Task.CompletedTask; }
            });

            if (Id > 0)
            {
                actionGroup.RelatedActions.Add(new ToolbarAction
                {
                    Text = "Packages",
                    Icon = "fas fa-box",
                    ActionFunc = () => { Navigation.NavigateTo($"/takeoffs/{TakeoffId}/revisions/{Id}/packages"); return Task.CompletedTask; },
                    Tooltip = "View packages in this revision"
                });
            }
        }

        return actionGroup;
    }
}
