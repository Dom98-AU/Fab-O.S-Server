using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;

namespace FabOS.WebServer.Components.Pages;

public partial class PackageCard : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private FabOS.WebServer.Services.BreadcrumbService BreadcrumbService { get; set; } = default!;

    private Package? package = null;
    private bool isLoading = true;
    private string errorMessage = "";
    private int fileCount = 0;

    // Default to edit mode as requested
    private bool isEditMode = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadPackage();
        var breadcrumbText = Id == 0 ? "Packages / New Package" : $"Packages / {package?.PackageName ?? $"Package #{Id}"}";
        BreadcrumbService.SetBreadcrumb(breadcrumbText);
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
                var packageCount = await DbContext.Packages.CountAsync();
                package = new Package
                {
                    Id = 0,
                    PackageNumber = $"PKG-{DateTime.Now:yyyyMMdd}-{(packageCount + 1):D4}",
                    PackageName = $"New Package",
                    Description = "",
                    Status = "Planning",
                    PackageSource = "Project",
                    EstimatedHours = 0,
                    EstimatedCost = 0,
                    ActualHours = 0,
                    ActualCost = 0,
                    LaborRatePerHour = 150,
                    ProcessingEfficiency = 100,
                    ProjectId = 1,
                    // CompanyId removed - not part of Package model
                    CreatedDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    IsDeleted = false
                };
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
                    // Count SharePoint files
                    fileCount = package.PackageDrawings?.Count(d => d.IsActive) ?? 0;

                    // Set edit mode as default for existing packages too
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

    private async Task SavePackage()
    {
        if (!isEditMode || package == null) return;

        try
        {
            package.LastModified = DateTime.UtcNow;

            if (package.Id == 0)
            {
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
                    Icon = "fas fa-plus",
                    ActionFunc = () => { Navigation.NavigateTo("/packages/0"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "Create new package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = isEditMode ? "Save" : "Edit",
                    Icon = isEditMode ? "fas fa-save" : "fas fa-edit",
                    ActionFunc = () => { if (isEditMode) return SavePackage(); else { EditPackage(); return Task.CompletedTask; } },
                    IsDisabled = package == null,
                    Tooltip = isEditMode ? "Save changes" : "Edit package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    ActionFunc = () => DeletePackage(),
                    IsDisabled = package == null || package.Id == 0,
                    Tooltip = "Delete package"
                },
                new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Cancel",
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
                    ActionFunc = () => { Navigation.NavigateTo("/takeoffs"); return Task.CompletedTask; },
                    IsDisabled = false,
                    Tooltip = "View all takeoffs"
                }
            }
        };
    }

    private void NavigateToSharePointFiles()
    {
        Navigation.NavigateTo($"/packages/{Id}/sharepoint-files");
    }

    public void Dispose()
    {
        BreadcrumbService.OnBreadcrumbChanged -= StateHasChanged;
    }
}