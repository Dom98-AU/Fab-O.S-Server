namespace FabOS.WebServer.Services.Interfaces
{
    /// <summary>
    /// Service for creating Nutrient DWS (Document Web Service) session tokens
    /// Session tokens are required to authenticate with the cloud-hosted Nutrient SDK
    /// </summary>
    public interface INutrientDwsSessionService
    {
        /// <summary>
        /// Creates a session token for a PDF document by calling Nutrient DWS API
        /// </summary>
        /// <param name="documentUrl">URL of the PDF document to load (must be publicly accessible)</param>
        /// <param name="userId">Optional user identifier for analytics</param>
        /// <returns>Session token for browser authentication</returns>
        Task<string> CreateSessionTokenAsync(string documentUrl, string? userId = null);
    }

    /// <summary>
    /// Response model for DWS session creation API
    /// </summary>
    public class DwsSessionResponse
    {
        public string? SessionId { get; set; }
        public string? Jwt { get; set; }
    }

    /// <summary>
    /// Request model for DWS session creation API
    /// </summary>
    public class DwsSessionRequest
    {
        public string? DocumentUrl { get; set; }
        public string? UserId { get; set; }
    }
}
