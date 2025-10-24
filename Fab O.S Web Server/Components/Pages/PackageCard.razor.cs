using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using FabOS.WebServer.Services;

namespace FabOS.WebServer.Components.Pages;

public partial class PackageCard : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int Id { get; set; }

    // Query parameters
    [SupplyParameterFromQuery(Name = "takeoffId")]
    public int? TakeoffIdFromQuery { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private FabOS.WebServer.Services.BreadcrumbService BreadcrumbService { get; set; } = default!;
    [Inject] private ILogger<PackageCard> Logger { get; set; } = default!;
    [Inject] private NumberSeriesService NumberSeriesService { get; set; } = default!;
    [Inject] private FabOS.WebServer.Services.Interfaces.ITakeoffRevisionService RevisionService { get; set; } = default!;

    private Package? package = null;
    private bool isLoading = true;
    private string errorMessage = "";
    private int fileCount = 0;

    // For new packages
    private bool isEditMode = true;

    // Section collapse management
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true },  // General section expanded by default
        { "schedule", true }, // Schedule section expanded by default
        { "related", false }  // Related section collapsed by default
    };
    private bool packageNumberGenerated = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadPackage();
        await UpdateBreadcrumbAsync();
    }

    private async Task UpdateBreadcrumbAsync()
    {
        if (Id == 0)
        {
            // For new packages, use custom label
            await BreadcrumbService.BuildAndSetSimpleBreadcrumbAsync(
                "Packages",
                "/packages",
                "Package",
                null,
                package?.PackageNumber ?? "New Package"
            );
        }
        else
        {
            // For existing packages, use the breadcrumb builder to load the actual package number
            await BreadcrumbService.BuildAndSetSimpleBreadcrumbAsync(
                "Packages",
                "/packages",
                "Package",
                Id
            );
        }
    }

    private async Task LoadPackage()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            if (Id == 0)
            {
                // Create new package
                package = new Package
                {
                    Id = 0,
                    PackageName = "New Package",
                    Description = "",
                    Status = "Planning",
                    PackageSource = TakeoffIdFromQuery.HasValue ? "Takeoff" : "Project",
                    EstimatedHours = 0,
                    EstimatedCost = 0,
                    ActualHours = 0,
                    ActualCost = 0,
                    LaborRatePerHour = 150,
                    ProcessingEfficiency = 100,
                    ProjectId = 1,
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    IsDeleted = false
                };

                // If creating from a takeoff, link to the active revision
                if (TakeoffIdFromQuery.HasValue)
                {
                    try
                    {
                        var activeRevision = await RevisionService.GetOrCreateActiveRevisionAsync(TakeoffIdFromQuery.Value);
                        package.RevisionId = activeRevision.Id;
                        Logger.LogInformation($"Linking new package to revision {activeRevision.RevisionCode} (ID: {activeRevision.Id}) for takeoff {TakeoffIdFromQuery.Value}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"Failed to get or create active revision for takeoff {TakeoffIdFromQuery.Value}");
                        errorMessage = $"Error linking package to takeoff: {ex.Message}";
                    }
                }

                // Auto-generate package number immediately for new packages
                await GeneratePackageNumber();
                await UpdateBreadcrumbAsync();

                isEditMode = true;
            }
            else
            {
                // Load existing package with related data
                package = await DbContext.Packages
                    .Include(p => p.PackageDrawings)
                    .FirstOrDefaultAsync(p => p.Id == Id);

                if (package == null)
                {
                    errorMessage = $"Package with ID {Id} not found.";
                }
                else
                {
                    // Generate package number for existing records that don't have one
                    if (string.IsNullOrEmpty(package.PackageNumber))
                    {
                        try
                        {
                            package.PackageNumber = await NumberSeriesService.GetNextNumberAsync("Package", 1);
                            DbContext.Packages.Update(package);
                            await DbContext.SaveChangesAsync();
                            Logger.LogInformation($"Generated package number {package.PackageNumber} for existing package ID {Id}");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, $"Failed to generate package number for existing package ID {Id}");
                            // Continue without number generation if it fails
                        }
                    }

                    // Count SharePoint files
                    fileCount = package.PackageDrawings?.Count(d => d.IsActive) ?? 0;

                    // Set edit mode as default for existing packages
                    isEditMode = true;
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading package: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task GeneratePackageNumber()
    {
        if (!packageNumberGenerated && package != null)
        {
            try
            {
                package.PackageNumber = await NumberSeriesService.GetNextNumberAsync("Package", 1);
                packageNumberGenerated = true;
                Logger.LogInformation($"Generated package number: {package.PackageNumber}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate package number");
                // Fallback to timestamp-based number
                var packageCount = await DbContext.Packages.CountAsync();
                package.PackageNumber = $"PKG-{DateTime.Now:yyyyMMdd}-{(packageCount + 1):D4}";
                packageNumberGenerated = true;
            }
        }
    }

    private async Task SavePackage()
    {
        if (!isEditMode || package == null) return;

        try
        {
            package.LastModified = DateTime.UtcNow;

            if (package.Id == 0)
            {
                // Ensure package number is generated before saving
                if (!packageNumberGenerated)
                {
                    await GeneratePackageNumber();
                }

                package.CreatedDate = DateTime.UtcNow;
                DbContext.Packages.Add(package);
            }
            else
            {
                DbContext.Packages.Update(package);
            }

            await DbContext.SaveChangesAsync();

            if (Id == 0)
            {
                // Navigate to the new package
                Navigation.NavigateTo($"/packages/{package.Id}", replace: true);
            }
            else
            {
                // Reload to show saved data
                await LoadPackage();
                isEditMode = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving package: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task DeletePackage()
    {
        if (package == null || package.Id == 0) return;

        try
        {
            package.IsDeleted = true;
            package.LastModified = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();
            Navigation.NavigateTo("/packages");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting package: {ex.Message}";
            StateHasChanged();
        }
    }

    private void EditPackage()
    {
        isEditMode = true;
        StateHasChanged();
    }

    private async Task CancelEdit()
    {
        if (Id == 0)
        {
            Navigation.NavigateTo("/packages");
        }
        else
        {
            await LoadPackage();
            isEditMode = false;
            StateHasChanged();
        }
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        return new ToolbarActionGroup
        {
            PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
            {
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "New",
                    Label = "New",
                    Icon = "fas fa-plus",
                    ActionFunc = () => { Navigation.NavigateTo("/packages/0"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "Create new package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = isEditMode ? "Save" : "Edit",
                    Label = isEditMode ? "Save" : "Edit",
                    Icon = isEditMode ? "fas fa-save" : "fas fa-edit",
                    ActionFunc = () => { if (isEditMode) return SavePackage(); else { EditPackage(); return Task.CompletedTask; } },
                    IsDisabled = package == null,
                    Tooltip = isEditMode ? "Save changes" : "Edit package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Delete",
                    Label = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = () => DeletePackage(),
                    IsDisabled = package == null || package.Id == 0,
                    Tooltip = "Delete package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Cancel",
                    Label = "Cancel",
                    Icon = "fas fa-times",
                    ActionFunc = () => CancelEdit(),
                    IsDisabled = !isEditMode,
                    Tooltip = "Cancel changes"
                }
            },
            MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
            {
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Duplicate",
                    Icon = "fas fa-copy",
                    ActionFunc = () => Task.CompletedTask,
                    IsDisabled = package == null || package.Id == 0,
                    Tooltip = "Duplicate this package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Export",
                    Icon = "fas fa-file-export",
                    ActionFunc = () => Task.CompletedTask,
                    IsDisabled = package == null,
                    Tooltip = "Export package details"
                }
            },
            RelatedActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
            {
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View All Packages",
                    Icon = "fas fa-box",
                    ActionFunc = () => { Navigation.NavigateTo("/packages"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "View all packages"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View Takeoffs",
                    Icon = "fas fa-ruler",
                    ActionFunc = () => { Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "View all takeoffs"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "SharePoint Takeoff Files",
                    Icon = "fas fa-folder-open",
                    ActionFunc = () => { Navigation.NavigateTo($"/packages/{Id}/sharepoint-files"); return Task.CompletedTask; },
                    IsDisabled = Id == 0,
                    Tooltip = "View SharePoint takeoff files for this package"
                }
            }
        };
    }

    private void NavigateToTakeoffs()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/takeoffs?packageId={Id}");
    }

    private bool IsSectionExpanded(string section)
    {
        return sectionStates.ContainsKey(section) && sectionStates[section];
    }

    private void ToggleSection(string section)
    {
        if (sectionStates.ContainsKey(section))
        {
            sectionStates[section] = !sectionStates[section];
        }
        else
        {
            sectionStates[section] = true;
        }
        StateHasChanged();
    }

    public void Dispose()
    {
        BreadcrumbService.OnBreadcrumbChanged -= StateHasChanged;
    }
}