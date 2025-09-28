using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("Companies")]
public class Company
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    [StringLength(50)]
    public string SubscriptionLevel { get; set; } = "Standard";

    [Required]
    public int MaxUsers { get; set; } = 10;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? Domain { get; set; }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<TraceDrawing> TraceDrawings { get; set; } = new List<TraceDrawing>();
    public virtual ICollection<MachineCenter> MachineCenters { get; set; } = new List<MachineCenter>();
}