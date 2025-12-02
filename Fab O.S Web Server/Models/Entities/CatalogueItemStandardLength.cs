using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities
{
    /// <summary>
    /// Represents a standard length available for a catalogue item
    /// One catalogue item can have multiple standard lengths
    /// </summary>
    [Table("CatalogueItemStandardLengths")]
    public class CatalogueItemStandardLength
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the catalogue item
        /// </summary>
        [Required]
        public int CatalogueItemId { get; set; }

        /// <summary>
        /// Standard length value in millimeters
        /// Examples: 6000, 6500, 8000, 12000
        /// Can also be text like "Cut to Length"
        /// </summary>
        [Required]
        [StringLength(50)]
        public string LengthValue { get; set; } = string.Empty;

        /// <summary>
        /// Unit of measurement (e.g., "mm", "m", "ft")
        /// Default: "mm"
        /// </summary>
        [StringLength(10)]
        public string Unit { get; set; } = "mm";

        /// <summary>
        /// Display order for UI (lower numbers first)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Whether this standard length is currently available
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Additional notes about this standard length
        /// </summary>
        [StringLength(200)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual CatalogueItem CatalogueItem { get; set; } = null!;
    }
}
