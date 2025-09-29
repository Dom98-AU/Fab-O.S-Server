using SixLabors.ImageSharp;

namespace FabOS.WebServer.Services.Interfaces
{
    public interface IPdfProcessingService
    {
        // PDF Page Operations
        Task<int> GetPageCountAsync(Stream pdfStream);
        Task<int> GetPageCountAsync(string pdfPath);
        Task<Stream> ExtractPageAsync(Stream pdfStream, int pageNumber);
        Task<List<Stream>> ExtractAllPagesAsync(Stream pdfStream);

        // PDF to Image Conversion
        Task<Image> RenderPageToImageAsync(Stream pdfStream, int pageNumber, float dpi = 150);
        Task<List<Image>> RenderAllPagesToImagesAsync(Stream pdfStream, float dpi = 150);
        Task<byte[]> RenderPageToJpegAsync(Stream pdfStream, int pageNumber, float dpi = 150, int quality = 85);
        Task<byte[]> RenderPageToPngAsync(Stream pdfStream, int pageNumber, float dpi = 150);

        // Text Extraction
        Task<string> ExtractTextFromPageAsync(Stream pdfStream, int pageNumber);
        Task<string> ExtractAllTextAsync(Stream pdfStream);
        Task<List<TextBlock>> ExtractTextBlocksAsync(Stream pdfStream, int pageNumber);
        Task<List<TextBlock>> ExtractTextInRegionAsync(Stream pdfStream, int pageNumber, Rectangle region);

        // Coordinate Mapping
        PdfPoint ScreenToPdfCoordinates(ScreenPoint screenPoint, float scale, float dpi);
        ScreenPoint PdfToScreenCoordinates(PdfPoint pdfPoint, float scale, float dpi);
        decimal CalculateRealDistance(PdfPoint point1, PdfPoint point2, float scale, string scaleUnit);
        decimal CalculateRealArea(List<PdfPoint> points, float scale, string scaleUnit);

        // Scale Calibration
        float CalculateScaleFromKnownDistance(PdfPoint point1, PdfPoint point2, decimal knownDistance, string unit);
        ScaleInfo DetectScaleFromText(string pageText);
        ScaleInfo CalibrateScale(decimal pixelDistance, decimal realDistance, string unit);

        // Measurement Operations
        MeasurementResult MeasureLinear(PdfPoint start, PdfPoint end, float scale, string unit);
        MeasurementResult MeasureArea(List<PdfPoint> points, float scale, string unit);
        MeasurementResult MeasurePerimeter(List<PdfPoint> points, float scale, string unit);
        MeasurementResult MeasureAngle(PdfPoint vertex, PdfPoint point1, PdfPoint point2);

        // Annotation Support
        Task<Stream> AddAnnotationToPdfAsync(Stream pdfStream, List<PdfAnnotation> annotations);
        Task<Stream> AddMeasurementOverlayAsync(Stream pdfStream, List<MeasurementOverlay> measurements);
        Task<byte[]> ExportAnnotatedPdfAsync(Stream pdfStream, List<PdfAnnotation> annotations);

        // Utility Methods
        Task<PdfInfo> GetPdfInfoAsync(Stream pdfStream);
        Task<bool> ValidatePdfAsync(Stream pdfStream);
        Task<Stream> OptimizePdfAsync(Stream pdfStream);
        Task<Stream> MergePdfsAsync(List<Stream> pdfStreams);
    }

    // Supporting classes
    public class ScreenPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class PdfPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int PageNumber { get; set; }
    }

    public class Rectangle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class TextBlock
    {
        public string Text { get; set; }
        public Rectangle BoundingBox { get; set; }
        public float FontSize { get; set; }
        public string FontName { get; set; }
    }

    public class ScaleInfo
    {
        public float Scale { get; set; }
        public string Unit { get; set; }
        public string ScaleText { get; set; } // e.g., "1:100", "1/4\" = 1'"
        public float PixelsPerUnit { get; set; }
        public bool IsMetric { get; set; }
    }

    public class MeasurementResult
    {
        public string Type { get; set; } // Linear, Area, Perimeter, Angle
        public decimal Value { get; set; }
        public string Unit { get; set; }
        public List<PdfPoint> Points { get; set; }
        public string FormattedValue { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    public class PdfAnnotation
    {
        public string Type { get; set; } // Text, Line, Rectangle, Polygon, Arrow, Cloud
        public List<PdfPoint> Points { get; set; }
        public string Text { get; set; }
        public string Color { get; set; }
        public float LineWidth { get; set; }
        public int PageNumber { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class MeasurementOverlay
    {
        public MeasurementResult Measurement { get; set; }
        public string Label { get; set; }
        public string Color { get; set; }
        public float LineWidth { get; set; }
        public bool ShowDimension { get; set; }
        public string FontFamily { get; set; }
        public float FontSize { get; set; }
    }

    public class PdfInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Subject { get; set; }
        public string Keywords { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? ModificationDate { get; set; }
        public int PageCount { get; set; }
        public string PdfVersion { get; set; }
        public long FileSize { get; set; }
        public List<PageInfo> Pages { get; set; }
    }

    public class PageInfo
    {
        public int PageNumber { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Rotation { get; set; }
        public string MediaBox { get; set; }
    }
}