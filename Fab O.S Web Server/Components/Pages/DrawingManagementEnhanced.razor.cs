using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Components.Pages;

public partial class DrawingManagementEnhanced : ComponentBase
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<DrawingManagementEnhanced> Logger { get; set; } = default!;

    private List<TraceDrawing> drawings = new();
    private List<TraceDrawing> filteredDrawings = new();
    private List<TraceDrawing> selectedDrawings = new();
    private TraceDrawing? editingDrawing;
    private bool isUploading = false;
    private bool showBatchActions = false;
    private string searchTerm = "";
    private string filterStatus = "all";
    private string filterProject = "all";
    private string viewMode = "grid";
    private Dictionary<string, bool> expandedGroups = new();
    private List<Project> projects = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadProjects();
        await LoadDrawings();
    }

    private async Task LoadProjects()
    {
        try
        {
            projects = await DbContext.Projects
                .OrderBy(p => p.ProjectName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading projects");
        }
    }

    private async Task LoadDrawings()
    {
        try
        {
            drawings = await DbContext.TraceDrawings
                .Include(d => d.Project)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading drawings");
        }
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        isUploading = true;
        try
        {
            var uploadTasks = new List<Task>();
            
            foreach (var file in e.GetMultipleFiles(10)) // Allow up to 10 files
            {
                if (file.Size > 100 * 1024 * 1024) // 100MB limit
                {
                    Logger.LogWarning($"File {file.Name} exceeds size limit");
                    continue;
                }

                uploadTasks.Add(ProcessFileUpload(file));
            }

            await Task.WhenAll(uploadTasks);
            await DbContext.SaveChangesAsync();
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading files");
        }
        finally
        {
            isUploading = false;
        }
    }

    private async Task ProcessFileUpload(IBrowserFile file)
    {
        var drawing = new TraceDrawing
        {
            CompanyId = 1, // TODO: Get from user context
            ProjectId = 1, // TODO: Get from selected project
            FileName = file.Name,
            FileType = file.ContentType,
            ProcessingStatus = "Processing",
            UploadDate = DateTime.UtcNow,
            UploadedBy = 1, // TODO: Get from user context
            BlobUrl = "" // Will be set after upload
        };

        // Extract drawing metadata if PDF
        if (file.ContentType == "application/pdf")
        {
            drawing.PageCount = await GetPdfPageCount(file);
        }

        // Save file content - would need to upload to blob storage
        using var stream = file.OpenReadStream(100 * 1024 * 1024);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        // In real implementation, upload to blob storage and set BlobUrl
        drawing.BlobUrl = $"temp://file_{Guid.NewGuid()}";

        DbContext.TraceDrawings.Add(drawing);
    }

    private async Task<int> GetPdfPageCount(IBrowserFile file)
    {
        // Placeholder for PDF page count extraction
        return 1;
    }

    private void ToggleDrawingSelection(TraceDrawing drawing)
    {
        if (selectedDrawings.Contains(drawing))
        {
            selectedDrawings.Remove(drawing);
        }
        else
        {
            selectedDrawings.Add(drawing);
        }

        showBatchActions = selectedDrawings.Any();
    }

    private void SelectAllDrawings()
    {
        selectedDrawings.Clear();
        selectedDrawings.AddRange(filteredDrawings);
        showBatchActions = true;
    }

    private void DeselectAllDrawings()
    {
        selectedDrawings.Clear();
        showBatchActions = false;
    }

    private async Task BatchDeleteDrawings()
    {
        try
        {
            DbContext.TraceDrawings.RemoveRange(selectedDrawings);
            await DbContext.SaveChangesAsync();
            selectedDrawings.Clear();
            showBatchActions = false;
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting drawings");
        }
    }

    private async Task BatchUpdateStatus(string newStatus)
    {
        try
        {
            foreach (var drawing in selectedDrawings)
            {
                drawing.ProcessingStatus = newStatus;
                // UpdatedDate not available in TraceDrawing
            }

            await DbContext.SaveChangesAsync();
            selectedDrawings.Clear();
            showBatchActions = false;
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating drawing status");
        }
    }

    private void StartEditingDrawing(TraceDrawing drawing)
    {
        editingDrawing = drawing;
    }

    private async Task SaveDrawingChanges()
    {
        if (editingDrawing != null)
        {
            try
            {
                // UpdatedDate not available in TraceDrawing
                await DbContext.SaveChangesAsync();
                editingDrawing = null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving drawing changes");
            }
        }
    }

    private void CancelEditingDrawing()
    {
        editingDrawing = null;
        StateHasChanged();
    }

    private async Task DeleteDrawing(TraceDrawing drawing)
    {
        try
        {
            DbContext.TraceDrawings.Remove(drawing);
            await DbContext.SaveChangesAsync();
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting drawing");
        }
    }

    private void ApplyFilters()
    {
        filteredDrawings = drawings;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredDrawings = filteredDrawings
                .Where(d => d.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (d.DrawingNumber?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        if (filterStatus != "all")
        {
            filteredDrawings = filteredDrawings
                .Where(d => d.Status?.ToLower() == filterStatus.ToLower())
                .ToList();
        }

        if (filterProject != "all")
        {
            filteredDrawings = filteredDrawings
                .Where(d => d.ProjectId.ToString() == filterProject)
                .ToList();
        }
    }

    private void SearchDrawings(string term)
    {
        searchTerm = term;
        ApplyFilters();
    }

    private void FilterByStatus(string status)
    {
        filterStatus = status;
        ApplyFilters();
    }

    private void FilterByProject(string projectId)
    {
        filterProject = projectId;
        ApplyFilters();
    }

    private void ChangeViewMode(string mode)
    {
        viewMode = mode;
    }

    private void ToggleGroup(string groupName)
    {
        if (expandedGroups.ContainsKey(groupName))
        {
            expandedGroups[groupName] = !expandedGroups[groupName];
        }
        else
        {
            expandedGroups[groupName] = true;
        }
    }

    private void OpenDrawingViewer(TraceDrawing drawing)
    {
        Navigation.NavigateTo($"/drawing-viewer/{drawing.Id}");
    }

    private void OpenDrawingComparison(TraceDrawing drawing1, TraceDrawing drawing2)
    {
        Navigation.NavigateTo($"/drawing-compare/{drawing1.Id}/{drawing2.Id}");
    }

    private async Task CreateRevision(TraceDrawing drawing)
    {
        try
        {
            // Revision tracking not yet implemented
            // TODO: Add revision support when entity model is updated
            await DbContext.SaveChangesAsync();
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating revision");
        }
    }

    private string GetNextRevisionNumber(TraceDrawing drawing)
    {
        // Revision numbering logic
        return "A"; // Default revision
    }

    // Additional missing methods and properties
    public enum UploadStep
    {
        FileSelection,
        TraceSelection,
        Settings,
        Review,
        OcrProcessing,
        OcrReview,
        Importing,
        Processing,
        Complete
    }

    private UploadStep currentStep = UploadStep.FileSelection;
    private bool showUploadModal = false;
    private string currentTraceName = "";
    private string newTraceName = "";
    private List<IBrowserFile> selectedFiles = new();
    private int uploadProgress = 0;
    private bool ocrEnabled = false;
    private string selectedFileName = "";
    private OcrResult? ocrResult;
    private DrawingSettings? settingsModel;
    private IBrowserFile? selectedFile;

    public class OcrResult
    {
        public string Text { get; set; } = "";
        public List<string> DetectedElements { get; set; } = new();
        public string Status { get; set; } = "";
        public double Confidence { get; set; }
        public string DrawingNumber { get; set; } = "";
        public string DrawingTitle { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string Scale { get; set; } = "";
        public string Date { get; set; } = "";
        public string Revision { get; set; } = "";
        public string DrawnBy { get; set; } = "";
        public string CheckedBy { get; set; } = "";
        public string ApprovedBy { get; set; } = "";
        public string Discipline { get; set; } = "";
        public string SheetNumber { get; set; } = "";
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public List<string> ExtractedLayers { get; set; } = new();
        public List<string> ExtractedBlocks { get; set; } = new();
        public Dictionary<string, string> ExtractedMetadata { get; set; } = new();
        public bool IsProcessing { get; set; }
        public string ProcessingMessage { get; set; } = "";
        public BoundingBox? TitleblockBounds { get; set; }
    }

    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DrawingSettings
    {
        public string DefaultScale { get; set; } = "1:100";
        public string DefaultUnit { get; set; } = "mm";
        public bool AutoProcess { get; set; } = false;
        public bool ExtractText { get; set; } = true;
    }

    private void StartUpload()
    {
        showUploadModal = true;
        currentStep = UploadStep.FileSelection;
        selectedFiles.Clear();
        StateHasChanged();
    }

    private void CloseUploadModal()
    {
        showUploadModal = false;
        currentStep = UploadStep.FileSelection;
        selectedFiles.Clear();
        StateHasChanged();
    }

    private void BackToFileSelection()
    {
        currentStep = UploadStep.FileSelection;
        StateHasChanged();
    }

    private void HandleFileSelection(InputFileChangeEventArgs e)
    {
        selectedFiles = e.GetMultipleFiles(10).ToList();
        if (selectedFiles.Any())
        {
            selectedFileName = selectedFiles.First().Name;
            currentStep = UploadStep.TraceSelection;
        }
        StateHasChanged();
    }

    private async Task HandleFileDrop(InputFileChangeEventArgs e)
    {
        HandleFileSelection(e);
        await Task.CompletedTask;
    }

    private void CreateNewTrace()
    {
        currentTraceName = newTraceName;
        currentStep = UploadStep.Settings;
        StateHasChanged();
    }

    private void ViewDrawing(TraceDrawing drawing)
    {
        Navigation.NavigateTo($"/drawing-viewer/{drawing.Id}");
    }

    private void ImportDrawing()
    {
        // Import the first selected drawing
        if (selectedDrawings.Any())
        {
            ImportDrawing(selectedDrawings.First());
        }
    }

    private void ImportDrawing(TraceDrawing drawing)
    {
        Console.WriteLine($"Import drawing: {drawing.FileName}");
        // Implement import logic
    }

    private void StartTakeoff(TraceDrawing drawing)
    {
        Navigation.NavigateTo($"/takeoff/{drawing.Id}");
    }

    private async Task ProcessWithOcr()
    {
        // Process the first selected drawing
        if (selectedDrawings.Any())
        {
            await ProcessWithOcr(selectedDrawings.First());
        }
    }

    private async Task ProcessWithOcr(TraceDrawing drawing)
    {
        try
        {
            drawing.ProcessingStatus = "Processing";
            await DbContext.SaveChangesAsync();

            // Simulate OCR processing
            await Task.Delay(2000);

            drawing.ProcessingStatus = "Completed";
            await DbContext.SaveChangesAsync();
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing with OCR");
        }
    }

    private string FormatFileSize(long? bytes)
    {
        if (bytes == null) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes.Value;
        while (size >= 1024 && i < sizes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:0.##} {sizes[i]}";
    }

    private string GetStatusBadgeColor(string? status)
    {
        return status?.ToLower() switch
        {
            "completed" => "success",
            "processing" => "warning",
            "failed" => "danger",
            "pending" => "info",
            _ => "secondary"
        };
    }

    private string keyframes => @"
        @keyframes slideIn {
            from { transform: translateY(-100%); opacity: 0; }
            to { transform: translateY(0); opacity: 1; }
        }";
}