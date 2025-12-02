using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FabOS.WebServer.Models.Entities;

// Catalogues - Named collections of catalogue items (Database Catalogue + Custom Catalogues)
[Table("Catalogues")]
public class Catalogue
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty; // "Database Catalogue", "Custom Steel Products"

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsSystemCatalogue { get; set; } = false; // true = Database Catalogue (cannot delete)

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public int? ModifiedBy { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;

    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }

    [ForeignKey(nameof(ModifiedBy))]
    public virtual User? Modifier { get; set; }
    public virtual ICollection<CatalogueItem> Items { get; set; } = new List<CatalogueItem>();
}

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

    // Catalogue relationship (links item to a catalogue)
    [Required]
    public int CatalogueId { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Catalogue Catalogue { get; set; } = null!;
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    public virtual ICollection<AssemblyComponent> AssemblyComponents { get; set; } = new List<AssemblyComponent>();
    public virtual ICollection<CatalogueItemStandardLength> StandardLengthsList { get; set; } = new List<CatalogueItemStandardLength>();
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

    [StringLength(500)]
    public string? QRCode { get; set; } // QR code for shop floor scanning

    // FabMate Integration
    public int? PurchaseOrderLineId { get; set; } // Which PO line this came from
    public int? AssemblyId { get; set; } // What assembly this will be used for
    public int? WorkOrderId { get; set; } // Which work order is using this item

    // Drawing part reference tracking (flows from CAD files)
    [StringLength(100)]
    public string? PartReference { get; set; } // "P1", "B1", "Col-A1" from drawing

    public int? DrawingPartId { get; set; } // Link back to DrawingPart
    public int? DrawingRevisionId { get; set; } // Which IFC/DXF/SMLX revision

    // Geometry fields (copied from DrawingPart when material is received)
    // Basic Dimensions (mm)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Length { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Width { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Thickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Diameter { get; set; }

    // Structural Steel Specific (Beams/Channels/UCs/UBs)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FlangeThickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FlangeWidth { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WebThickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WebDepth { get; set; }

    // Hollow Section Specific (SHS/RHS/CHS)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OutsideDiameter { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WallThickness { get; set; }

    // Angle Specific (Equal/Unequal Angles)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? LegA { get; set; } // First leg

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LegB { get; set; } // Second leg

    [StringLength(20)]
    public string Status { get; set; } = "Available"; // Available, Reserved, InUse, Consumed, Scrapped, Quarantined

    [StringLength(20)]
    public string? ConditionOnReceipt { get; set; } // Good, Damaged, Incorrect, ShortShipped

    [StringLength(2000)]
    public string? ReceiptNotes { get; set; } // Notes from goods receipt

    public int? ReceivedByUserId { get; set; } // Who received the item

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
    public virtual Assembly? Assembly { get; set; }
    public virtual WorkOrder? WorkOrder { get; set; }
    public virtual User? ReceivedByUser { get; set; }
    public virtual DrawingPart? DrawingPart { get; set; }
    public virtual DrawingRevision? DrawingRevision { get; set; }
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

    // FabMate Integration - Link to WorkPackage
    public int? WorkPackageId { get; set; }

    [Required]
    public int QuantityRequired { get; set; } = 1; // How many of this assembly to build

    // QDocs Integration - Link to IFC drawing (optional)
    public int? QDocsAssemblyId { get; set; } // Link to QDocs Assembly entity
    public int? IFCRevisionId { get; set; } // Which IFC revision this was created from

    [StringLength(20)]
    public string Source { get; set; } = "Direct"; // Direct (Excel upload) or QDocs (from IFC)

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
    public virtual WorkPackage? WorkPackage { get; set; }
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

// ==========================================
// PURCHASE ORDERS
// ==========================================

[Table("PurchaseOrders")]
public class PurchaseOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string PONumber { get; set; } = string.Empty; // PO-2025-0001

    // Link to WorkPackage (what this PO is for)
    [Required]
    public int WorkPackageId { get; set; }

    // Supplier information
    [Required]
    public int SupplierId { get; set; } // Link to Customer entity (suppliers are customers too)

    [StringLength(200)]
    public string? SupplierReference { get; set; }

    // Dates
    [Required]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public DateTime? RequiredDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    // Status
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Sent, Acknowledged, InTransit, PartiallyReceived, Received, Cancelled

    // Totals (calculated from line items)
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalValue { get; set; }

    // QR Code for goods receipt
    [StringLength(500)]
    public string? QRCode { get; set; }

    // ITP Integration (QDocs module)
    [Required]
    public bool CreateITP { get; set; } = false; // Was ITP created with this PO?

    public int? ITPId { get; set; } // Link to QDocs InspectionTestPlan (if material requires ITP inspection)

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Audit fields
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    [Required]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public int? LastModifiedBy { get; set; }

    [Required]
    public int CompanyId { get; set; }

    // Navigation properties
    [ForeignKey("WorkPackageId")]
    public virtual WorkPackage WorkPackage { get; set; } = null!;

    [ForeignKey("SupplierId")]
    public virtual Customer Supplier { get; set; } = null!;

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("LastModifiedBy")]
    public virtual User? LastModifiedByUser { get; set; }

    // Note: ITP navigation property commented out to avoid circular dependency with QDocs module
    // Access ITP via ITPId when needed: context.InspectionTestPlans.Find(po.ITPId)
    // [ForeignKey("ITPId")]
    // public virtual InspectionTestPlan? ITP { get; set; }

    public virtual ICollection<PurchaseOrderLineItem> LineItems { get; set; } = new List<PurchaseOrderLineItem>();
}

[Table("PurchaseOrderLineItems")]
public class PurchaseOrderLineItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseOrderId { get; set; }

    [Required]
    public int LineNumber { get; set; }

    // What we're ordering
    [Required]
    public int CatalogueItemId { get; set; }

    public int? AssemblyId { get; set; } // Which assembly needs this material

    public int? DrawingPartId { get; set; } // Link to the DrawingPart being ordered

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    // Quantities
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityOrdered { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal QuantityReceived { get; set; } = 0;

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;

    // Pricing
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    // Status
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, PartiallyReceived, Received, Cancelled

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("PurchaseOrderId")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    [ForeignKey("CatalogueItemId")]
    public virtual CatalogueItem CatalogueItem { get; set; } = null!;

    [ForeignKey("AssemblyId")]
    public virtual Assembly? Assembly { get; set; }

    [ForeignKey("DrawingPartId")]
    public virtual DrawingPart? DrawingPart { get; set; }
}

// ============================================================================
// ITP Material Inspection (FabMate Module)
// Maintains module isolation - References QDocs ITP by ID only
// ============================================================================

/// <summary>
/// ITP Material Receipt Inspection - Records material quality checks during goods receipt
/// Part of FabMate module but integrates with QDocs ITP system
/// </summary>
[Table("ITPMaterialInspections")]
public class ITPMaterialInspection
{
    [Key]
    public int Id { get; set; }

    // QDocs Integration (Foreign Key Only - No Navigation Property)
    [Required]
    public int ITPId { get; set; } // Links to QDocs InspectionTestPlan

    // FabMate References
    [Required]
    public int InventoryItemId { get; set; } // The received material being inspected

    public int? PurchaseOrderLineId { get; set; } // Which PO line was received (optional for traceability)

    // Inspection Details
    [Required]
    public DateTime InspectionDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string InspectorName { get; set; } = string.Empty;

    public int? InspectorUserId { get; set; }

    // Dimensional Inspection (all measurements in mm)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MeasuredLength { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MeasuredWidth { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MeasuredThickness { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MeasuredDiameter { get; set; }

    public bool? DimensionsPass { get; set; }

    [StringLength(1000)]
    public string? DimensionNotes { get; set; }

    // Visual Inspection
    public bool? VisualInspectionPass { get; set; }

    [StringLength(500)]
    public string? DamageObserved { get; set; } // "None", "Dents", "Rust", "Scratches", "Bent", etc.

    [StringLength(2000)]
    public string? VisualInspectionNotes { get; set; }

    // Surface Finish
    [StringLength(20)]
    public string? SurfaceFinish { get; set; } // "Good", "Fair", "Poor"

    [StringLength(1000)]
    public string? SurfaceFinishNotes { get; set; }

    // Mill Certificates
    public bool? MillCertificatesReceived { get; set; }

    [StringLength(100)]
    public string? MillCertificateReference { get; set; }

    [StringLength(2000)]
    public string? CertificateNotes { get; set; }

    // Photos/Attachments (JSON array of file paths or URLs)
    [StringLength(4000)]
    public string? PhotoUrls { get; set; } // e.g., ["uploads/inspections/2025/photo1.jpg", "uploads/inspections/2025/photo2.jpg"]

    // Digital Signature (Base64 encoded image data)
    [Column(TypeName = "nvarchar(MAX)")]
    public string? InspectorSignature { get; set; }

    // Overall Result
    [Required]
    [StringLength(20)]
    public string InspectionResult { get; set; } = "Pending"; // "Pass", "Fail", "Conditional", "Pending"

    [StringLength(2000)]
    public string? InspectionNotes { get; set; }

    // Audit
    [Required]
    public int CompanyId { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // Navigation Properties (FabMate module only - NO QDocs navigation to maintain isolation)
    [ForeignKey("InventoryItemId")]
    public virtual InventoryItem InventoryItem { get; set; } = null!;

    [ForeignKey("PurchaseOrderLineId")]
    public virtual PurchaseOrderLineItem? PurchaseOrderLineItem { get; set; }

    [ForeignKey("InspectorUserId")]
    public virtual User? InspectorUser { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    // Note: ITP navigation property intentionally omitted to maintain module isolation
    // Access ITP via: context.InspectionTestPlans.Find(inspection.ITPId)
}
