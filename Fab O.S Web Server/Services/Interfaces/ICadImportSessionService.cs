using FabOS.WebServer.Models.DTOs;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing CAD import sessions.
/// Stores pending import data until user confirms with part mappings.
/// </summary>
public interface ICadImportSessionService
{
    /// <summary>
    /// Create a new import session and store the parse result
    /// </summary>
    /// <param name="drawingId">Drawing ID</param>
    /// <param name="drawingRevisionId">Drawing revision ID</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="parseResult">Parse result from IFC/SMLX parser</param>
    /// <returns>Preview DTO for API response</returns>
    CadImportPreviewDto CreateSession(
        int drawingId,
        int drawingRevisionId,
        string fileName,
        CadParseResultDto parseResult);

    /// <summary>
    /// Get an existing import session by ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Session data or null if not found/expired</returns>
    CadImportSessionData? GetSession(string sessionId);

    /// <summary>
    /// Get the preview DTO for an existing session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Preview DTO or null if not found/expired</returns>
    CadImportPreviewDto? GetPreview(string sessionId);

    /// <summary>
    /// Apply user-provided part mappings to the session and return the final parse result
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Mappings provided by user</param>
    /// <returns>Updated parse result or null if session not found</returns>
    CadParseResultDto? ApplyMappings(string sessionId, ConfirmCadImportRequestDto request);

    /// <summary>
    /// Remove a session from cache (after successful import or cancellation)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    void RemoveSession(string sessionId);
}
