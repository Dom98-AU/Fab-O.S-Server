using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services
{
    /// <summary>
    /// Implementation of INutrientDwsSessionService
    /// Calls Nutrient DWS API to create session tokens for PDF viewing
    /// </summary>
    public class NutrientDwsSessionService : INutrientDwsSessionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NutrientDwsSessionService> _logger;

        public NutrientDwsSessionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NutrientDwsSessionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateSessionTokenAsync(string documentUrl, string? userId = null)
        {
            var apiKey = _configuration["Nutrient:DWS:ApiKey"];
            var apiBaseUrl = _configuration["Nutrient:DWS:ApiBaseUrl"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Nutrient DWS API key is not configured in appsettings.json");
            }

            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new InvalidOperationException("Nutrient DWS API base URL is not configured in appsettings.json");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                // Prepare request payload
                var requestPayload = new
                {
                    document = new
                    {
                        url = documentUrl
                    },
                    userId = userId
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Create HTTP request
                var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/viewer/sessions")
                {
                    Content = content
                };

                // Add Bearer token authentication
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                _logger.LogInformation("[NutrientDWS] Creating session token for document: {DocumentUrl}", documentUrl);

                // Call DWS API
                var response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("[NutrientDWS] Failed to create session token. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to create DWS session token: {response.StatusCode} - {errorContent}");
                }

                // Parse response
                var responseContent = await response.Content.ReadAsStringAsync();
                var sessionResponse = JsonSerializer.Deserialize<DwsSessionResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (string.IsNullOrEmpty(sessionResponse?.Jwt))
                {
                    _logger.LogError("[NutrientDWS] Session token (JWT) not found in response: {Response}", responseContent);
                    throw new InvalidOperationException("DWS API did not return a valid session token");
                }

                _logger.LogInformation("[NutrientDWS] ✓ Session token created successfully. SessionId: {SessionId}",
                    sessionResponse.SessionId);

                return sessionResponse.Jwt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NutrientDWS] Exception while creating session token for {DocumentUrl}", documentUrl);
                throw;
            }
        }
    }
}
