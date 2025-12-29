using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;
// Resolve ambiguous ToolbarAction reference
using ToolbarAction = FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction;

namespace FabOS.WebServer.Components.Pages.Estimate;

public partial class EstimatePackageCard : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    [SupplyParameterFromQuery(Name = "revisionId")]
    public int? RevisionId { get; set; }

    [Inject] private ApplicationDbContext Context { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IEstimationCalculationService CalculationService { get; set; } = default!;
    [Inject] private IWorksheetExcelService ExcelService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<EstimatePackageCard> Logger { get; set; } = default!;

    private bool isLoading = true;
    private bool isEditMode = false;
    private bool isCreateMode = false;
    private string? errorMessage;

    private EstimationRevisionPackage? package;
    private EstimationRevision? parentRevision;
    private List<EstimationWorksheetTemplate> templates = new();

    // Authentication state
    private int currentUserId = 0;
    private int currentCompanyId = 0;

    // Add Worksheet Dialog
    private bool showAddWorksheetDialog = false;
    private int? selectedTemplateId;
    private string newWorksheetName = "";
    private string newWorksheetDescription = "";
    private string newWorksheetType = "MaterialCosts";

    // Section collapse management (same pattern as EstimationCard)
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true },
        { "cost", true },
        { "worksheets", true }
    };

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

    private int? previousId;

    protected override async Task OnParametersSetAsync()
    {
        // Detect when URL parameters change (e.g., after creating a package and navigating)
        if (previousId.HasValue && previousId.Value != Id)
        {
            // Reset state for new package
            isCreateMode = false;
            isEditMode = false;
            errorMessage = null;
            package = null;
            parentRevision = null;

            await LoadPackage();
        }
        previousId = Id;
    }

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
            Logger.LogWarning("User is not authenticated or missing required claims for EstimatePackageCard page");
            errorMessage = "User is not authenticated. Please log in and try again.";
            isLoading = false;
            return;
        }

        await LoadPackage();
        await LoadTemplates();
    }

    private async Task LoadPackage()
    {
        try
        {
            isLoading = true;

            // Create mode: Id == 0 and RevisionId provided
            if (Id == 0 && RevisionId.HasValue)
            {
                isCreateMode = true;
                isEditMode = true;

                // Load parent revision to validate and get context
                parentRevision = await Context.EstimationRevisions
                    .Include(r => r.Estimation)
                    .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .FirstOrDefaultAsync(r => r.Id == RevisionId.Value && r.CompanyId == currentCompanyId && !r.IsDeleted);

                if (parentRevision == null)
                {
                    errorMessage = "Revision not found. Cannot create package.";
                    return;
                }

                // Calculate next sort order
                var nextSortOrder = parentRevision.Packages.Any()
                    ? parentRevision.Packages.Max(p => p.SortOrder) + 1
                    : 0;

                // Create a new package object (not saved yet)
                package = new EstimationRevisionPackage
                {
                    CompanyId = currentCompanyId,
                    RevisionId = RevisionId.Value,
                    Name = "",
                    Description = "",
                    SortOrder = nextSortOrder,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    Revision = parentRevision
                };

                Logger.LogInformation("Creating new package for revision {RevisionId}", RevisionId);
                return;
            }

            // Edit/View mode: Load existing package
            package = await Context.EstimationRevisionPackages
                .Include(p => p.Revision)
                    .ThenInclude(r => r!.Estimation)
                .Include(p => p.Worksheets.Where(w => !w.IsDeleted))
                    .ThenInclude(w => w.Rows.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == Id && p.CompanyId == currentCompanyId && !p.IsDeleted);

            if (package == null)
            {
                errorMessage = "Package not found.";
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading package {Id}", Id);
            errorMessage = "Error loading package. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadTemplates()
    {
        try
        {
            templates = await Context.EstimationWorksheetTemplates
                .Where(t => t.CompanyId == currentCompanyId && !t.IsDeleted)
                .OrderBy(t => t.WorksheetType)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading templates");
        }
    }

    private void GoBack()
    {
        // In create mode, use parentRevision for navigation
        if (isCreateMode && parentRevision != null)
        {
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{parentRevision.EstimationId}/revisions/{parentRevision.Id}/packages");
            return;
        }

        if (package?.Revision != null && package.Revision.EstimationId > 0)
        {
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{package.Revision.EstimationId}/revisions/{package.Revision.Id}/packages");
        }
        else if (package?.Revision?.EstimationId != null)
        {
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{package.Revision.EstimationId}");
        }
        else
        {
            Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations");
        }
    }

    private async Task SaveChanges()
    {
        if (package == null)
            return;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(package.Name))
        {
            errorMessage = "Package name is required.";
            return;
        }

        try
        {
            // Check if this is a new package (Id == 0) rather than relying on isCreateMode flag
            // because Blazor Server may not re-run OnInitializedAsync when URL params change
            if (package.Id == 0)
            {
                // Insert new package
                Context.EstimationRevisionPackages.Add(package);
                await Context.SaveChangesAsync();

                Logger.LogInformation("Created new package {Id} for revision {RevisionId}", package.Id, package.RevisionId);

                // Reset create mode flag and navigate to the newly created package
                isCreateMode = false;
                isEditMode = false;
                Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/{package.Id}", replace: true);
                return;
            }

            // Update existing package
            package.ModifiedBy = currentUserId;
            package.ModifiedDate = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            Logger.LogInformation("Updated package {Id}", package.Id);
            isEditMode = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving package {Id}", package?.Id);
            errorMessage = "Error saving package. Please try again.";
        }
    }

    private async Task DeletePackage()
    {
        if (package == null)
            return;

        try
        {
            package.IsDeleted = true;
            package.ModifiedBy = currentUserId;
            package.ModifiedDate = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            Logger.LogInformation("Deleted package {Id}", package.Id);
            GoBack();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting package {Id}", package?.Id);
            errorMessage = "Error deleting package. Please try again.";
        }
    }

    private void OpenWorksheet(int worksheetId)
    {
        Navigation.NavigateTo($"/{TenantSlug}/estimate/worksheets/{worksheetId}");
    }

    private async Task AddWorksheet()
    {
        if (package == null || string.IsNullOrWhiteSpace(newWorksheetName))
            return;

        try
        {
            var nextSortOrder = package.Worksheets.Any()
                ? package.Worksheets.Max(w => w.SortOrder) + 1
                : 0;

            var worksheet = new EstimationWorksheet
            {
                CompanyId = currentCompanyId,
                PackageId = package.Id,
                TemplateId = selectedTemplateId,
                Name = newWorksheetName,
                Description = newWorksheetDescription,
                WorksheetType = newWorksheetType,
                SortOrder = nextSortOrder,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            Context.EstimationWorksheets.Add(worksheet);
            await Context.SaveChangesAsync();

            // Copy columns from template if selected
            if (selectedTemplateId.HasValue)
            {
                var templateColumns = await Context.EstimationWorksheetColumns
                    .Where(c => c.TemplateId == selectedTemplateId && !c.IsDeleted)
                    .OrderBy(c => c.SortOrder)
                    .ToListAsync();

                foreach (var templateCol in templateColumns)
                {
                    var column = new EstimationWorksheetInstanceColumn
                    {
                        WorksheetId = worksheet.Id,
                        ColumnKey = templateCol.ColumnKey,
                        ColumnName = templateCol.ColumnName,
                        DataType = templateCol.DataType,
                        Width = templateCol.Width,
                        IsRequired = templateCol.IsRequired,
                        IsReadOnly = templateCol.IsReadOnly,
                        IsFrozen = templateCol.IsFrozen,
                        IsHidden = templateCol.IsHidden,
                        Formula = templateCol.Formula,
                        DefaultValue = templateCol.DefaultValue,
                        SelectOptions = templateCol.SelectOptions,
                        Precision = templateCol.Precision,
                        SortOrder = templateCol.SortOrder
                    };
                    Context.EstimationWorksheetInstanceColumns.Add(column);
                }
                await Context.SaveChangesAsync();
            }

            Logger.LogInformation("Added worksheet {WorksheetName} to package {PackageId}", newWorksheetName, package.Id);

            showAddWorksheetDialog = false;
            selectedTemplateId = null;
            newWorksheetName = "";
            newWorksheetDescription = "";
            newWorksheetType = "MaterialCosts";

            await LoadPackage();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding worksheet to package {PackageId}", package?.Id);
            errorMessage = "Error adding worksheet. Please try again.";
        }
    }

    private async Task DeleteWorksheet(int worksheetId)
    {
        try
        {
            var worksheet = await Context.EstimationWorksheets
                .FirstOrDefaultAsync(w => w.Id == worksheetId && w.CompanyId == currentCompanyId && !w.IsDeleted);

            if (worksheet == null)
                return;

            worksheet.IsDeleted = true;
            worksheet.ModifiedBy = currentUserId;
            worksheet.ModifiedDate = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            Logger.LogInformation("Deleted worksheet {WorksheetId}", worksheetId);

            await LoadPackage();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting worksheet {WorksheetId}", worksheetId);
            errorMessage = "Error deleting worksheet. Please try again.";
        }
    }

    private async Task ExportWorksheet(int worksheetId)
    {
        try
        {
            var bytes = await ExcelService.ExportWorksheetAsync(worksheetId);
            var worksheet = package?.Worksheets.FirstOrDefault(w => w.Id == worksheetId);
            var fileName = $"{worksheet?.Name ?? "worksheet"}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            // Download file via JS interop
            await JSRuntime.InvokeVoidAsync("downloadFileFromBytes", fileName, bytes);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting worksheet {WorksheetId}", worksheetId);
            errorMessage = "Error exporting worksheet. Please try again.";
        }
    }

    private async Task ExportPackage()
    {
        if (package == null)
            return;

        try
        {
            var bytes = await ExcelService.ExportPackageAsync(package.Id);
            var fileName = $"{package.Name}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            // Download file via JS interop
            await JSRuntime.InvokeVoidAsync("downloadFileFromBytes", fileName, bytes);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting package {PackageId}", package?.Id);
            errorMessage = "Error exporting package. Please try again.";
        }
    }

    private async Task RecalculatePackage()
    {
        if (package == null)
            return;

        try
        {
            await CalculationService.RecalculatePackageAsync(package.Id);
            await LoadPackage();

            Logger.LogInformation("Recalculated package {PackageId}", package.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error recalculating package {PackageId}", package?.Id);
            errorMessage = "Error recalculating package. Please try again.";
        }
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

    private string GetWorksheetTypeBadgeClass(string type)
    {
        return type switch
        {
            "MaterialCosts" => "bg-primary",
            "LaborCosts" => "bg-info",
            "WeldingCosts" => "bg-warning text-dark",
            "Equipment" => "bg-success",
            _ => "bg-secondary"
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
                Text = "Back to Estimation",
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
                Style = ToolbarActionStyle.Primary,
                IsDisabled = package?.Revision?.Status != "Draft" && !isEditMode
            }
        };

        // Only show menu actions when not in create mode
        if (!isCreateMode)
        {
            group.MenuActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "Recalculate",
                    Text = "Recalculate Totals",
                    Icon = "fas fa-calculator",
                    ActionFunc = RecalculatePackage
                },
                new()
                {
                    Label = "Export",
                    Text = "Export to Excel",
                    Icon = "fas fa-file-excel",
                    ActionFunc = ExportPackage
                },
                new()
                {
                    Label = "Delete",
                    Text = "Delete Package",
                    Icon = "fas fa-trash",
                    ActionFunc = DeletePackage,
                    Style = ToolbarActionStyle.Danger,
                    IsDisabled = package?.Revision?.Status != "Draft"
                }
            };

            group.RelatedActions = new List<ToolbarAction>
            {
                new()
                {
                    Label = "Costing Worksheets",
                    Text = "View Costing Worksheets",
                    Icon = "fas fa-table",
                    ActionFunc = async () => {
                        Navigation.NavigateTo($"/{TenantSlug}/estimate/packages/{Id}/worksheets");
                        await Task.CompletedTask;
                    }
                }
            };
        }
        else
        {
            group.MenuActions = new List<ToolbarAction>();
            group.RelatedActions = new List<ToolbarAction>();
        }

        // Add revision navigation for existing packages
        if (!isCreateMode && package?.Revision != null && package.Revision.EstimationId > 0)
        {
            group.RelatedActions.Add(new()
            {
                Label = "Back to Packages",
                Text = "Back to Packages",
                Icon = "fas fa-box",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{package.Revision.EstimationId}/revisions/{package.Revision.Id}/packages");
                    await Task.CompletedTask;
                }
            });

            group.RelatedActions.Add(new()
            {
                Label = "Back to Revision",
                Text = "Back to Revision",
                Icon = "fas fa-history",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{package.Revision.EstimationId}/revisions/{package.Revision.Id}");
                    await Task.CompletedTask;
                }
            });
        }

        // Add revision navigation for create mode using parentRevision
        if (isCreateMode && parentRevision != null)
        {
            group.RelatedActions.Add(new()
            {
                Label = "Back to Packages",
                Text = "Back to Packages",
                Icon = "fas fa-box",
                ActionFunc = async () => {
                    Navigation.NavigateTo($"/{TenantSlug}/estimate/estimations/{parentRevision.EstimationId}/revisions/{parentRevision.Id}/packages");
                    await Task.CompletedTask;
                }
            });
        }

        group.RelatedActions.Add(new()
        {
            Label = "Templates",
            Text = "Worksheet Templates",
            Icon = "fas fa-table",
            ActionFunc = async () => {
                Navigation.NavigateTo($"/{TenantSlug}/estimate/templates");
                await Task.CompletedTask;
            }
        });

        return group;
    }

    #endregion
}
