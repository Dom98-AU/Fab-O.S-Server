using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.Entities
{
    /// <summary>
    /// Represents an edit lock on a PDF drawing to prevent concurrent editing conflicts.
    /// Implements per-drawing locking with automatic timeout after 10 minutes of inactivity.
    /// </summary>
    public class PdfEditLock
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The drawing being locked for editing
        /// </summary>
        [Required]
        public int PackageDrawingId { get; set; }

        /// <summary>
        /// Blazor circuit ID - unique identifier for this browser tab/session
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// User who acquired the lock
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// User's display name (cached for quick display in "locked by X" banners)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// When the lock was first acquired
        /// </summary>
        [Required]
        public DateTime LockedAt { get; set; }

        /// <summary>
        /// Last heartbeat ping from the client (updated every 30 seconds)
        /// Used to detect disconnected sessions
        /// </summary>
        [Required]
        public DateTime LastHeartbeat { get; set; }

        /// <summary>
        /// Last time user made an annotation change (draw, edit, delete)
        /// Used for 10-minute inactivity timeout
        /// </summary>
        [Required]
        public DateTime LastActivityAt { get; set; }

        /// <summary>
        /// Whether this lock is currently active
        /// Set to false when manually released or admin force-unlocks
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual PackageDrawing? PackageDrawing { get; set; }
        public virtual User? User { get; set; }
    }
}
