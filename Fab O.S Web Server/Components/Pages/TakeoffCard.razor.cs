using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Components.Shared;

namespace FabOS.WebServer.Components.Pages;

// TakeoffFile class for demo file data
public class TakeoffFile
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BlobUrl { get; set; }
}

public partial class TakeoffCard : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Parameter] public int Id { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISharePointService? SharePointService { get; set; }
    [Inject] private ILogger<TakeoffCard> Logger { get; set; } = default!;
    [Inject] private FabOS.WebServer.Services.BreadcrumbService BreadcrumbService { get; set; } = default!;

    private TraceDrawing? takeoff = null;
    private bool isLoading = true;
    private string errorMessage = "";

    // Customer and Contact selection
    private List<Customer> customers = new();
    private List<CustomerContact> customerContacts = new();
    private Customer? selectedCustomer;
    private CustomerAddress? selectedAddress;
    private CustomerContact? selectedContact;
    private string selectedContactEmail = "";
    private string selectedContactPhone = "";

    // For new takeoffs
    private bool isEditMode = true;

    // Section collapse management
    private Dictionary<string, bool> sectionStates = new Dictionary<string, bool>
    {
        { "general", true }  // General section expanded by default
    };
    private bool takeoffNumberGenerated = false;


    // File list data
    private List<TakeoffFile> takeoffFiles = new();
    private IToolbarActionProvider fileActionProvider = null!;
    private FileActionProvider fileActions = new();
    private bool showFilesModal = false;

    // SharePoint state
    private SharePointConnectionStatus? sharePointStatus;
    private bool isCheckingSharePoint = true;
    private bool folderExists = false;
    private bool isCreatingFolder = false;
    private bool isLoadingFiles = false;
    private string? sharePointError;

    // SharePoint setup form
    private string setupTenantId = string.Empty;
    private string setupClientId = string.Empty;
    private string setupClientSecret = string.Empty;
    private string setupSiteUrl = string.Empty;
    private bool isConfiguringSharePoint = false;

    protected override async Task OnInitializedAsync()
    {
        fileActionProvider = fileActions;
        await LoadData();
        LoadSampleFiles();

        // Set breadcrumb in main layout
        var breadcrumbText = Id == 0 ? "Takeoffs / New Takeoff" : $"Takeoffs / {takeoff?.DrawingNumber ?? $"Takeoff #{Id}"}";
        BreadcrumbService.SetBreadcrumb(breadcrumbText);
    }

    private async Task LoadData()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // Load customers first
            customers = await DbContext.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();


            // Load or create takeoff
            if (Id == 0)
            {
                // Create new takeoff
                takeoff = new TraceDrawing
                {
                    Id = 0,
                    FileName = "New Takeoff.pdf",
                    FileType = "pdf",
                    ProcessingStatus = "Draft",
                    Status = "Planning",
                    UploadedBy = 2,
                    CompanyId = 1,
                    ProjectId = 1,
                    BlobUrl = "https://placeholder.blob.url",
                    CreatedDate = DateTime.UtcNow,
                    UploadDate = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    ScaleUnit = "mm",
                    Scale = 100
                };
                isEditMode = true;
            }
            else
            {
                // Load existing takeoff
                takeoff = await DbContext.TraceDrawings
                    .FirstOrDefaultAsync(t => t.Id == Id);

                if (takeoff == null)
                {
                    errorMessage = $"Takeoff with ID {Id} not found.";
                }
                else if (takeoff.CustomerId != null)
                {
                    // Load customer contacts if customer is selected
                    await LoadCustomerContacts(takeoff.CustomerId.Value);

                    // Load selected contact if exists
                    if (takeoff.ContactId != null)
                    {
                        selectedContact = customerContacts.FirstOrDefault(c => c.Id == takeoff.ContactId);
                        if (selectedContact != null)
                        {
                            selectedContactEmail = selectedContact.Email ?? "";
                            selectedContactPhone = selectedContact.PhoneNumber ?? "";
                        }
                    }

                    // Load selected customer details
                    selectedCustomer = customers.FirstOrDefault(c => c.Id == takeoff.CustomerId.Value);
                    if (selectedCustomer != null)
                    {
                        selectedAddress = await DbContext.CustomerAddresses
                            .FirstOrDefaultAsync(a => a.CustomerId == selectedCustomer.Id && a.IsPrimary);
                    }
                }

                // Set edit mode as default for existing takeoffs
                isEditMode = true;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadCustomerContacts(int customerId)
    {
        customerContacts = await DbContext.CustomerContacts
            .Where(c => c.CustomerId == customerId && c.IsActive)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync();
    }

    private async Task OnCustomerChanged(ChangeEventArgs e)
    {
        if (takeoff == null) return;

        if (int.TryParse(e.Value?.ToString(), out int customerId))
        {
            takeoff.CustomerId = customerId;
            takeoff.ContactId = null; // Reset contact when customer changes
            selectedContact = null;
            selectedContactEmail = "";
            selectedContactPhone = "";

            await LoadCustomerContacts(customerId);

            selectedCustomer = customers.FirstOrDefault(c => c.Id == customerId);
            if (selectedCustomer != null)
            {
                selectedAddress = await DbContext.CustomerAddresses
                    .FirstOrDefaultAsync(a => a.CustomerId == selectedCustomer.Id && a.IsPrimary);
            }
        }
        else
        {
            takeoff.CustomerId = null;
            takeoff.ContactId = null;
            customerContacts.Clear();
            selectedCustomer = null;
            selectedAddress = null;
        }

        StateHasChanged();
    }

    private void OnContactChanged(ChangeEventArgs e)
    {
        if (takeoff == null) return;

        if (int.TryParse(e.Value?.ToString(), out int contactId))
        {
            takeoff.ContactId = contactId;
            selectedContact = customerContacts.FirstOrDefault(c => c.Id == contactId);
            if (selectedContact != null)
            {
                selectedContactEmail = selectedContact.Email ?? "";
                selectedContactPhone = selectedContact.PhoneNumber ?? "";
            }
        }
        else
        {
            takeoff.ContactId = null;
            selectedContact = null;
            selectedContactEmail = "";
            selectedContactPhone = "";
        }
    }

    private async Task GenerateTakeoffNumber()
    {
        if (takeoff != null && !takeoffNumberGenerated && string.IsNullOrEmpty(takeoff.DrawingNumber))
        {
            var lastTakeoff = await DbContext.TraceDrawings
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            int nextNumber = (lastTakeoff?.Id ?? 0) + 1;
            takeoff.DrawingNumber = $"TO-{DateTime.Now.Year}-{nextNumber:D5}";
            takeoffNumberGenerated = true;
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



    private int GetProgressPercentage()
    {
        if (takeoff == null) return 0;

        int totalFields = 10;
        int completedFields = 0;

        if (!string.IsNullOrEmpty(takeoff.DrawingNumber)) completedFields++;
        if (!string.IsNullOrEmpty(takeoff.ProjectName)) completedFields++;
        if (takeoff.CustomerId != null) completedFields++;
        if (takeoff.ContactId != null) completedFields++;
        if (!string.IsNullOrEmpty(takeoff.DrawingType)) completedFields++;
        if (!string.IsNullOrEmpty(takeoff.Status)) completedFields++;
        if (!string.IsNullOrEmpty(takeoff.ProjectNumber)) completedFields++;
        if (!string.IsNullOrEmpty(takeoff.Revision)) completedFields++;
        if (!string.IsNullOrEmpty(takeoff.TraceName)) completedFields++;
        if (takeoff.Scale != null && takeoff.Scale > 0) completedFields++;

        return (completedFields * 100) / totalFields;
    }

    private int GetSectionCompletion(string section)
    {
        if (takeoff == null) return 0;

        switch (section)
        {
            case "basic":
                int basicTotal = 4;
                int basicCompleted = 0;
                if (!string.IsNullOrEmpty(takeoff.DrawingNumber)) basicCompleted++;
                if (!string.IsNullOrEmpty(takeoff.ProjectName)) basicCompleted++;
                if (takeoff.CustomerId != null) basicCompleted++;
                if (takeoff.ContactId != null) basicCompleted++;
                return (basicCompleted * 100) / basicTotal;
            default:
                return 0;
        }
    }

    // IToolbarActionProvider implementation
    public FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionGroup GetActions()
    {
        var actionGroup = new FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionGroup();

        if (takeoff != null)
        {
            // Primary Actions (New, Edit/Save, Delete, Cancel)
            if (Id == 0)
            {
                // New takeoff - show Save as primary
                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Save",
                    Icon = "fas fa-save",
                    Action = EventCallback.Factory.Create(this, SaveTakeoff),
                    Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
                });

                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Cancel",
                    Icon = "fas fa-times",
                    Action = EventCallback.Factory.Create(this, CancelEdit)
                });
            }
            else if (!isEditMode)
            {
                // View mode - show Edit button
                // New button - always creates a new takeoff
                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "New",
                    Icon = "fas fa-plus",
                    Action = EventCallback.Factory.Create(this, () => Navigation.NavigateTo("/takeoffs/0"))
                });

                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Edit",
                    Icon = "fas fa-edit",
                    Action = EventCallback.Factory.Create(this, StartEdit)
                });

                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Delete",
                    Icon = "fas fa-trash",
                    Action = EventCallback.Factory.Create(this, DeleteTakeoff),
                    Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Danger
                });
            }
            else
            {
                // Edit mode - show Save and Cancel
                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Save",
                    Icon = "fas fa-save",
                    Action = EventCallback.Factory.Create(this, SaveTakeoff),
                    Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
                });

                actionGroup.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Cancel",
                    Icon = "fas fa-times",
                    Action = EventCallback.Factory.Create(this, CancelEdit)
                });
            }

            // Menu Actions (Actions dropdown)
            if (Id != 0)
            {
                actionGroup.MenuActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View Drawing",
                    Icon = "fas fa-eye",
                    Action = EventCallback.Factory.Create(this, () => ViewDrawing(takeoff))
                });

                actionGroup.MenuActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Manage Drawings",
                    Icon = "fas fa-images",
                    Action = EventCallback.Factory.Create(this, () => ManageDrawings(takeoff))
                });

                actionGroup.MenuActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Start Measuring",
                    Icon = "fas fa-ruler",
                    Action = EventCallback.Factory.Create(this, () => StartMeasuring(takeoff))
                });

                actionGroup.MenuActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "Generate Number",
                    Icon = "fas fa-magic",
                    Action = EventCallback.Factory.Create(this, GenerateTakeoffNumber),
                    IsDisabled = takeoffNumberGenerated || !isEditMode
                });
            }

            // Related Actions (Related dropdown)
            actionGroup.RelatedActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Text = "Takeoff Files",
                Icon = "fas fa-folder-open",
                Action = EventCallback.Factory.Create(this, OpenFilesModal)
            });

            actionGroup.RelatedActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Text = "View All Takeoffs",
                Icon = "fas fa-list",
                Action = EventCallback.Factory.Create(this, () => Navigation.NavigateTo("/takeoffs"))
            });

            actionGroup.RelatedActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Text = "Packages",
                Icon = "fas fa-box",
                Action = EventCallback.Factory.Create(this, () => Navigation.NavigateTo($"/takeoffs/{Id}/packages")),
                Tooltip = "View packages for this takeoff"
            });

            if (takeoff.ProjectId != null)
            {
                actionGroup.RelatedActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View Project",
                    Icon = "fas fa-project-diagram",
                    Action = EventCallback.Factory.Create(this, () => Navigation.NavigateTo($"/projects/{takeoff.ProjectId}"))
                });
            }

            if (takeoff.CustomerId != null)
            {
                actionGroup.RelatedActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
                {
                    Text = "View Customer",
                    Icon = "fas fa-user",
                    Action = EventCallback.Factory.Create(this, () => Navigation.NavigateTo($"/customers/{takeoff.CustomerId}"))
                });
            }
        }

        return actionGroup;
    }

    // Keep this for backward compatibility
    public IEnumerable<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction> GetToolbarActions()
    {
        var actionGroup = GetActions();
        var allActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>();
        allActions.AddRange(actionGroup.PrimaryActions);
        allActions.AddRange(actionGroup.MenuActions);
        allActions.AddRange(actionGroup.RelatedActions);
        return allActions;
    }

    private void StartEdit()
    {
        isEditMode = true;
        StateHasChanged();
    }

    private void NavigateToPackages()
    {
        Navigation.NavigateTo($"/takeoffs/{Id}/packages");
    }

    private async Task SaveTakeoff()
    {
        if (takeoff == null) return;

        try
        {
            if (takeoff.Id == 0)
            {
                DbContext.TraceDrawings.Add(takeoff);
            }
            else
            {
                DbContext.TraceDrawings.Update(takeoff);
            }

            await DbContext.SaveChangesAsync();

            if (takeoff.Id != 0)
            {
                isEditMode = false;
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving: {ex.Message}";
        }
    }

    private async Task CancelEdit()
    {
        if (takeoff?.Id == 0)
        {
            Navigation.NavigateTo("/takeoffs");
        }
        else
        {
            await LoadData();
            isEditMode = false;
        }
    }

    private void ViewDrawing(TraceDrawing drawing)
    {
        if (drawing != null)
        {
            Navigation.NavigateTo($"/pdf-viewer/{drawing.Id}");
        }
    }

    private void ManageDrawings(TraceDrawing drawing)
    {
        if (drawing != null)
        {
            Navigation.NavigateTo($"/takeoffs/{drawing.Id}/drawings");
        }
    }

    private void StartMeasuring(TraceDrawing drawing)
    {
        if (drawing != null)
        {
            Navigation.NavigateTo($"/measure/{drawing.Id}");
        }
    }

    private async Task DeleteTakeoff()
    {
        if (takeoff != null && takeoff.Id != 0)
        {
            // In a real application, you'd want to show a confirmation dialog
            try
            {
                DbContext.TraceDrawings.Remove(takeoff);
                await DbContext.SaveChangesAsync();
                Navigation.NavigateTo("/takeoffs");
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting takeoff: {ex.Message}";
            }
        }
    }

    // Field change handlers for autosave
    private System.Timers.Timer? autosaveTimer;
    private bool hasUnsavedChanges = false;

    private async Task OnFieldChanged()
    {
        if (!isEditMode || takeoff == null) return;

        hasUnsavedChanges = true;

        // Cancel any existing timer
        autosaveTimer?.Stop();
        autosaveTimer?.Dispose();

        // Start a new timer that will save after 1 second of no changes
        autosaveTimer = new System.Timers.Timer(1000);
        autosaveTimer.Elapsed += async (sender, e) =>
        {
            autosaveTimer?.Dispose();
            await InvokeAsync(async () =>
            {
                await AutoSave();
            });
        };
        autosaveTimer.AutoReset = false;
        autosaveTimer.Start();
    }

    private async Task AutoSave()
    {
        if (!hasUnsavedChanges || takeoff == null) return;

        try
        {
            takeoff.LastModified = DateTime.UtcNow;

            if (takeoff.Id == 0)
            {
                DbContext.TraceDrawings.Add(takeoff);
            }
            else
            {
                DbContext.TraceDrawings.Update(takeoff);
            }

            await DbContext.SaveChangesAsync();
            hasUnsavedChanges = false;

            // If this was a new takeoff, update the ID and URL
            if (Id == 0 && takeoff.Id != 0)
            {
                Navigation.NavigateTo($"/takeoffs/{takeoff.Id}", false);
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Autosave failed: {ex.Message}";
            StateHasChanged();
        }
    }

    // File list methods
    private void LoadSampleFiles()
    {
        // Sample file data for demonstration
        takeoffFiles = new List<TakeoffFile>
        {
            new TakeoffFile
            {
                Id = 1,
                FileName = "Floor_Plan_Level_1.pdf",
                FileType = "PDF",
                FileSize = 2548576,
                UploadedDate = DateTime.Now.AddDays(-5),
                UploadedBy = "John Doe",
                Status = "Verified",
                Version = "v2.1",
                Description = "First floor architectural plans"
            },
            new TakeoffFile
            {
                Id = 2,
                FileName = "Structural_Details.dwg",
                FileType = "DWG",
                FileSize = 4128768,
                UploadedDate = DateTime.Now.AddDays(-3),
                UploadedBy = "Jane Smith",
                Status = "In Review",
                Version = "v1.0",
                Description = "Structural engineering drawings"
            },
            new TakeoffFile
            {
                Id = 3,
                FileName = "Site_Plan.pdf",
                FileType = "PDF",
                FileSize = 1876543,
                UploadedDate = DateTime.Now.AddDays(-7),
                UploadedBy = "Mike Johnson",
                Status = "Approved",
                Version = "v3.0",
                Description = "Complete site layout plan"
            },
            new TakeoffFile
            {
                Id = 4,
                FileName = "MEP_Systems.xlsx",
                FileType = "XLSX",
                FileSize = 524288,
                UploadedDate = DateTime.Now.AddDays(-1),
                UploadedBy = "Sarah Wilson",
                Status = "Draft",
                Version = "v1.0",
                Description = "MEP calculations and schedules"
            },
            new TakeoffFile
            {
                Id = 5,
                FileName = "Elevation_North.jpg",
                FileType = "JPG",
                FileSize = 987654,
                UploadedDate = DateTime.Now.AddDays(-10),
                UploadedBy = "Tom Brown",
                Status = "Verified",
                Version = "v2.0",
                Description = "North elevation rendering"
            }
        };
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private bool SearchFile(TakeoffFile file, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return true;

        var term = searchTerm.ToLower();
        return file.FileName.ToLower().Contains(term) ||
               file.FileType.ToLower().Contains(term) ||
               file.Status.ToLower().Contains(term) ||
               file.UploadedBy.ToLower().Contains(term) ||
               (file.Description?.ToLower().Contains(term) ?? false);
    }

    private async Task HandleFileClick(TakeoffFile file)
    {
        // Handle file click - could open preview, download, etc.
        await Task.CompletedTask;
    }

    private async Task HandleFileDoubleClick(TakeoffFile file)
    {
        // Handle file double click - could open in viewer
        if (file.FileType == "PDF" && !string.IsNullOrEmpty(file.BlobUrl))
        {
            Navigation.NavigateTo($"/pdf-viewer/{file.Id}");
        }
        await Task.CompletedTask;
    }

    private async void OpenFilesModal()
    {
        showFilesModal = true;
        StateHasChanged();

        // Check SharePoint status when modal opens
        await CheckSharePointStatus();
    }

    private void CloseFilesModal()
    {
        showFilesModal = false;
        StateHasChanged();
    }

    // SharePoint Methods
    private async Task CheckSharePointStatus()
    {
        if (SharePointService == null)
        {
            Logger.LogWarning("SharePoint service not configured");
            isCheckingSharePoint = false;
            StateHasChanged();
            return;
        }

        isCheckingSharePoint = true;
        sharePointError = null;
        StateHasChanged();

        try
        {
            // Check connection status
            sharePointStatus = await SharePointService.GetConnectionStatusAsync();

            if (sharePointStatus.IsConnected && takeoff != null && !string.IsNullOrEmpty(takeoff.DrawingNumber))
            {
                // Check if folder exists for this takeoff
                folderExists = await SharePointService.TakeoffFolderExistsAsync(takeoff.DrawingNumber);

                if (folderExists)
                {
                    // Load files from SharePoint
                    await LoadSharePointFiles();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking SharePoint status");
            sharePointError = "Failed to check SharePoint status. Please try again.";
        }
        finally
        {
            isCheckingSharePoint = false;
            StateHasChanged();
        }
    }

    private async Task LoadSharePointFiles()
    {
        if (SharePointService == null || takeoff == null || string.IsNullOrEmpty(takeoff.DrawingNumber))
            return;

        isLoadingFiles = true;
        StateHasChanged();

        try
        {
            var files = await SharePointService.GetTakeoffFilesAsync(takeoff.DrawingNumber);

            takeoffFiles = files.Select(f => new TakeoffFile
            {
                Id = 0, // Will be set when saved to database
                FileName = f.Name,
                FileType = GetFileTypeFromName(f.Name),
                FileSize = f.Size,
                UploadedDate = f.CreatedDateTime,
                UploadedBy = f.CreatedBy,
                Status = "Active",
                Version = "v1.0",
                BlobUrl = f.WebUrl,
                Description = $"Uploaded from SharePoint"
            }).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading SharePoint files");
            sharePointError = "Failed to load files from SharePoint.";
        }
        finally
        {
            isLoadingFiles = false;
            StateHasChanged();
        }
    }

    private string GetFileTypeFromName(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToUpper().TrimStart('.');
        return string.IsNullOrEmpty(extension) ? "FILE" : extension;
    }

    private async Task ConfigureSharePoint()
    {
        if (SharePointService == null)
            return;

        isConfiguringSharePoint = true;
        sharePointError = null;
        StateHasChanged();

        try
        {
            var success = await SharePointService.ConfigureSharePointAsync(
                setupTenantId,
                setupClientId,
                setupClientSecret,
                setupSiteUrl);

            if (success)
            {
                await CheckSharePointStatus();
            }
            else
            {
                sharePointError = "Failed to configure SharePoint. Please check your credentials.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error configuring SharePoint");
            sharePointError = $"Configuration error: {ex.Message}";
        }
        finally
        {
            isConfiguringSharePoint = false;
            StateHasChanged();
        }
    }

    private async Task CreateSharePointFolder()
    {
        if (SharePointService == null || takeoff == null || string.IsNullOrEmpty(takeoff.DrawingNumber))
            return;

        isCreatingFolder = true;
        sharePointError = null;
        StateHasChanged();

        try
        {
            var folderInfo = await SharePointService.CreateTakeoffFolderAsync(takeoff.DrawingNumber);

            if (folderInfo != null)
            {
                folderExists = true;

                // TODO: Save SharePoint folder ID to takeoff entity

                await LoadSharePointFiles();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating SharePoint folder");
            sharePointError = "Failed to create SharePoint folder. Please try again.";
        }
        finally
        {
            isCreatingFolder = false;
            StateHasChanged();
        }
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        if (SharePointService == null || !folderExists || takeoff == null || string.IsNullOrEmpty(takeoff.DrawingNumber))
            return;

        var file = e.File;
        if (file == null)
            return;

        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 250 * 1024 * 1024); // 250MB limit
            var uploadedFile = await SharePointService.UploadFileAsync(
                takeoff.DrawingNumber,
                stream,
                file.Name,
                file.ContentType);

            if (uploadedFile != null)
            {
                // Reload files
                await LoadSharePointFiles();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading file");
            sharePointError = $"Failed to upload {file.Name}: {ex.Message}";
            StateHasChanged();
        }
    }

    // File Action Provider inner class
    private class FileActionProvider : IToolbarActionProvider
    {
        public ToolbarActionGroup GetActions()
        {
            var group = new ToolbarActionGroup();

            group.PrimaryActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Text = "Upload",
                Icon = "fas fa-upload",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Upload file")),
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
            });

            group.MenuActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Text = "Download All",
                Icon = "fas fa-download",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Download all files"))
            });

            group.MenuActions.Add(new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Text = "Export List",
                Icon = "fas fa-file-export",
                Action = EventCallback.Factory.Create(this, () => Console.WriteLine("Export file list"))
            });

            return group;
        }
    }

    public void Dispose()
    {
        autosaveTimer?.Dispose();
    }
}