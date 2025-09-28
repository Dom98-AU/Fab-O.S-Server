using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// Catalogue Items - Master list of ALL purchasable/usable items (7,107 items ready)
[Table("CatalogueItems")]
public class CatalogueItem
{
    [Key]
    public int Id { get; set; }

    // Primary Identification
    [Required]
    [StringLength(50)]
    public string ItemCode { get; set; } = string.Empty; // Format: PP350MS-001

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty; // Full formatted description

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty; // 'Plates', 'Hollow Sections', 'Bars', 'Pipes', etc.

    [Required]
    [StringLength(50)]
    public string Material { get; set; } = string.Empty; // 'Mild Steel', 'Stainless Steel', 'Aluminum'

    [StringLength(50)]
    public string? Profile { get; set; } // '150UB', '100UC', '100x100SHS', '21.3CHS'

    // Primary Dimensions (mm)
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Length_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Width_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Height_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Depth_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Thickness_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Diameter_mm { get; set; }

    // Pipe/Tube Specific Dimensions
    [Column(TypeName = "decimal(10,2)")]
    public decimal? OD_mm { get; set; } // Outside diameter

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ID_mm { get; set; } // Inside diameter

    [Column(TypeName = "decimal(10,2)")]
    public decimal? WallThickness_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Wall_mm { get; set; } // Alternative wall thickness field

    [StringLength(20)]
    public string? NominalBore { get; set; } // 'NB15', 'NB20', etc.

    [StringLength(20)]
    public string? ImperialEquiv { get; set; } // '1/2"', '3/4"', etc.

    // Beam/Column Specific
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Web_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Flange_mm { get; set; }

    // Angle Specific
    [Column(TypeName = "decimal(10,2)")]
    public decimal? A_mm { get; set; } // First leg

    [Column(TypeName = "decimal(10,2)")]
    public decimal? B_mm { get; set; } // Second leg

    // Size Fields
    [StringLength(50)]
    public string? Size_mm { get; set; } // Can be string for complex sizes

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Size { get; set; } // Numeric size

    [StringLength(20)]
    public string? Size_inch { get; set; } // Imperial size

    // Sheet/Plate Specific
    [Column(TypeName = "decimal(10,2)")]
    public decimal? BMT_mm { get; set; } // Base Metal Thickness

    [Column(TypeName = "decimal(10,2)")]
    public decimal? BaseThickness_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RaisedThickness_mm { get; set; } // For tread plates

    // Weight and Mass
    [Column(TypeName = "decimal(10,3)")]
    public decimal? Mass_kg_m { get; set; } // kg per meter

    [Column(TypeName = "decimal(10,3)")]
    public decimal? Mass_kg_m2 { get; set; } // kg per square meter

    [Column(TypeName = "decimal(10,3)")]
    public decimal? Mass_kg_length { get; set; } // kg per standard length

    [Column(TypeName = "decimal(10,3)")]
    public decimal? Weight_kg { get; set; } // Total weight per unit

    [Column(TypeName = "decimal(10,3)")]
    public decimal? Weight_kg_m2 { get; set; } // Weight per square meter

    // Surface Area (for coating calculations)
    [Column(TypeName = "decimal(10,4)")]
    public decimal? SurfaceArea_m2 { get; set; } // Total surface area

    [Column(TypeName = "decimal(10,4)")]
    public decimal? SurfaceArea_m2_per_m { get; set; } // Surface area per meter

    [Column(TypeName = "decimal(10,4)")]
    public decimal? SurfaceArea_m2_per_m2 { get; set; } // Surface area per square meter

    [StringLength(50)]
    public string? Surface { get; set; } // Surface type/texture

    // Material Specifications
    [StringLength(50)]
    public string? Standard { get; set; } // 'AS/NZS 3678', 'AS/NZS 3679.1', 'AS 1163', 'AS 1074'

    [StringLength(50)]
    public string? Grade { get; set; } // '250', '300', '350', 'C350', '304', '316'

    [StringLength(50)]
    public string? Alloy { get; set; } // '5083', '6061' for aluminum

    [StringLength(50)]
    public string? Temper { get; set; } // 'H116', 'T6' for aluminum

    // Finish Specifications
    [StringLength(100)]
    public string? Finish { get; set; } // 'Black/Mill', '2B', 'No.1', 'Hot Dip Galvanized', etc.

    [StringLength(50)]
    public string? Finish_Standard { get; set; } // Additional finish standard

    [StringLength(50)]
    public string? Coating { get; set; } // 'Z275' for galvanized

    // Standard Lengths and Availability
    [StringLength(200)]
    public string? StandardLengths { get; set; } // '6000,8000,12000' comma-separated

    public int? StandardLength_m { get; set; } // Standard length in meters

    [StringLength(20)]
    public string? Cut_To_Size { get; set; } // 'Available', 'Not Available'

    // Product Type and Features
    [StringLength(50)]
    public string? Type { get; set; } // 'L' (Light), 'M' (Medium), 'H' (Heavy) for pipes

    [StringLength(50)]
    public string? ProductType { get; set; } // 'Tread Plate', etc.

    [StringLength(50)]
    public string? Pattern { get; set; } // '5-Bar' for tread plates

    [StringLength(500)]
    public string? Features { get; set; } // Special features or notes

    [StringLength(50)]
    public string? Tolerance { get; set; } // 'H9' for precision items

    // Pipe Classifications
    [StringLength(50)]
    public string? Pressure { get; set; } // 'Class L', 'Class M', 'Class H'

    // Supplier Information
    [StringLength(100)]
    public string? SupplierCode { get; set; } // Supplier's product code

    public int? PackQty { get; set; } // Package quantity

    [StringLength(20)]
    public string? Unit { get; set; } = "EA"; // 'EA', 'LENGTH', 'SHEET'

    // Compliance and Ratings
    [StringLength(200)]
    public string? Compliance { get; set; } // 'AS1657 Compliant'

    [StringLength(50)]
    public string? Duty_Rating { get; set; } // 'Light Duty', 'Heavy Duty'

    // Status and Metadata
    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public virtual ICollection<AssemblyComponent> AssemblyComponents { get; set; } = new List<AssemblyComponent>();
    public virtual GratingSpecification? GratingSpecification { get; set; }
}

// Grating-Specific Fields (Extended table for complex grating products)
[Table("GratingSpecifications")]
public class GratingSpecification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CatalogueItemId { get; set; }

    // Grating-specific dimensions
    [Column(TypeName = "decimal(10,2)")]
    public decimal? LoadBar_Height_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? LoadBar_Spacing_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? LoadBar_Thickness_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CrossBar_Spacing_mm { get; set; }

    // Panel dimensions
    [Column(TypeName = "decimal(10,2)")]
    public decimal? Standard_Panel_Length_mm { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Standard_Panel_Width_mm { get; set; }

    // Additional properties
    [StringLength(50)]
    public string? Dimensions { get; set; } // '25x25', '10x3'

    // Navigation properties
    public virtual CatalogueItem CatalogueItem { get; set; } = null!;
}

// Inventory Items - Physical stock in warehouses
[Table("InventoryItems")]
public class InventoryItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CatalogueItemId { get; set; }

    [Required]
    [StringLength(50)]
    public string InventoryCode { get; set; } = string.Empty; // INV-2025-000001

    // Location tracking
    [StringLength(50)]
    public string? WarehouseCode { get; set; }

    [StringLength(50)]
    public string? Location { get; set; }

    [StringLength(50)]
    public string? BinLocation { get; set; }

    // Quantity tracking
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityOnHand { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? QuantityReserved { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? QuantityAvailable { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    // Traceability information
    [StringLength(50)]
    public string? LotNumber { get; set; }

    [StringLength(50)]
    public string? HeatNumber { get; set; }

    [StringLength(100)]
    public string? MillCertificate { get; set; }

    [StringLength(100)]
    public string? Supplier { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    // Costing
    [Column(TypeName = "decimal(18,4)")]
    public decimal? UnitCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? TotalCost { get; set; }

    // Status
    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual CatalogueItem CatalogueItem { get; set; } = null!;
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}

// Inventory Transactions (movements, issues, receipts)
[Table("InventoryTransactions")]
public class InventoryTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    [Required]
    [StringLength(50)]
    public string TransactionNumber { get; set; } = string.Empty; // TXN-2025-000001

    [Required]
    [StringLength(50)]
    public string TransactionType { get; set; } = string.Empty; // Receipt, Issue, Transfer, Adjustment

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Reference { get; set; } // PO#, WO#, etc.

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    public int? UserId { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual Company Company { get; set; } = null!;
}

// Assemblies - Manufactured items containing other items
[Table("Assemblies")]
public class Assembly
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string AssemblyNumber { get; set; } = string.Empty; // ASM-2025-000001

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Version { get; set; }

    [StringLength(100)]
    public string? DrawingNumber { get; set; }

    // Hierarchy
    public int? ParentAssemblyId { get; set; }

    // Build information
    [Column(TypeName = "decimal(18,4)")]
    public decimal? EstimatedWeight { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedHours { get; set; }

    // Status
    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    public virtual Assembly? ParentAssembly { get; set; }
    public virtual ICollection<Assembly> SubAssemblies { get; set; } = new List<Assembly>();
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<AssemblyComponent> Components { get; set; } = new List<AssemblyComponent>();
}

// Assembly Components (Bill of Materials)
[Table("AssemblyComponents")]
public class AssemblyComponent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AssemblyId { get; set; }

    // Component can be either a CatalogueItem or another Assembly
    public int? CatalogueItemId { get; set; }
    public int? ComponentAssemblyId { get; set; }

    [Required]
    [StringLength(100)]
    public string ComponentReference { get; set; } = string.Empty; // Reference on drawing

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? AlternateItems { get; set; } // Comma-separated alternate ItemCodes

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public virtual Assembly Assembly { get; set; } = null!;
    public virtual CatalogueItem? CatalogueItem { get; set; }
    public virtual Assembly? ComponentAssembly { get; set; }
}
