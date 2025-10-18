using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities
{
    [Table("NumberSeries")]
    public class NumberSeries
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Prefix { get; set; }

        [StringLength(20)]
        public string? Suffix { get; set; }

        [Required]
        public int CurrentNumber { get; set; }

        [Required]
        public int StartingNumber { get; set; } = 1;

        [Required]
        public int IncrementBy { get; set; } = 1;

        [Required]
        public int MinDigits { get; set; } = 4;

        [StringLength(100)]
        public string? Format { get; set; }

        public bool IncludeYear { get; set; }

        public bool IncludeMonth { get; set; }

        public bool IncludeCompanyCode { get; set; }

        public bool ResetYearly { get; set; }

        public bool ResetMonthly { get; set; }

        public int? LastResetYear { get; set; }

        public int? LastResetMonth { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        public bool AllowManualEntry { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? PreviewExample { get; set; }

        public DateTime LastUsed { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastModified { get; set; }

        public int? CreatedByUserId { get; set; }

        public int? LastModifiedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("LastModifiedByUserId")]
        public virtual User? LastModifiedBy { get; set; }

        // Helper method to generate next number
        public string GenerateNextNumber()
        {
            var numberPart = CurrentNumber.ToString().PadLeft(MinDigits, '0');
            var result = Prefix ?? "";

            if (IncludeYear)
            {
                result += DateTime.Now.Year;
                if (!string.IsNullOrEmpty(Prefix))
                    result += "-";
            }

            if (IncludeMonth)
            {
                result += DateTime.Now.ToString("MM");
                if (IncludeYear || !string.IsNullOrEmpty(Prefix))
                    result += "-";
            }

            result += numberPart;

            if (!string.IsNullOrEmpty(Suffix))
            {
                result += Suffix;
            }

            return result;
        }
    }
}