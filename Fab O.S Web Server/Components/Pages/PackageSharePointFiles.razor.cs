using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Security.Claims;
using FabOS.WebServer.Components.Shared;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.Columns;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Models.CloudStorage;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Services.Implementations.CloudStorage;

namespace FabOS.WebServer.Components.Pages;

public partial class PackageSharePointFiles : ComponentBase, IToolbarActionProvider, IDisposable
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private ISharePointService SharePointService { get; set; } = default!;
    [Inject] private CloudStorageProviderFactory CloudStorageFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<PackageSharePointFiles> Logger { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int PackageId { get; set; }

    private Package? package;
    private List<PackageDrawing> packageDrawings = new();
    private bool isLoading = true;
    private bool sharePointConfigured = false;
    private bool sharePointConnected = false;
    private string sharePointError = "";
    private bool folderStructureExists = false;
    private bool isCreatingFolderStructure = false;
    private int currentUserId = 0;
    private string searchTerm = "";
    private string currentFolderPath = "";

    // Modal state
    private bool showUploadModal = false;
    private bool showTakeoffModal = false;
    private int selectedDrawingId = 0;
    private FileUploadModal? uploadModal;

    // File and folder lists
    private List<SharePointFileInfo> allFiles = new();
    private List<SharePointFileInfo> filteredFiles = new();
    private List<SharePointFolderInfo> allFolders = new();
    private List<SharePointFolderInfo> filteredFolders = new();

    // View state
    private GenericViewSwitcher<SharePointFileInfo>.ViewType currentView = GenericViewSwitcher<SharePointFileInfo>.ViewType.Table;

    // Selection tracking
    private List<SharePointFileInfo> selectedTableItems = new();
    private List<SharePointFileInfo> selectedListItems = new();
    private List<SharePointFileInfo> selectedCardItems = new();

    // Table columns
    private List<GenericTableView<SharePointFileInfo>.TableColumn<SharePointFileInfo>> tableColumns = new();

    // Column management
    private List<ColumnDefinition> columnDefinitions = new();
    private ViewState currentViewState = new();
    private bool hasUnsavedChanges = false;
    private bool hasCustomColumnConfig = false;
    private List<ColumnDefinition> managedColumns = new();

    protected override async Task OnInitializedAsync()
    {
        // Get current user ID
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst("user_id") ?? user.FindFirst("UserId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                currentUserId = userId;
            }
        }

        InitializeColumnDefinitions();
        await InitializeTableColumns();
        await LoadPackage();
        await CheckSharePointStatus();
        await LoadPackageDrawings();

        if (package != null && sharePointConnected)
        {
            await InitializeFolderPath();
            await CheckFolderStructureExists();
        }
    }


    private void InitializeColumnDefinitions()
    {
        columnDefinitions = new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                Id = "file-name",
                PropertyName = "Name",
                DisplayName = "Name",
                Order = 0,
                IsVisible = true,
                IsRequired = true
            },
            new ColumnDefinition
            {
                Id = "file-type",
                PropertyName = "Type",
                DisplayName = "Type",
                Order = 1,
                IsVisible = true
            },
            new ColumnDefinition
            {
                Id = "file-size",
                PropertyName = "Size",
                DisplayName = "Size",
                Order = 2,
                IsVisible = true
            },
            new ColumnDefinition
            {
                Id = "file-modified",
                PropertyName = "LastModifiedDateTime",
                DisplayName = "Modified",
                Order = 3,
                IsVisible = true
            }
        };
        managedColumns = columnDefinitions;
    }

    private async Task InitializeTableColumns()
    {
        await UpdateTableColumns();
    }

    private async Task UpdateTableColumns()
    {
        tableColumns = columnDefinitions
            .Where(c => c.IsVisible)
            .OrderBy(c => c.Order)
            .Select(c => CreateTableColumn(c))
            .Where(col => col != null)
            .ToList()!;

        await Task.CompletedTask;
    }

    private GenericTableView<SharePointFileInfo>.TableColumn<SharePointFileInfo>? CreateTableColumn(ColumnDefinition columnDef)
    {
        return columnDef.PropertyName switch
        {
            "Name" => new GenericTableView<SharePointFileInfo>.TableColumn<SharePointFileInfo>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.Name,
                CssClass = "text-start",
                PropertyName = columnDef.PropertyName
            },
            "Type" => new GenericTableView<SharePointFileInfo>.TableColumn<SharePointFileInfo>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => GetFileType(item),
                CssClass = "text-center",
                PropertyName = columnDef.PropertyName
            },
            "Size" => new GenericTableView<SharePointFileInfo>.TableColumn<SharePointFileInfo>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => FormatFileSize(item.Size),
                CssClass = "text-end",
                PropertyName = columnDef.PropertyName
            },
            "LastModifiedDateTime" => new GenericTableView<SharePointFileInfo>.TableColumn<SharePointFileInfo>
            {
                Header = columnDef.DisplayName,
                ValueSelector = item => item.LastModifiedDateTime.ToString("MMM dd, yyyy"),
                CssClass = "text-center",
                PropertyName = columnDef.PropertyName
            },
            _ => null
        };
    }

    private async Task LoadPackage()
    {
        try
        {
            package = await DbContext.Packages
                .Include(p => p.Revision)
                    .ThenInclude(r => r!.Takeoff)
                .FirstOrDefaultAsync(p => p.Id == PackageId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading package");
        }
    }

    private async Task CheckSharePointStatus()
    {
        try
        {
            var status = await SharePointService.GetConnectionStatusAsync();
            sharePointConfigured = status.IsConfigured;
            sharePointConnected = status.IsConnected;

            if (!status.IsConnected && !string.IsNullOrEmpty(status.ErrorMessage))
            {
                sharePointError = status.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            sharePointConfigured = false;
            sharePointConnected = false;
            sharePointError = ex.Message;
        }
    }

    private async Task LoadPackageDrawings()
    {
        try
        {
            if (package != null)
            {
                packageDrawings = await DbContext.PackageDrawings
                    .Where(pd => pd.PackageId == PackageId && pd.IsActive)
                    .OrderByDescending(pd => pd.UploadedDate)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading package drawings");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task InitializeFolderPath()
    {
        if (package?.Revision?.Takeoff == null) return;

        currentFolderPath = await SharePointService.GetPackageFolderPathAsync(
            package.Revision.Takeoff.TakeoffNumber,
            package.Revision.RevisionCode,
            package.PackageNumber);
    }

    private async Task CheckFolderStructureExists()
    {
        try
        {
            if (package?.Revision?.Takeoff == null)
            {
                folderStructureExists = false;
                return;
            }

            folderStructureExists = await SharePointService.PackageFolderExistsAsync(
                package.Revision.Takeoff.TakeoffNumber,
                package.Revision.RevisionCode,
                package.PackageNumber);

            if (folderStructureExists)
            {
                await RefreshFolderContents();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking folder structure");
            folderStructureExists = false;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task CreateFolderStructure()
    {
        if (package?.Revision?.Takeoff == null) return;

        isCreatingFolderStructure = true;
        StateHasChanged();

        try
        {
            await SharePointService.EnsurePackageFolderExistsAsync(
                package.Revision.Takeoff.TakeoffNumber,
                package.Revision.RevisionCode,
                package.PackageNumber);

            currentFolderPath = await SharePointService.GetPackageFolderPathAsync(
                package.Revision.Takeoff.TakeoffNumber,
                package.Revision.RevisionCode,
                package.PackageNumber);

            folderStructureExists = true;
            await RefreshFolderContents();

            await JS.InvokeVoidAsync("alert", "Folder structure created successfully!");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating folder structure");
            await JS.InvokeVoidAsync("alert", $"Failed to create folder structure: {ex.Message}");
        }
        finally
        {
            isCreatingFolderStructure = false;
            StateHasChanged();
        }
    }

    private async Task RefreshFolderContents()
    {
        try
        {
            if (string.IsNullOrEmpty(currentFolderPath)) return;

            var contents = await SharePointService.GetFolderContentsAsync(currentFolderPath);
            allFolders = contents.Folders ?? new List<SharePointFolderInfo>();
            allFiles = contents.Files ?? new List<SharePointFileInfo>();

            FilterItems();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading folder contents");
        }
    }

    private void FilterItems()
    {
        var files = allFiles.AsEnumerable();
        var folders = allFolders.AsEnumerable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            files = files.Where(f => f.Name.ToLower().Contains(searchLower));
            folders = folders.Where(f => f.Name.ToLower().Contains(searchLower));
        }

        filteredFiles = files.ToList();
        filteredFolders = folders.ToList();
    }

    private void OnSearchChanged(string newSearchTerm)
    {
        searchTerm = newSearchTerm;
        FilterItems();
        StateHasChanged();
    }

    private void OnViewChanged(GenericViewSwitcher<SharePointFileInfo>.ViewType newView)
    {
        currentView = newView;
    }

    private void HandleFileClick(SharePointFileInfo file)
    {
        // Single click - could be used for selection
    }

    private async Task HandleFileDoubleClick(SharePointFileInfo file)
    {
        // Find corresponding PackageDrawing and open in viewer
        var drawing = packageDrawings.FirstOrDefault(d => d.SharePointItemId == file.Id);
        if (drawing != null)
        {
            selectedDrawingId = drawing.Id;
            showTakeoffModal = true;
        }
        else
        {
            await JS.InvokeVoidAsync("alert", "File not found in database. Please refresh and try again.");
        }
    }

    private async Task NavigateToFolder(SharePointFolderInfo folder)
    {
        var folderPath = string.IsNullOrEmpty(currentFolderPath)
            ? folder.Path
            : $"{currentFolderPath}/{folder.Name}";

        currentFolderPath = folderPath;
        await RefreshFolderContents();
        StateHasChanged();
    }

    private void HandleTableSelectionChanged(List<SharePointFileInfo> selected)
    {
        selectedTableItems = selected;
        StateHasChanged();
    }

    private void HandleListSelectionChanged(List<SharePointFileInfo> selected)
    {
        selectedListItems = selected;
        StateHasChanged();
    }

    private void HandleCardSelectionChanged(List<SharePointFileInfo> selected)
    {
        selectedCardItems = selected;
        StateHasChanged();
    }

    private List<SharePointFileInfo> GetSelectedFiles()
    {
        return currentView switch
        {
            GenericViewSwitcher<SharePointFileInfo>.ViewType.Table => selectedTableItems,
            GenericViewSwitcher<SharePointFileInfo>.ViewType.List => selectedListItems,
            GenericViewSwitcher<SharePointFileInfo>.ViewType.Card => selectedCardItems,
            _ => new List<SharePointFileInfo>()
        };
    }

    // Toolbar Actions
    public ToolbarActionGroup GetActions()
    {
        var selected = GetSelectedFiles();
        var hasSelection = selected.Any();

        return new ToolbarActionGroup
        {
            PrimaryActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "Back to Package",
                    Icon = "fas fa-arrow-left",
                    ActionFunc = () => { NavigateBack(); return Task.CompletedTask; },
                    Style = ToolbarActionStyle.Secondary
                }
            },
            MenuActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "Upload Files",
                    Icon = "fas fa-upload",
                    ActionFunc = () => { UploadFiles(); return Task.CompletedTask; },
                    IsDisabled = !sharePointConnected
                },
                new ToolbarAction
                {
                    Text = "Create Folder",
                    Icon = "fas fa-folder-plus",
                    ActionFunc = async () => await CreateFolderDialog(),
                    IsDisabled = !sharePointConnected
                },
                new ToolbarAction
                {
                    Text = "Refresh",
                    Icon = "fas fa-sync",
                    ActionFunc = async () => await RefreshFolderContents()
                },
                new ToolbarAction
                {
                    Text = $"Delete Selected ({selected.Count})",
                    Icon = "fas fa-trash",
                    ActionFunc = async () => await DeleteSelectedFiles(),
                    IsDisabled = !hasSelection,
                    Style = ToolbarActionStyle.Danger,
                    IsVisible = hasSelection
                }
            },
            RelatedActions = new List<ToolbarAction>
            {
                new ToolbarAction
                {
                    Text = "SharePoint Settings",
                    Icon = "fas fa-cog",
                    ActionFunc = () => { NavigateToSharePointSetup(); return Task.CompletedTask; }
                },
                new ToolbarAction
                {
                    Text = "View in SharePoint",
                    Icon = "fas fa-external-link-alt",
                    ActionFunc = async () => await OpenInSharePoint(),
                    IsDisabled = !sharePointConfigured
                }
            }
        };
    }

    private void UploadFiles()
    {
        showUploadModal = true;
        StateHasChanged();
    }

    private async Task CreateFolderDialog()
    {
        var folderName = await JS.InvokeAsync<string>("prompt", "Enter folder name:");
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            await CreateFolder(folderName);
        }
    }

    private async Task CreateFolder(string folderName)
    {
        try
        {
            await SharePointService.CreateFolderAsync(currentFolderPath, folderName);
            await RefreshFolderContents();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating folder");
            await JS.InvokeVoidAsync("alert", $"Failed to create folder: {ex.Message}");
        }
    }

    private async Task DeleteSelectedFiles()
    {
        var selected = GetSelectedFiles();
        if (!selected.Any()) return;

        var confirmed = await JS.InvokeAsync<bool>("confirm", $"Delete {selected.Count} selected file(s)?");
        if (!confirmed) return;

        try
        {
            var itemIds = selected.Select(f => f.Id).ToList();
            await SharePointService.DeleteMultipleFilesAsync(itemIds);

            selectedTableItems.Clear();
            selectedListItems.Clear();
            selectedCardItems.Clear();

            await RefreshFolderContents();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting files");
            await JS.InvokeVoidAsync("alert", $"Failed to delete files: {ex.Message}");
        }
    }

    private async Task HandleUploadRequest(FileUploadModal.UploadFileRequest request)
    {
        if (package == null) return;

        try
        {
            var drawingNumber = Path.GetFileNameWithoutExtension(request.File.Name);

            if (request.FileData == null)
            {
                throw new InvalidOperationException("File data is not available. Please try again.");
            }

            var memoryStream = new MemoryStream(request.FileData);
            memoryStream.Position = 0;

            var packageWithRevision = await DbContext.Packages
                .Include(p => p.Revision)
                    .ThenInclude(r => r!.Takeoff)
                .FirstOrDefaultAsync(p => p.Id == PackageId);

            if (packageWithRevision?.Revision?.Takeoff == null)
            {
                throw new InvalidOperationException("Package is not associated with a takeoff");
            }

            var folderPath = await SharePointService.GetPackageFolderPathAsync(
                packageWithRevision.Revision.Takeoff.TakeoffNumber,
                packageWithRevision.Revision.RevisionCode,
                packageWithRevision.PackageNumber);

            // ====== MIGRATED TO CLOUD STORAGE ARCHITECTURE ======
            Logger.LogInformation("[PackageSharePointFiles] Using CloudStorage architecture for upload");

            // Get the cloud storage provider (defaults to SharePoint)
            var storageProvider = CloudStorageFactory.GetDefaultProvider();

            // Create upload request using the new CloudStorage models
            var cloudUploadRequest = new CloudFileUploadRequest
            {
                FolderPath = folderPath,
                FileName = request.File.Name,
                Content = memoryStream,
                ContentType = "application/pdf",
                Metadata = new Dictionary<string, string>
                {
                    { "PackageId", PackageId.ToString() },
                    { "DrawingNumber", drawingNumber },
                    { "UploadedBy", currentUserId.ToString() }
                }
            };

            // Upload using the cloud storage provider
            var uploadResult = await storageProvider.UploadFileAsync(cloudUploadRequest);

            Logger.LogInformation("[PackageSharePointFiles] File uploaded successfully via {Provider}, FileId: {FileId}",
                storageProvider.ProviderName, uploadResult.FileId);

            if (currentUserId == 0)
            {
                throw new InvalidOperationException("User is not authenticated. Please log in and try again.");
            }

            // Create PackageDrawing with new cloud storage fields
            var drawing = new PackageDrawing
            {
                PackageId = PackageId,
                DrawingNumber = drawingNumber,
                DrawingTitle = "",
                // Legacy SharePoint fields (maintained for backward compatibility)
                SharePointItemId = uploadResult.FileId,
                SharePointUrl = uploadResult.WebUrl,
                // New cloud storage fields
                StorageProvider = storageProvider.ProviderName,
                ProviderFileId = uploadResult.FileId,
                ProviderMetadata = System.Text.Json.JsonSerializer.Serialize(uploadResult.ProviderMetadata),
                FileType = "PDF",
                FileSize = request.File.Size,
                UploadedDate = DateTime.UtcNow,
                UploadedBy = currentUserId,
                IsActive = true
            };

            DbContext.PackageDrawings.Add(drawing);
            await DbContext.SaveChangesAsync();

            packageDrawings.Insert(0, drawing);
            uploadModal?.SetUploadedDrawingId(drawing.Id);

            await RefreshFolderContents();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading file via cloud storage");
            throw;
        }
    }

    private async Task HandleUploadComplete(FileUploadModal.UploadCompleteResult result)
    {
        var updatedDrawing = await DbContext.PackageDrawings
            .FirstOrDefaultAsync(d => d.Id == result.DrawingId);

        if (updatedDrawing != null)
        {
            var existingIndex = packageDrawings.FindIndex(d => d.Id == result.DrawingId);
            if (existingIndex >= 0)
            {
                packageDrawings[existingIndex] = updatedDrawing;
            }
        }

        showUploadModal = false;
        await RefreshFolderContents();
        StateHasChanged();
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo($"/{TenantSlug}/trace/packages/{PackageId}");
    }

    private void NavigateToSharePointSetup()
    {
        Navigation.NavigateTo("/admin/sharepoint-setup");
    }

    private async Task RetryConnection()
    {
        await CheckSharePointStatus();
        StateHasChanged();
    }

    private async Task OpenInSharePoint()
    {
        await JS.InvokeVoidAsync("alert", "Open in SharePoint functionality to be implemented");
    }

    // Helper methods
    private string GetFileIconClass(SharePointFileInfo file)
    {
        var extension = Path.GetExtension(file.Name).ToLower();
        return extension switch
        {
            ".pdf" => "fas fa-file-pdf text-danger",
            ".doc" or ".docx" => "fas fa-file-word text-primary",
            ".xls" or ".xlsx" => "fas fa-file-excel text-success",
            ".ppt" or ".pptx" => "fas fa-file-powerpoint text-warning",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "fas fa-file-image text-info",
            ".zip" or ".rar" or ".7z" => "fas fa-file-archive text-secondary",
            ".txt" => "fas fa-file-alt text-muted",
            _ => "fas fa-file text-muted"
        };
    }

    private string GetFileType(SharePointFileInfo file)
    {
        var extension = Path.GetExtension(file.Name).ToUpper();
        return string.IsNullOrEmpty(extension) ? "File" : extension.TrimStart('.');
    }

    private string FormatFileSize(long bytes)
    {
        var sizes = new[] { "B", "KB", "MB", "GB" };
        var order = 0;
        var size = (double)bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    // Modal methods
    private string GetTakeoffModalTitle()
    {
        var drawing = packageDrawings.FirstOrDefault(d => d.Id == selectedDrawingId);
        return drawing != null ? $"{drawing.DrawingNumber} - {drawing.DrawingTitle}" : "PDF Takeoff";
    }

    private void CloseTakeoffModal()
    {
        showTakeoffModal = false;
        selectedDrawingId = 0;
    }

    // Column management methods
    private async Task HandleColumnsChanged(List<ColumnDefinition> columns)
    {
        managedColumns = columns;
        columnDefinitions = columns;
        await UpdateTableColumns();
        hasUnsavedChanges = true;
        hasCustomColumnConfig = true;
        StateHasChanged();
    }

    private void HandleViewLoaded(ViewState viewState)
    {
        hasUnsavedChanges = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
