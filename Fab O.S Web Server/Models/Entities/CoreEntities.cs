using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

[Table("Customers")]
public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CompanyId { get; set; } = 1;

    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = "Default Company";

    // Audit fields - nullable until user system is implemented
    public int? CreatedById { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    // Australian Business Number
    [StringLength(20)]
    public string? ABN { get; set; }

    // Legacy fields - kept for backward compatibility
    [StringLength(200)]
    public string? ContactPerson { get; set; }

    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    // Additional fields
    [StringLength(100)]
    public string? Website { get; set; }

    [StringLength(50)]
    public string? Industry { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
}

[Table("CustomerContacts")]
public class CustomerContact
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Title { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(20)]
    public string? MobileNumber { get; set; }

    // Address fields
    [StringLength(200)]
    public string? AddressLine1 { get; set; }

    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    // Google Places API fields
    [StringLength(500)]
    public string? GooglePlaceId { get; set; }

    [StringLength(500)]
    public string? FormattedAddress { get; set; }

    public bool InheritCustomerAddress { get; set; } = true;

    [Required]
    public bool IsPrimary { get; set; } = false;

    [Required]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;
}

[Table("CustomerAddresses")]
public class CustomerAddress
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    [StringLength(50)]
    public string AddressType { get; set; } = "Main"; // Main, Billing, Shipping, Site

    [Required]
    [StringLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = "Australia";

    // Google Places API fields
    [StringLength(500)]
    public string? GooglePlaceId { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    [StringLength(500)]
    public string? FormattedAddress { get; set; }

    [Required]
    public bool IsPrimary { get; set; } = false;

    [Required]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } = null!;
}

[Table("EfficiencyRates")]
public class EfficiencyRate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Rate { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
}

[Table("RoutingTemplates")]
public class RoutingTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
    public virtual ICollection<RoutingOperation> RoutingOperations { get; set; } = new List<RoutingOperation>();
}

[Table("RoutingOperations")]
public class RoutingOperation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RoutingTemplateId { get; set; }

    [Required]
    [StringLength(100)]
    public string OperationName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int Sequence { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SetupTime { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RunTime { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public DateTime LastModified { get; set; }

    // Navigation properties
    [ForeignKey("RoutingTemplateId")]
    public virtual RoutingTemplate RoutingTemplate { get; set; } = null!;
}
