using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Components.Shared.Interfaces;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models;
using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Components.Pages;

public partial class TakeoffDrawingManager : ComponentBase, IToolbarActionProvider
{
    [Parameter] public string? TenantSlug { get; set; }
    [Parameter] public int? TakeoffId { get; set; }

    [Inject] private ApplicationDbContext DbContext { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<Takeoff> drawings = new();
    private List<Takeoff> filteredDrawings = new();
    private Takeoff? selectedTakeoff;
    private string activeTab = "overview";
    private bool isUploading = false;
    private DrawingUploadModel uploadModel = new();
    private DrawingUploadModel editModel = new(); // For edit modal
    private EditContext? editContext;
    private ValidationMessageStore? messageStore;
    private bool showEditModal = false;
    private Takeoff? editingDrawing;

    protected override async Task OnInitializedAsync()
    {
        await LoadDrawings();
        editContext = new EditContext(uploadModel);
        messageStore = new ValidationMessageStore(editContext);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (TakeoffId.HasValue)
        {
            selectedTakeoff = await DbContext.TraceDrawings
                .FirstOrDefaultAsync(t => t.Id == TakeoffId.Value);
        }
        await LoadDrawings();
    }

    private async Task LoadDrawings()
    {
        try
        {
            var query = DbContext.TraceDrawings.AsQueryable();

            if (TakeoffId.HasValue)
            {
                // Filter by takeoff if specified
                query = query.Where(d => d.Id == TakeoffId.Value);
            }

            drawings = await query
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading drawings: {ex.Message}");
        }
    }

    private void SetActiveTab(string tab)
    {
        activeTab = tab;
        StateHasChanged();
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        isUploading = true;
        try
        {
            foreach (var file in e.GetMultipleFiles())
            {
                // Handle file upload logic
                var drawing = new Takeoff
                {
                    CompanyId = 1, // TODO: Get from user context
                    ProjectId = null, // Projects are optional - no longer using Project entity
                    DrawingNumber = uploadModel.DrawingNumber,
                    FileName = file.Name,
                    FileType = file.ContentType,
                    Scale = uploadModel.Scale,
                    ScaleUnit = uploadModel.ScaleUnit,
                    ProcessingStatus = "Pending",
                    UploadDate = DateTime.UtcNow,
                    UploadedBy = 1, // TODO: Get from user context
                    BlobUrl = $"/uploads/{Guid.NewGuid()}_{file.Name}"
                };

                DbContext.TraceDrawings.Add(drawing);
            }

            await DbContext.SaveChangesAsync();
            await LoadDrawings();

            // Reset form
            uploadModel = new DrawingUploadModel();
            editContext = new EditContext(uploadModel);
            messageStore = new ValidationMessageStore(editContext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
        }
        finally
        {
            isUploading = false;
        }
    }

    private async Task DeleteDrawing(int drawingId)
    {
        try
        {
            var drawing = await DbContext.TraceDrawings.FindAsync(drawingId);
            if (drawing != null)
            {
                DbContext.TraceDrawings.Remove(drawing);
                await DbContext.SaveChangesAsync();
                await LoadDrawings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting drawing: {ex.Message}");
        }
    }

    private void ViewDrawing(int drawingId)
    {
        Navigation.NavigateTo($"/takeoffs/drawing/{drawingId}");
    }

    private async Task ProcessDrawing(int drawingId)
    {
        try
        {
            var drawing = await DbContext.TraceDrawings.FindAsync(drawingId);
            if (drawing != null)
            {
                drawing.ProcessingStatus = "In Progress";
                await DbContext.SaveChangesAsync();
                // Trigger OCR/processing logic here
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing drawing: {ex.Message}");
        }
    }

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions()
    {
        var group = new ToolbarActionGroup();
        group.PrimaryActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Upload Drawing",
                Text = "Upload Drawing",
                Icon = "fas fa-cloud-upload-alt",
                Action = EventCallback.Factory.Create(this, () => SetActiveTab("upload")),
                IsDisabled = false,
                Style = FabOS.WebServer.Components.Shared.Interfaces.ToolbarActionStyle.Primary
            }
        };
        group.MenuActions = new List<FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction>
        {
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Process All",
                Text = "Process All",
                Icon = "fas fa-cog",
                Action = EventCallback.Factory.Create(this, ProcessAllDrawings),
                IsDisabled = !drawings.Any(d => d.ProcessingStatus == "Pending")
            },
            new FabOS.WebServer.Components.Shared.Interfaces.ToolbarAction
            {
                Label = "Export List",
                Text = "Export List",
                Icon = "fas fa-file-export",
                Action = EventCallback.Factory.Create(this, () => ExportDrawingsList()),
                IsDisabled = !drawings.Any()
            }
        };
        return group;
    }

    private async Task ProcessAllDrawings()
    {
        var pendingDrawings = drawings.Where(d => d.ProcessingStatus == "Pending").ToList();
        foreach (var drawing in pendingDrawings)
        {
            await ProcessDrawing(drawing.Id);
        }
    }

    private void ExportDrawingsList()
    {
        // Implement export logic
        Console.WriteLine("Export drawings list");
    }

    private void CloseEditModal()
    {
        showEditModal = false;
        editingDrawing = null;
        editModel = new DrawingUploadModel();
        StateHasChanged();
    }

    private async Task SaveDrawingChanges()
    {
        if (editingDrawing == null) return;

        try
        {
            // Update the drawing with edit model values
            editingDrawing.DrawingNumber = editModel.DrawingNumber;
            editingDrawing.FileType = "application/pdf"; // Use a default or from model
            editingDrawing.Scale = editModel.Scale;
            editingDrawing.ScaleUnit = editModel.ScaleUnit;
            // Description doesn't exist in Takeoff

            await DbContext.SaveChangesAsync();
            await LoadDrawings();
            CloseEditModal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving drawing changes: {ex.Message}");
        }
    }

    private void OpenEditModal(Takeoff drawing)
    {
        editingDrawing = drawing;
        editModel = new DrawingUploadModel
        {
            DrawingNumber = drawing.DrawingNumber ?? "",
            DrawingName = drawing.FileName ?? "",
            DrawingType = "Structural",
            Scale = drawing.Scale ?? 1,
            ScaleUnit = drawing.ScaleUnit ?? "mm",
            Description = ""
        };
        showEditModal = true;
        StateHasChanged();
    }

    // Inner class for upload model
    public class DrawingUploadModel
    {
        [Required(ErrorMessage = "Drawing name is required")]
        public string DrawingName { get; set; } = "";

        [Required(ErrorMessage = "Drawing type is required")]
        public string DrawingType { get; set; } = "Structural";

        [Required(ErrorMessage = "Scale is required")]
        [Range(1, 10000, ErrorMessage = "Scale must be between 1 and 10000")]
        public decimal Scale { get; set; } = 1;

        [Required(ErrorMessage = "Scale unit is required")]
        public string ScaleUnit { get; set; } = "mm";

        public string? Description { get; set; }

        public string Discipline { get; set; } = "Structural";

        public string Revision { get; set; } = "A";

        public string ProjectName { get; set; } = "";

        public string ClientName { get; set; } = "";

        public string DrawingNumber { get; set; } = "";

        public string DrawingTitle { get; set; } = "";
    }

    public enum UploadStep
    {
        FileSelection,
        DrawingDetails,
        Settings,
        Review,
        Processing,
        Complete
    }

    private UploadStep currentUploadStep = UploadStep.FileSelection;
    private IBrowserFile? selectedFile;
    private bool showUploadModal = false;
    private int uploadProgress = 0;
    private string statusFilter = "all";
    private string disciplineFilter = "all";
    private string sortBy = "date";
    private string searchTerm = "";
    private string searchQuery = "";
    private bool isLoading = false;
    private DrawingSettings? settingsModel;

    public class DrawingSettings
    {
        public string DefaultScale { get; set; } = "1:100";
        public string DefaultUnit { get; set; } = "mm";
        public bool AutoProcess { get; set; } = false;
        public bool ExtractText { get; set; } = true;
        public string ProcessingMode { get; set; } = "Standard";
        public bool EnableOcr { get; set; } = true;
        public string OutputFormat { get; set; } = "PDF";
        public bool ExtractMetadata { get; set; } = true;
        public string DefaultClientName { get; set; } = "";
        public bool AutoProcessUploads { get; set; } = false;
        public bool GenerateThumbnails { get; set; } = true;
        public string DefaultDiscipline { get; set; } = "Structural";
        public string DefaultProjectName { get; set; } = "";
    }

    private string processingMessage = "";

    private void StartUpload()
    {
        showUploadModal = true;
        currentUploadStep = UploadStep.FileSelection;
        uploadModel = new DrawingUploadModel();
        StateHasChanged();
    }

    private void CloseUploadModal()
    {
        showUploadModal = false;
        selectedFile = null;
        uploadModel = new DrawingUploadModel();
        currentUploadStep = UploadStep.FileSelection;
        StateHasChanged();
    }

    private void NextStep()
    {
        if ((int)currentUploadStep < Enum.GetValues<UploadStep>().Length - 1)
        {
            currentUploadStep = (UploadStep)((int)currentUploadStep + 1);
            StateHasChanged();
        }
    }

    private void PreviousStep()
    {
        if ((int)currentUploadStep > 0)
        {
            currentUploadStep = (UploadStep)((int)currentUploadStep - 1);
            StateHasChanged();
        }
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        StateHasChanged();
    }

    private async Task SubmitUpload()
    {
        if (selectedFile == null) return;

        currentUploadStep = UploadStep.Processing;
        uploadProgress = 0;
        StateHasChanged();

        try
        {
            // Simulate upload progress
            for (int i = 0; i <= 100; i += 10)
            {
                uploadProgress = i;
                StateHasChanged();
                await Task.Delay(100);
            }

            currentUploadStep = UploadStep.Complete;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload error: {ex.Message}");
        }
    }

    private void SaveSettings()
    {
        // Save upload settings
        StateHasChanged();
    }

    private void ResetSettings()
    {
        uploadModel = new DrawingUploadModel();
        StateHasChanged();
    }

    private void EditDrawing(Takeoff drawing)
    {
        editingDrawing = drawing;
        editModel = new DrawingUploadModel
        {
            DrawingName = drawing.DrawingNumber ?? "",
            DrawingType = "Structural",
            Scale = drawing.Scale ?? 1,
            ScaleUnit = drawing.ScaleUnit ?? "mm",
            Description = ""
        };
        showEditModal = true;
        StateHasChanged();
    }

    // Extension properties for Takeoff
    public string? DrawingName { get; set; }
    public string? DrawingType { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public long? FileSize { get; set; }
    public byte[]? FileContent { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    private async Task DuplicateDrawing(Takeoff drawing)
    {
        try
        {
            var duplicate = new Takeoff
            {
                CompanyId = drawing.CompanyId,
                ProjectId = drawing.ProjectId,
                DrawingNumber = $"{drawing.DrawingNumber}_copy",
                FileName = $"Copy of {drawing.FileName}",
                FileType = drawing.FileType,
                BlobUrl = drawing.BlobUrl,
                Scale = drawing.Scale,
                ScaleUnit = drawing.ScaleUnit,
                ProcessingStatus = "Pending",
                UploadDate = DateTime.UtcNow,
                UploadedBy = drawing.UploadedBy
            };

            DbContext.TraceDrawings.Add(duplicate);
            await DbContext.SaveChangesAsync();
            await LoadDrawings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error duplicating drawing: {ex.Message}");
        }
    }

    private void DownloadDrawing(Takeoff drawing)
    {
        // Implement file download
        Console.WriteLine($"Download: {drawing.FileName}");
    }

    private void StartTakeoff(Takeoff drawing)
    {
        Navigation.NavigateTo($"/takeoff/{drawing.Id}");
    }

    private void FilterDrawings(string filter)
    {
        searchTerm = filter;
        StateHasChanged();
    }

    private void ClearFilters()
    {
        searchTerm = "";
        statusFilter = "all";
        disciplineFilter = "all";
        StateHasChanged();
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

    private string FormatScale(decimal? scale, string? unit)
    {
        if (scale == null) return "N/A";
        return $"1:{scale} {unit ?? "mm"}";
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

    private string GetCardClasses(Takeoff drawing)
    {
        return drawing.ProcessingStatus?.ToLower() == "completed" ? "card-completed" : "";
    }
}