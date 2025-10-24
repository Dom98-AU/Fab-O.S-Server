using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces
{
    /// <summary>
    /// Service for managing PDF edit locks to prevent concurrent editing conflicts.
    /// Implements per-drawing locking with automatic timeout after 10 minutes of inactivity.
    /// </summary>
    public interface IPdfLockService
    {
        /// <summary>
        /// Attempts to acquire an edit lock for a drawing.
        /// Returns the lock if successful, or null if drawing is already locked by another session.
        /// </summary>
        /// <param name="drawingId">The PackageDrawing ID to lock</param>
        /// <param name="sessionId">Unique session/circuit ID (Blazor circuit)</param>
        /// <param name="userId">User attempting to acquire the lock</param>
        /// <param name="userName">User's display name (for "locked by X" banners)</param>
        /// <returns>PdfEditLock if successful, null if already locked</returns>
        Task<PdfEditLock?> TryAcquireLockAsync(int drawingId, string sessionId, int userId, string userName);

        /// <summary>
        /// Releases a lock by session ID.
        /// Called when user closes the PDF viewer or saves & closes.
        /// </summary>
        /// <param name="sessionId">Session ID that owns the lock</param>
        Task ReleaseLockAsync(string sessionId);

        /// <summary>
        /// Updates the heartbeat timestamp for a lock to indicate the session is still alive.
        /// Should be called every 30 seconds from the client.
        /// </summary>
        /// <param name="sessionId">Session ID that owns the lock</param>
        Task UpdateHeartbeatAsync(string sessionId);

        /// <summary>
        /// Updates the last activity timestamp for a lock.
        /// Called whenever user makes an annotation change (draw, edit, delete).
        /// Used for 10-minute inactivity timeout.
        /// </summary>
        /// <param name="sessionId">Session ID that owns the lock</param>
        Task UpdateActivityAsync(string sessionId);

        /// <summary>
        /// Gets the currently active lock for a drawing, if any.
        /// Used to check if a drawing is locked and by whom.
        /// </summary>
        /// <param name="drawingId">The PackageDrawing ID</param>
        /// <returns>Active lock if exists, null otherwise</returns>
        Task<PdfEditLock?> GetActiveLockAsync(int drawingId);

        /// <summary>
        /// Releases all stale locks (heartbeat older than 30 seconds or inactive for 10+ minutes).
        /// Called by background service every 30 seconds.
        /// </summary>
        Task ReleaseStaleLocksAsync();

        /// <summary>
        /// Force-releases a lock (admin function).
        /// Logs the force-release action for audit trail.
        /// </summary>
        /// <param name="lockId">Lock ID to release</param>
        /// <param name="adminUserId">Admin user performing the action</param>
        Task ForceReleaseLockAsync(int lockId, int adminUserId);

        /// <summary>
        /// Gets all currently active locks across all drawings.
        /// Used for admin dashboard to see who's editing what.
        /// </summary>
        /// <returns>List of all active locks</returns>
        Task<List<PdfEditLock>> GetAllActiveLocksAsync();

        /// <summary>
        /// Checks if a session has an active lock for a specific drawing.
        /// </summary>
        /// <param name="sessionId">Session ID to check</param>
        /// <param name="drawingId">Drawing ID to check</param>
        /// <returns>True if session has active lock, false otherwise</returns>
        Task<bool> HasActiveLockAsync(string sessionId, int drawingId);
    }
}
