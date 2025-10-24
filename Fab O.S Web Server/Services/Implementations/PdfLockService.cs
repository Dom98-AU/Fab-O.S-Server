using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations
{
    /// <summary>
    /// Service for managing PDF edit locks to prevent concurrent editing conflicts.
    /// Implements per-drawing locking with automatic timeout after 10 minutes of inactivity.
    /// </summary>
    public class PdfLockService : IPdfLockService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PdfLockService> _logger;

        // Constants for timeout configuration
        private const int HEARTBEAT_TIMEOUT_SECONDS = 30;   // Release lock if no heartbeat for 30 seconds
        private const int INACTIVITY_TIMEOUT_MINUTES = 10;  // Release lock if no activity for 10 minutes

        public PdfLockService(
            ApplicationDbContext context,
            ILogger<PdfLockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PdfEditLock?> TryAcquireLockAsync(int drawingId, string sessionId, int userId, string userName)
        {
            _logger.LogInformation("[PdfLockService] Attempting to acquire lock for DrawingId={DrawingId}, SessionId={SessionId}, UserId={UserId}",
                drawingId, sessionId, userId);

            // First, clean up any stale locks
            await ReleaseStaleLocksAsync();

            // Check if this session already has the lock
            var existingSessionLock = await _context.PdfEditLocks
                .FirstOrDefaultAsync(l => l.SessionId == sessionId && l.PackageDrawingId == drawingId && l.IsActive);

            if (existingSessionLock != null)
            {
                _logger.LogInformation("[PdfLockService] ✓ Session already has lock: LockId={LockId}", existingSessionLock.Id);
                return existingSessionLock;
            }

            // Check if drawing is locked by another session
            var existingLock = await _context.PdfEditLocks
                .FirstOrDefaultAsync(l => l.PackageDrawingId == drawingId && l.IsActive);

            if (existingLock != null)
            {
                _logger.LogWarning("[PdfLockService] ✗ Drawing already locked by SessionId={SessionId}, User={UserName}",
                    existingLock.SessionId, existingLock.UserName);
                return null;
            }

            // Create new lock
            var now = DateTime.UtcNow;
            var newLock = new PdfEditLock
            {
                PackageDrawingId = drawingId,
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                LockedAt = now,
                LastHeartbeat = now,
                LastActivityAt = now,
                IsActive = true
            };

            _context.PdfEditLocks.Add(newLock);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[PdfLockService] ✓ Lock acquired: LockId={LockId}, DrawingId={DrawingId}, User={UserName}",
                newLock.Id, drawingId, userName);

            return newLock;
        }

        public async Task ReleaseLockAsync(string sessionId)
        {
            _logger.LogInformation("[PdfLockService] Releasing lock for SessionId={SessionId}", sessionId);

            var pdfLock = await _context.PdfEditLocks
                .FirstOrDefaultAsync(l => l.SessionId == sessionId && l.IsActive);

            if (pdfLock != null)
            {
                pdfLock.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("[PdfLockService] ✓ Lock released: LockId={LockId}, DrawingId={DrawingId}",
                    pdfLock.Id, pdfLock.PackageDrawingId);
            }
            else
            {
                _logger.LogWarning("[PdfLockService] No active lock found for SessionId={SessionId}", sessionId);
            }
        }

        public async Task UpdateHeartbeatAsync(string sessionId)
        {
            var pdfLock = await _context.PdfEditLocks
                .FirstOrDefaultAsync(l => l.SessionId == sessionId && l.IsActive);

            if (pdfLock != null)
            {
                pdfLock.LastHeartbeat = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogDebug("[PdfLockService] Heartbeat updated for SessionId={SessionId}, LockId={LockId}",
                    sessionId, pdfLock.Id);
            }
        }

        public async Task UpdateActivityAsync(string sessionId)
        {
            var pdfLock = await _context.PdfEditLocks
                .FirstOrDefaultAsync(l => l.SessionId == sessionId && l.IsActive);

            if (pdfLock != null)
            {
                pdfLock.LastActivityAt = DateTime.UtcNow;
                pdfLock.LastHeartbeat = DateTime.UtcNow; // Also update heartbeat
                await _context.SaveChangesAsync();

                _logger.LogDebug("[PdfLockService] Activity updated for SessionId={SessionId}, LockId={LockId}",
                    sessionId, pdfLock.Id);
            }
        }

        public async Task<PdfEditLock?> GetActiveLockAsync(int drawingId)
        {
            return await _context.PdfEditLocks
                .Include(l => l.User)
                .Include(l => l.PackageDrawing)
                .FirstOrDefaultAsync(l => l.PackageDrawingId == drawingId && l.IsActive);
        }

        public async Task ReleaseStaleLocksAsync()
        {
            var now = DateTime.UtcNow;
            var heartbeatCutoff = now.AddSeconds(-HEARTBEAT_TIMEOUT_SECONDS);
            var inactivityCutoff = now.AddMinutes(-INACTIVITY_TIMEOUT_MINUTES);

            // Find stale locks (either no heartbeat for 30 sec OR no activity for 10 min)
            var staleLocks = await _context.PdfEditLocks
                .Where(l => l.IsActive &&
                    (l.LastHeartbeat < heartbeatCutoff || l.LastActivityAt < inactivityCutoff))
                .ToListAsync();

            if (staleLocks.Any())
            {
                _logger.LogInformation("[PdfLockService] Releasing {Count} stale locks", staleLocks.Count);

                foreach (var pdfLock in staleLocks)
                {
                    pdfLock.IsActive = false;

                    var reason = pdfLock.LastHeartbeat < heartbeatCutoff
                        ? "heartbeat timeout (connection lost)"
                        : "inactivity timeout (10 minutes)";

                    _logger.LogInformation("[PdfLockService] Released stale lock: LockId={LockId}, DrawingId={DrawingId}, User={UserName}, Reason={Reason}",
                        pdfLock.Id, pdfLock.PackageDrawingId, pdfLock.UserName, reason);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task ForceReleaseLockAsync(int lockId, int adminUserId)
        {
            _logger.LogWarning("[PdfLockService] Force releasing lock: LockId={LockId}, AdminUserId={AdminUserId}",
                lockId, adminUserId);

            var pdfLock = await _context.PdfEditLocks
                .FirstOrDefaultAsync(l => l.Id == lockId);

            if (pdfLock != null)
            {
                pdfLock.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogWarning("[PdfLockService] ✓ Lock force-released: LockId={LockId}, DrawingId={DrawingId}, User={UserName}, AdminUserId={AdminUserId}",
                    pdfLock.Id, pdfLock.PackageDrawingId, pdfLock.UserName, adminUserId);

                // TODO: Consider adding audit log entry for force-release actions
            }
            else
            {
                _logger.LogWarning("[PdfLockService] Lock not found for force release: LockId={LockId}", lockId);
            }
        }

        public async Task<List<PdfEditLock>> GetAllActiveLocksAsync()
        {
            return await _context.PdfEditLocks
                .Include(l => l.User)
                .Include(l => l.PackageDrawing)
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.LockedAt)
                .ToListAsync();
        }

        public async Task<bool> HasActiveLockAsync(string sessionId, int drawingId)
        {
            return await _context.PdfEditLocks
                .AnyAsync(l => l.SessionId == sessionId &&
                              l.PackageDrawingId == drawingId &&
                              l.IsActive);
        }
    }
}
