using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for managing takeoff revisions
/// </summary>
public interface ITakeoffRevisionService
{
    /// <summary>
    /// Creates a new revision for a takeoff
    /// </summary>
    /// <param name="takeoffId">The takeoff to create a revision for</param>
    /// <param name="description">Optional description of the revision</param>
    /// <param name="userId">User creating the revision</param>
    /// <returns>The created revision</returns>
    Task<TakeoffRevision> CreateRevisionAsync(int takeoffId, string? description = null, int? userId = null);

    /// <summary>
    /// Gets the active (working) revision for a takeoff
    /// </summary>
    /// <param name="takeoffId">The takeoff ID</param>
    /// <returns>The active revision, or null if none exists</returns>
    Task<TakeoffRevision?> GetActiveRevisionAsync(int takeoffId);

    /// <summary>
    /// Gets all revisions for a takeoff
    /// </summary>
    /// <param name="takeoffId">The takeoff ID</param>
    /// <returns>List of revisions</returns>
    Task<List<TakeoffRevision>> GetRevisionsByTakeoffAsync(int takeoffId);

    /// <summary>
    /// Gets a specific revision by ID
    /// </summary>
    /// <param name="revisionId">The revision ID</param>
    /// <returns>The revision, or null if not found</returns>
    Task<TakeoffRevision?> GetRevisionByIdAsync(int revisionId);

    /// <summary>
    /// Sets a revision as the active (working) revision for its takeoff
    /// </summary>
    /// <param name="revisionId">The revision to activate</param>
    /// <returns>True if successful</returns>
    Task<bool> SetActiveRevisionAsync(int revisionId);

    /// <summary>
    /// Copies a revision to create a new one. Includes all packages and SharePoint files.
    /// </summary>
    /// <param name="sourceRevisionId">The revision to copy from</param>
    /// <param name="newDescription">Description for the new revision</param>
    /// <param name="userId">User creating the new revision</param>
    /// <returns>The new revision</returns>
    Task<TakeoffRevision> CopyRevisionAsync(int sourceRevisionId, string? newDescription = null, int? userId = null);

    /// <summary>
    /// Deletes a revision (soft delete). Cannot delete the active revision.
    /// </summary>
    /// <param name="revisionId">The revision to delete</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteRevisionAsync(int revisionId);

    /// <summary>
    /// Generates the next revision code for a takeoff (A, B, C, ..., Z, AA, AB, ...)
    /// </summary>
    /// <param name="takeoffId">The takeoff ID</param>
    /// <returns>The next revision code</returns>
    Task<string> GetNextRevisionCodeAsync(int takeoffId);

    /// <summary>
    /// Gets or creates the active revision for a takeoff
    /// </summary>
    /// <param name="takeoffId">The takeoff ID</param>
    /// <param name="userId">User ID for creating if needed</param>
    /// <returns>The active revision</returns>
    Task<TakeoffRevision> GetOrCreateActiveRevisionAsync(int takeoffId, int? userId = null);
}
