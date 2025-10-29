using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Components.Shared;

public partial class MeasurementExportModal : ComponentBase
{
    [Inject] private IMeasurementExportService ExportService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<MeasurementExportModal> Logger { get; set; } = default!;

    [Parameter] public int PackageDrawingId { get; set; }
    [Parameter] public string DrawingNumber { get; set; } = string.Empty;
    [Parameter] public string DrawingTitle { get; set; } = string.Empty;
    [Parameter] public int MeasurementCount { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private bool _isVisible = false;
    private bool _isExporting = false;
    private bool _groupByItem = true;
    private string? _errorMessage = null;

    /// <summary>
    /// Show the export modal
    /// </summary>
    public void Show()
    {
        _isVisible = true;
        _isExporting = false;
        _errorMessage = null;
        _groupByItem = true;
        StateHasChanged();
    }

    /// <summary>
    /// Hide the export modal
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
        StateHasChanged();
    }

    /// <summary>
    /// Cancel export and close modal
    /// </summary>
    private async Task Cancel()
    {
        Hide();
        await OnClose.InvokeAsync();
    }

    /// <summary>
    /// Export measurements to Excel and trigger download
    /// </summary>
    private async Task ExportToExcel()
    {
        try
        {
            _isExporting = true;
            _errorMessage = null;
            StateHasChanged();

            Logger.LogInformation("[MeasurementExportModal] Starting export for PackageDrawingId={PackageDrawingId}, GroupByItem={GroupByItem}",
                PackageDrawingId, _groupByItem);

            // Generate Excel file
            var excelBytes = await ExportService.ExportMeasurementsToExcelAsync(PackageDrawingId, _groupByItem);

            if (excelBytes == null || excelBytes.Length == 0)
            {
                throw new InvalidOperationException("Export service returned empty file");
            }

            Logger.LogInformation("[MeasurementExportModal] Excel file generated successfully, Size={Size} bytes", excelBytes.Length);

            // Generate filename
            var fileName = GenerateFileName();

            // Download file via JavaScript
            await DownloadFileAsync(fileName, excelBytes);

            Logger.LogInformation("[MeasurementExportModal] Export completed successfully");

            // Close modal after successful export
            _isExporting = false;
            Hide();
            await OnClose.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[MeasurementExportModal] Error exporting measurements for PackageDrawingId={PackageDrawingId}", PackageDrawingId);
            _errorMessage = $"Export failed: {ex.Message}";
            _isExporting = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Generate filename for the Excel export
    /// </summary>
    private string GenerateFileName()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeDrawingNumber = string.IsNullOrEmpty(DrawingNumber)
            ? "Drawing"
            : DrawingNumber.Replace("/", "-").Replace("\\", "-").Replace(" ", "_");

        return $"MaterialList_{safeDrawingNumber}_{timestamp}.xlsx";
    }

    /// <summary>
    /// Download file to user's browser using JavaScript interop
    /// </summary>
    private async Task DownloadFileAsync(string fileName, byte[] fileBytes)
    {
        var base64 = Convert.ToBase64String(fileBytes);
        await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, base64);
    }
}
