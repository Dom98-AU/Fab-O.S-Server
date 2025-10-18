using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

/// <summary>
/// Represents a revision of a takeoff. Takeoffs can have multiple revisions (A, B, C, etc.)
/// to track changes over time. Each revision contains its own set of packages and files.
/// </summary>
[Table("TakeoffRevisions")]
public class TakeoffRevision
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The takeoff this revision belongs to
    /// </summary>
    [Required]
    public int TakeoffId { get; set; }

    /// <summary>
    /// Revision code (A, B, C, etc.)
    /// </summary>
    [Required]
    [StringLength(5)]
    public string RevisionCode { get; set; } = string.Empty;

    /// <summary>
    /// Only one revision can be active per takeoff. This is the "working" revision.
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Description of what changed in this revision
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// If this revision was copied from another, track the source revision
    /// </summary>
    public int? CopiedFromRevisionId { get; set; }

    /// <summary>
    /// User who created this revision
    /// </summary>
    public int? CreatedBy { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    [ForeignKey("TakeoffId")]
    public virtual TraceDrawing Takeoff { get; set; } = null!;

    [ForeignKey("CopiedFromRevisionId")]
    public virtual TakeoffRevision? CopiedFromRevision { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    /// Packages that belong to this revision
    /// </summary>
    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
}
