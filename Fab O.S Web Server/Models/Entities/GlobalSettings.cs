using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities
{
    [Table("GlobalSettings")]
    public class GlobalSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SettingType { get; set; } // string, int, bool, json

        [StringLength(50)]
        public string? Category { get; set; } // System, Security, Display, etc.

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsSystemSetting { get; set; } // Cannot be modified by users

        public bool RequiresRestart { get; set; } // Requires system restart to take effect

        public bool IsEncrypted { get; set; } // Value is encrypted in database

        [StringLength(500)]
        public string? ValidationRule { get; set; } // JSON validation rules

        public string? DefaultValue { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }

        public DateTime LastModified { get; set; }

        public int? LastModifiedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("LastModifiedByUserId")]
        public virtual User? LastModifiedBy { get; set; }

        // Helper method to get typed value
        public T GetValue<T>()
        {
            if (string.IsNullOrEmpty(SettingValue))
                return default(T)!;

            try
            {
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)bool.Parse(SettingValue);
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.Parse(SettingValue);
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)SettingValue;
                }
                else
                {
                    // For complex types, assume JSON
                    return System.Text.Json.JsonSerializer.Deserialize<T>(SettingValue)!;
                }
            }
            catch
            {
                return default(T)!;
            }
        }

        // Helper method to set typed value
        public void SetValue<T>(T value)
        {
            if (value == null)
            {
                SettingValue = string.Empty;
                return;
            }

            if (typeof(T) == typeof(string) || typeof(T) == typeof(bool) || typeof(T) == typeof(int))
            {
                SettingValue = value.ToString()!;
            }
            else
            {
                // For complex types, serialize to JSON
                SettingValue = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }
    }
}