using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using FabOS.WebServer.Services;

namespace FabOS.WebServer.Components.Pages;

public partial class SimplePdfTest : ComponentBase
{
    [Inject] private ILogger<SimplePdfTest> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string pdfUrl = "";
    private string testPdfPath = "/sample-documents/test.pdf";
    private IBrowserFile? uploadedFile;
    private bool isLoading = false;
    private string statusMessage = "";
    private byte[]? pdfBytes;
    private string pdfBase64 = "";

    protected override void OnInitialized()
    {
        // Initialize with a test PDF if available
        pdfUrl = testPdfPath;
        statusMessage = "Component initialized. Ready to load PDF.";
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        isLoading = true;
        statusMessage = "Uploading PDF...";

        try
        {
            uploadedFile = e.File;

            if (uploadedFile.ContentType != "application/pdf")
            {
                statusMessage = "Please upload a PDF file.";
                return;
            }

            if (uploadedFile.Size > 50 * 1024 * 1024) // 50MB limit
            {
                statusMessage = "File size exceeds 50MB limit.";
                return;
            }

            // Read file into memory
            using var stream = uploadedFile.OpenReadStream(50 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            pdfBytes = memoryStream.ToArray();

            // Convert to base64 for display
            pdfBase64 = Convert.ToBase64String(pdfBytes);
            pdfUrl = $"data:application/pdf;base64,{pdfBase64}";

            statusMessage = $"PDF loaded: {uploadedFile.Name} ({uploadedFile.Size / 1024:N0} KB)";
            Logger.LogInformation($"PDF uploaded: {uploadedFile.Name}, Size: {uploadedFile.Size}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error uploading PDF");
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void LoadSamplePdf()
    {
        pdfUrl = testPdfPath;
        statusMessage = "Sample PDF loaded.";
        Logger.LogInformation("Sample PDF loaded");
    }

    private void LoadExternalPdf()
    {
        // Example external PDF URL
        pdfUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf";
        statusMessage = "External PDF loaded.";
        Logger.LogInformation("External PDF loaded");
    }

    private void ClearPdf()
    {
        pdfUrl = "";
        pdfBase64 = "";
        pdfBytes = null;
        uploadedFile = null;
        statusMessage = "PDF cleared.";
        Logger.LogInformation("PDF cleared");
    }

    private async Task DownloadPdf()
    {
        if (pdfBytes != null && uploadedFile != null)
        {
            try
            {
                // In a real application, this would trigger a file download
                statusMessage = $"Downloading: {uploadedFile.Name}";
                Logger.LogInformation($"PDF download initiated: {uploadedFile.Name}");
                
                // Simulate download delay
                await Task.Delay(500);
                statusMessage = "Download complete.";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error downloading PDF");
                statusMessage = $"Download error: {ex.Message}";
            }
        }
        else
        {
            statusMessage = "No PDF to download.";
        }
    }

    private void TestPdfViewer()
    {
        Navigation.NavigateTo("/pdf-viewer-test");
    }
}