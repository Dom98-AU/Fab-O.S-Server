using FabOS.WebServer.Services.Implementations;

namespace FabOS.WebServer.Services.Interfaces
{
    public interface IAzureOcrService
    {
        Task<OcrAnalysisResult> AnalyzePdfTitleblock(Stream pdfStream, string fileName);
    }
}
