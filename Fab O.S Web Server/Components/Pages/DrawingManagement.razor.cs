using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Components.Pages;

public partial class DrawingManagement : ComponentBase
{
    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<DrawingManagement> Logger { get; set; } = default!;

    private List<Takeoff> drawings = new();
    private List<Takeoff> filteredDrawings = new();
    private Takeoff? selectedDrawing;
    private bool isUploading = false;
    private string searchTerm = "";
    private string filterStatus = "all";
    private string viewMode = "grid";

    protected override async Task OnInitializedAsync()
    {
        await LoadDrawings();
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
            foreach (var file in e.GetMultipleFiles())
            {
                if (file.Size > 100 * 1024 * 1024) // 100MB limit
                {
                    Logger.LogWarning($"File {file.Name} exceeds size limit");
                    continue;
                }

                var drawing = new Takeoff
                {
                    CompanyId = 1, // TODO: Get from user context
                    ProjectId = null, // Projects are optional - no longer using Project entity
                    FileName = file.Name,
                    FileType = file.ContentType,
                    ProcessingStatus = "Processing",
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = 1 // TODO: Get from user context
                };

                // Save file content - would need to upload to blob storage
                using var stream = file.OpenReadStream(100 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                // In real implementation, upload to blob storage and set BlobUrl
                drawing.BlobUrl = $"temp://file_{Guid.NewGuid()}";

                DbContext.TraceDrawings.Add(drawing);
            }

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

    private void SelectDrawing(Takeoff drawing)
    {
        selectedDrawing = drawing;
    }

    private async Task DeleteDrawing(Takeoff drawing)
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
                .Where(d => d.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (filterStatus != "all")
        {
            filteredDrawings = filteredDrawings
                .Where(d => d.Status?.ToLower() == filterStatus.ToLower())
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

    private void ChangeViewMode(string mode)
    {
        viewMode = mode;
    }

    private void OpenDrawingViewer(Takeoff drawing)
    {
        Navigation.NavigateTo($"/drawing-viewer/{drawing.Id}");
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
    private string selectedFileName = "";
    private bool ocrEnabled = false;
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

    private void ViewDrawing(Takeoff drawing)
    {
        Navigation.NavigateTo($"/drawing-viewer/{drawing.Id}");
    }

    private void ImportDrawing()
    {
        // Import the currently selected drawing
        if (selectedDrawing != null)
        {
            ImportDrawing(selectedDrawing);
        }
    }

    private void ImportDrawing(Takeoff drawing)
    {
        Console.WriteLine($"Import drawing: {drawing.FileName}");
        // Implement import logic
    }

    private void StartTakeoff(Takeoff drawing)
    {
        Navigation.NavigateTo($"/takeoff/{drawing.Id}");
    }

    private async Task ProcessWithOcr()
    {
        // Process the currently selected drawing
        if (selectedDrawing != null)
        {
            await ProcessWithOcr(selectedDrawing);
        }
    }

    private async Task ProcessWithOcr(Takeoff drawing)
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
}