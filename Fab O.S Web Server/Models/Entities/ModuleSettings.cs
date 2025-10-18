using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities
{
    [Table("ModuleSettings")]
    public class ModuleSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ModuleName { get; set; } = string.Empty;

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SettingType { get; set; } // string, int, bool, json

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsUserSpecific { get; set; }

        public int? UserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }

        public DateTime LastModified { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? LastModifiedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("LastModifiedByUserId")]
        public virtual User? LastModifiedBy { get; set; }
    }
}