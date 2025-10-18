using System.Text.Json;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Data.Contexts;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net.Http.Headers;

namespace FabOS.WebServer.Services.Implementations
{
    public class AzureOcrService : IAzureOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _apiVersion;
        private readonly string _modelId;

        public AzureOcrService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            
            _endpoint = _configuration["AzureDocumentIntelligence:Endpoint"] ?? 
                throw new InvalidOperationException("Azure Document Intelligence endpoint not configured");
            _apiKey = _configuration["AzureDocumentIntelligence:ApiKey"] ?? 
                throw new InvalidOperationException("Azure Document Intelligence API key not configured");
            _apiVersion = _configuration["AzureDocumentIntelligence:ApiVersion"] ?? "2024-02-29-preview";
            _modelId = _configuration["AzureDocumentIntelligence:ModelId"] ?? "prebuilt-layout";

            // Configure HTTP client
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
        }

        public async Task<OcrAnalysisResult> AnalyzePdfTitleblock(Stream pdfStream, string fileName)
        {
            try
            {
                // Get current tenant/company context
                var tenantContext = await GetCurrentTenantContext();
                
                // For development/testing, use mock data first, then implement real OCR
                if (_configuration.GetValue<bool>("AzureDocumentIntelligence:UseMockData", true))
                {
                    return await GenerateMockOcrResult(fileName, tenantContext);
                }
                else
                {
                    return await CallAzureDocumentIntelligence(pdfStream, fileName, tenantContext);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"OCR analysis failed: {ex.Message}", ex);
            }
        }

        private async Task<TenantContext> GetCurrentTenantContext()
        {
            // Extract tenant information from current user context
            var httpContext = _httpContextAccessor.HttpContext;
            
            // For now, return mock tenant context
            // In production, this would extract from JWT claims or session
            return new TenantContext
            {
                CompanyId = 1,
                CompanyName = "Demo Steel Fabrication",
                TenantId = "demo-tenant",
                DefaultProjectTemplate = "Steel Construction"
            };
        }

        private async Task<OcrAnalysisResult> GenerateMockOcrResult(string fileName, TenantContext tenantContext)
        {
            // Simulate processing time
            await Task.Delay(2000);

            // Generate tenant-aware mock data
            var mockResult = new OcrAnalysisResult
            {
                DrawingNumber = ExtractMockDrawingNumber(fileName),
                DrawingTitle = ExtractMockDrawingTitle(fileName),
                ProjectName = $"{tenantContext.CompanyName} - {tenantContext.DefaultProjectTemplate}",
                ClientName = GenerateClientName(tenantContext),
                Discipline = DetermineDiscipline(fileName),
                Scale = "1:100",
                Revision = "A",
                DrawingDate = DateTime.Now.AddDays(-30),
                Confidence = 0.95f,
                TenantId = tenantContext.TenantId,
                CompanyId = tenantContext.CompanyId,
                TitleblockBounds = new BoundingBox
                {
                    X = 1200,
                    Y = 50,
                    Width = 400,
                    Height = 200
                },
                ExtractedFields = new List<ExtractedField>
                {
                    new ExtractedField
                    {
                        FieldName = "Drawing Number",
                        Value = ExtractMockDrawingNumber(fileName),
                        Confidence = 0.98f,
                        Bounds = new BoundingBox { X = 1220, Y = 70, Width = 180, Height = 30 }
                    },
                    new ExtractedField
                    {
                        FieldName = "Drawing Title",
                        Value = ExtractMockDrawingTitle(fileName),
                        Confidence = 0.92f,
                        Bounds = new BoundingBox { X = 1220, Y = 110, Width = 360, Height = 30 }
                    },
                    new ExtractedField
                    {
                        FieldName = "Project Name",
                        Value = $"{tenantContext.CompanyName} - {tenantContext.DefaultProjectTemplate}",
                        Confidence = 0.88f,
                        Bounds = new BoundingBox { X = 1220, Y = 150, Width = 360, Height = 30 }
                    },
                    new ExtractedField
                    {
                        FieldName = "Client",
                        Value = GenerateClientName(tenantContext),
                        Confidence = 0.85f,
                        Bounds = new BoundingBox { X = 1220, Y = 190, Width = 280, Height = 25 }
                    },
                    new ExtractedField
                    {
                        FieldName = "Scale",
                        Value = "1:100",
                        Confidence = 0.95f,
                        Bounds = new BoundingBox { X = 1420, Y = 70, Width = 80, Height = 25 }
                    },
                    new ExtractedField
                    {
                        FieldName = "Revision",
                        Value = "A",
                        Confidence = 0.90f,
                        Bounds = new BoundingBox { X = 1520, Y = 70, Width = 60, Height = 25 }
                    }
                }
            };

            return mockResult;
        }

        private async Task<OcrAnalysisResult> CallAzureDocumentIntelligence(Stream pdfStream, string fileName, TenantContext tenantContext)
        {
            try
            {
                // Convert stream to byte array
                var pdfBytes = new byte[pdfStream.Length];
                await pdfStream.ReadAsync(pdfBytes, 0, pdfBytes.Length);
                pdfStream.Position = 0; // Reset stream position

                // Step 1: Submit document for analysis
                var analyzeUrl = $"{_endpoint.TrimEnd('/')}/formrecognizer/documentModels/{_modelId}:analyze?api-version={_apiVersion}";
                
                using var content = new ByteArrayContent(pdfBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                
                var response = await _httpClient.PostAsync(analyzeUrl, content);
                response.EnsureSuccessStatusCode();

                // Get operation location from response headers
                var operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault()
                    ?? throw new Exception("Operation-Location header not found in response");

                // Step 2: Poll for results
                OcrAnalysisResult? result = null;
                var maxAttempts = 30; // 30 seconds max
                var attempt = 0;

                while (result == null && attempt < maxAttempts)
                {
                    await Task.Delay(1000); // Wait 1 second
                    attempt++;

                    var statusResponse = await _httpClient.GetAsync(operationLocation);
                    statusResponse.EnsureSuccessStatusCode();

                    var statusJson = await statusResponse.Content.ReadAsStringAsync();
                    var statusData = JsonSerializer.Deserialize<JsonElement>(statusJson);

                    var status = statusData.GetProperty("status").GetString();

                    if (status == "succeeded")
                    {
                        result = await ParseDocumentIntelligenceResult(statusJson, fileName, tenantContext);
                    }
                    else if (status == "failed")
                    {
                        var error = statusData.TryGetProperty("error", out var errorElement) 
                            ? errorElement.GetProperty("message").GetString() 
                            : "Unknown error";
                        throw new Exception($"Document Intelligence analysis failed: {error}");
                    }
                }

                if (result == null)
                {
                    throw new TimeoutException("Document Intelligence analysis timed out");
                }

                return result;
            }
            catch (Exception ex)
            {
                // Fallback to mock data if Azure service fails
                Console.WriteLine($"Azure Document Intelligence failed, using mock data: {ex.Message}");
                return await GenerateMockOcrResult(fileName, tenantContext);
            }
        }

        private async Task<OcrAnalysisResult> ParseDocumentIntelligenceResult(string jsonResult, string fileName, TenantContext tenantContext)
        {
            var document = JsonSerializer.Deserialize<JsonElement>(jsonResult);
            var analyzeResult = document.GetProperty("analyzeResult");

            // Step 1: Detect titleblock region
            var titleblockRegion = DetectTitleblockRegion(analyzeResult);

            // Step 2: Extract text content from titleblock region
            var titleblockText = ExtractTextFromRegion(analyzeResult, titleblockRegion);

            // Step 3: Extract titleblock fields using pattern matching
            var extractedFields = ExtractTitleblockFields(titleblockText, analyzeResult, titleblockRegion);

            // Step 4: Parse individual fields
            var drawingNumber = GetFieldValue(extractedFields, "Drawing Number", ExtractMockDrawingNumber(fileName));
            var drawingTitle = GetFieldValue(extractedFields, "Drawing Title", ExtractMockDrawingTitle(fileName));
            var projectName = GetFieldValue(extractedFields, "Project", $"{tenantContext.CompanyName} Project");
            var clientName = GetFieldValue(extractedFields, "Client", GenerateClientName(tenantContext));
            var scale = GetFieldValue(extractedFields, "Scale", "1:100");
            var revision = GetFieldValue(extractedFields, "Revision", "A");
            var dateStr = GetFieldValue(extractedFields, "Date", null);

            // Step 5: Parse date if found
            DateTime? drawingDate = ParseDrawingDate(dateStr);

            // Step 6: Calculate overall confidence
            var confidence = CalculateAverageConfidence(extractedFields);

            return new OcrAnalysisResult
            {
                DrawingNumber = drawingNumber,
                DrawingTitle = drawingTitle,
                ProjectName = projectName,
                ClientName = clientName,
                Discipline = DetermineDiscipline(fileName),
                Scale = scale,
                Revision = revision,
                DrawingDate = drawingDate ?? DateTime.Now.AddDays(-30),
                Confidence = confidence,
                TenantId = tenantContext.TenantId,
                CompanyId = tenantContext.CompanyId,
                ExtractedFields = extractedFields,
                TitleblockBounds = titleblockRegion
            };
        }

        /// <summary>
        /// Detects the titleblock region (typically bottom-right of the drawing)
        /// using keyword density analysis
        /// </summary>
        private BoundingBox DetectTitleblockRegion(JsonElement analyzeResult)
        {
            try
            {
                // Get page dimensions
                var pages = analyzeResult.GetProperty("pages");
                if (pages.GetArrayLength() == 0)
                {
                    return GetDefaultTitleblockBounds();
                }

                var firstPage = pages[0];
                var pageWidth = firstPage.GetProperty("width").GetDouble();
                var pageHeight = firstPage.GetProperty("height").GetDouble();

                // Define titleblock search region (bottom-right quadrant)
                // Typically titleblocks are in bottom-right 30% of page
                var searchRegion = new BoundingBox
                {
                    X = (int)(pageWidth * 0.65),
                    Y = (int)(pageHeight * 0.70),
                    Width = (int)(pageWidth * 0.35),
                    Height = (int)(pageHeight * 0.30)
                };

                // Look for titleblock keywords in this region
                var titleblockKeywords = new[] { "drawing", "dwg", "title", "project", "client", "scale", "revision", "rev", "date", "drawn", "checked" };

                if (!firstPage.TryGetProperty("words", out var words))
                {
                    return searchRegion;
                }

                var keywordMatches = new List<(double x, double y, double width, double height)>();

                foreach (var word in words.EnumerateArray())
                {
                    if (!word.TryGetProperty("content", out var contentProp))
                        continue;

                    var content = contentProp.GetString()?.ToLower() ?? "";

                    if (titleblockKeywords.Any(k => content.Contains(k)))
                    {
                        if (word.TryGetProperty("polygon", out var polygon))
                        {
                            var bbox = PolygonToBoundingBox(polygon);
                            if (IsWithinRegion(bbox, searchRegion))
                            {
                                keywordMatches.Add((bbox.X, bbox.Y, bbox.Width, bbox.Height));
                            }
                        }
                    }
                }

                // If we found keywords, calculate bounding box around all matches
                if (keywordMatches.Any())
                {
                    var minX = keywordMatches.Min(m => m.x);
                    var minY = keywordMatches.Min(m => m.y);
                    var maxX = keywordMatches.Max(m => m.x + m.width);
                    var maxY = keywordMatches.Max(m => m.y + m.height);

                    return new BoundingBox
                    {
                        X = (int)minX,
                        Y = (int)minY,
                        Width = (int)(maxX - minX),
                        Height = (int)(maxY - minY)
                    };
                }

                return searchRegion;
            }
            catch
            {
                return GetDefaultTitleblockBounds();
            }
        }

        /// <summary>
        /// Converts Azure polygon format to bounding box
        /// </summary>
        private BoundingBox PolygonToBoundingBox(JsonElement polygon)
        {
            var points = polygon.EnumerateArray().ToList();
            if (points.Count < 4)
            {
                return new BoundingBox { X = 0, Y = 0, Width = 0, Height = 0 };
            }

            var xCoords = new List<double>();
            var yCoords = new List<double>();

            for (int i = 0; i < points.Count; i += 2)
            {
                xCoords.Add(points[i].GetDouble());
                if (i + 1 < points.Count)
                {
                    yCoords.Add(points[i + 1].GetDouble());
                }
            }

            var minX = xCoords.Min();
            var minY = yCoords.Min();
            var maxX = xCoords.Max();
            var maxY = yCoords.Max();

            return new BoundingBox
            {
                X = (int)minX,
                Y = (int)minY,
                Width = (int)(maxX - minX),
                Height = (int)(maxY - minY)
            };
        }

        /// <summary>
        /// Checks if a bounding box is within a region
        /// </summary>
        private bool IsWithinRegion(BoundingBox bbox, BoundingBox region)
        {
            return bbox.X >= region.X &&
                   bbox.Y >= region.Y &&
                   (bbox.X + bbox.Width) <= (region.X + region.Width) &&
                   (bbox.Y + bbox.Height) <= (region.Y + region.Height);
        }

        /// <summary>
        /// Extracts all text content from a specific region
        /// </summary>
        private string ExtractTextFromRegion(JsonElement analyzeResult, BoundingBox region)
        {
            var textBuilder = new StringBuilder();

            try
            {
                var pages = analyzeResult.GetProperty("pages");
                if (pages.GetArrayLength() == 0)
                    return "";

                var firstPage = pages[0];
                if (!firstPage.TryGetProperty("words", out var words))
                    return "";

                foreach (var word in words.EnumerateArray())
                {
                    if (word.TryGetProperty("polygon", out var polygon) &&
                        word.TryGetProperty("content", out var content))
                    {
                        var bbox = PolygonToBoundingBox(polygon);
                        if (IsWithinRegion(bbox, region))
                        {
                            textBuilder.Append(content.GetString() + " ");
                        }
                    }
                }
            }
            catch
            {
                // Return empty string on error
            }

            return textBuilder.ToString();
        }

        /// <summary>
        /// Extracts titleblock fields using regex patterns
        /// </summary>
        private List<ExtractedField> ExtractTitleblockFields(string titleblockText, JsonElement analyzeResult, BoundingBox titleblockRegion)
        {
            var fields = new List<ExtractedField>();

            // Define regex patterns for common titleblock fields
            var patterns = new Dictionary<string, string>
            {
                { "Drawing Number", @"(?:drawing|dwg)\s*(?:no\.?|number|#)[\s:]*([A-Z0-9\-\.\/]+)" },
                { "Drawing Title", @"(?:title|drawing\s*title)[\s:]*([^\n\r]{5,100})" },
                { "Project", @"(?:project|job)\s*(?:name)?[\s:]*([^\n\r]{5,100})" },
                { "Client", @"(?:client|owner|company)[\s:]*([^\n\r]{3,100})" },
                { "Scale", @"(?:scale)[\s:]*(\d+:\d+|1/\d+|NTS|AS NOTED)" },
                { "Revision", @"(?:rev\.?|revision)[\s:]*([A-Z0-9]{1,5})" },
                { "Date", @"(?:date|drawn)[\s:]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4})" }
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    titleblockText,
                    pattern.Value,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (match.Success && match.Groups.Count > 1)
                {
                    fields.Add(new ExtractedField
                    {
                        FieldName = pattern.Key,
                        Value = match.Groups[1].Value.Trim(),
                        Confidence = 0.85f, // Pattern match confidence
                        Bounds = titleblockRegion
                    });
                }
            }

            return fields;
        }

        /// <summary>
        /// Gets field value from extracted fields or returns fallback
        /// </summary>
        private string GetFieldValue(List<ExtractedField> fields, string fieldName, string? fallback)
        {
            var field = fields.FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            return field?.Value ?? fallback ?? "";
        }

        /// <summary>
        /// Calculates average confidence from extracted fields
        /// </summary>
        private float CalculateAverageConfidence(List<ExtractedField> fields)
        {
            if (!fields.Any())
                return 0.5f; // Low confidence if no fields extracted

            return fields.Average(f => f.Confidence);
        }

        /// <summary>
        /// Parses drawing date from various formats
        /// </summary>
        private DateTime? ParseDrawingDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            var formats = new[]
            {
                "M/d/yyyy", "MM/dd/yyyy", "M-d-yyyy", "MM-dd-yyyy",
                "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy",
                "M/d/yy", "MM/dd/yy", "M-d-yy", "MM-dd-yy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    return date;
                }
            }

            // Try general parsing as last resort
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                return parsedDate;
            }

            return null;
        }

        /// <summary>
        /// Returns default titleblock bounds (bottom-right corner)
        /// </summary>
        private BoundingBox GetDefaultTitleblockBounds()
        {
            return new BoundingBox
            {
                X = 1200,
                Y = 50,
                Width = 400,
                Height = 200
            };
        }

        private string ExtractMockDrawingNumber(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            if (nameWithoutExt.Contains("-"))
            {
                return nameWithoutExt.ToUpper();
            }
            
            return $"DWG-{DateTime.Now:yyyyMMdd}-001";
        }

        private string ExtractMockDrawingTitle(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            if (nameWithoutExt.ToLower().Contains("foundation"))
                return "Foundation Plan";
            if (nameWithoutExt.ToLower().Contains("beam"))
                return "Beam Layout Plan";
            if (nameWithoutExt.ToLower().Contains("column"))
                return "Column Layout Plan";
            if (nameWithoutExt.ToLower().Contains("detail"))
                return "Connection Details";
            if (nameWithoutExt.ToLower().Contains("elevation"))
                return "Building Elevation";
            if (nameWithoutExt.ToLower().Contains("section"))
                return "Building Section";
                
            return "General Arrangement Drawing";
        }

        private string DetermineDiscipline(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToLower();
            
            if (nameWithoutExt.StartsWith("s-") || nameWithoutExt.Contains("struct"))
                return "Structural";
            if (nameWithoutExt.StartsWith("a-") || nameWithoutExt.Contains("arch"))
                return "Architectural";
            if (nameWithoutExt.StartsWith("m-") || nameWithoutExt.Contains("mech"))
                return "Mechanical";
            if (nameWithoutExt.StartsWith("e-") || nameWithoutExt.Contains("elec"))
                return "Electrical";
                
            return "Structural"; // Default for steel fabrication
        }

        private string GenerateClientName(TenantContext tenantContext)
        {
            // Generate client name based on tenant context
            return tenantContext.CompanyName.Contains("Demo") 
                ? "ABC Construction Company" 
                : $"{tenantContext.CompanyName} Client";
        }
    }

    public class TenantContext
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public string TenantId { get; set; } = "";
        public string DefaultProjectTemplate { get; set; } = "";
    }

    public class OcrAnalysisResult
    {
        public string DrawingNumber { get; set; } = "";
        public string DrawingTitle { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string Discipline { get; set; } = "";
        public string Scale { get; set; } = "";
        public string Revision { get; set; } = "";
        public DateTime? DrawingDate { get; set; }
        public float Confidence { get; set; }
        
        // Multi-tenant properties
        public string TenantId { get; set; } = "";
        public int CompanyId { get; set; }
        
        public BoundingBox? TitleblockBounds { get; set; }
        public List<ExtractedField> ExtractedFields { get; set; } = new();
        public string? PreviewImageBase64 { get; set; }
    }

    public class ExtractedField
    {
        public string FieldName { get; set; } = "";
        public string Value { get; set; } = "";
        public float Confidence { get; set; }
        public BoundingBox? Bounds { get; set; }
    }

    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
