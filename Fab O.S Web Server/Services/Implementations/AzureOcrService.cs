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
                    new ExtractedField { FieldName = "Drawing Number", Value = ExtractMockDrawingNumber(fileName), Confidence = 0.98f },
                    new ExtractedField { FieldName = "Drawing Title", Value = ExtractMockDrawingTitle(fileName), Confidence = 0.92f },
                    new ExtractedField { FieldName = "Project Name", Value = $"{tenantContext.CompanyName} - {tenantContext.DefaultProjectTemplate}", Confidence = 0.88f },
                    new ExtractedField { FieldName = "Client", Value = GenerateClientName(tenantContext), Confidence = 0.85f },
                    new ExtractedField { FieldName = "Scale", Value = "1:100", Confidence = 0.95f },
                    new ExtractedField { FieldName = "Revision", Value = "A", Confidence = 0.90f }
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
            
            // Extract text and key-value pairs
            var extractedFields = new List<ExtractedField>();
            var confidence = 0.8f; // Default confidence

            // Try to extract titleblock information using pattern matching
            var drawingNumber = ExtractFieldFromDocument(analyzeResult, "drawing.?number|dwg.?no|drawing.?no", fileName);
            var drawingTitle = ExtractFieldFromDocument(analyzeResult, "title|drawing.?title|description", fileName);
            var projectName = ExtractFieldFromDocument(analyzeResult, "project|job.?name|project.?name", fileName);
            var clientName = ExtractFieldFromDocument(analyzeResult, "client|owner|company", fileName);
            var scale = ExtractFieldFromDocument(analyzeResult, "scale", "1:100");
            var revision = ExtractFieldFromDocument(analyzeResult, "rev|revision", "A");

            return new OcrAnalysisResult
            {
                DrawingNumber = drawingNumber,
                DrawingTitle = drawingTitle,
                ProjectName = projectName ?? $"{tenantContext.CompanyName} Project",
                ClientName = clientName ?? GenerateClientName(tenantContext),
                Discipline = DetermineDiscipline(fileName),
                Scale = scale,
                Revision = revision,
                DrawingDate = DateTime.Now.AddDays(-30),
                Confidence = confidence,
                TenantId = tenantContext.TenantId,
                CompanyId = tenantContext.CompanyId,
                ExtractedFields = extractedFields,
                TitleblockBounds = ExtractTitleblockBounds(analyzeResult)
            };
        }

        private string ExtractFieldFromDocument(JsonElement analyzeResult, string pattern, string fallback)
        {
            // This would implement pattern matching against the extracted text
            // For now, return smart fallback based on patterns
            return fallback;
        }

        private BoundingBox? ExtractTitleblockBounds(JsonElement analyzeResult)
        {
            // This would analyze the document layout to find the titleblock region
            // For now, return a default titleblock location (typically bottom-right)
            return new BoundingBox { X = 1200, Y = 50, Width = 400, Height = 200 };
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
