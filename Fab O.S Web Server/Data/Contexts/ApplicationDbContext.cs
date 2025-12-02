using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Models.ViewState;
using FabOS.WebServer.Models.Calibration;

namespace FabOS.WebServer.Data.Contexts;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<PackageDrawing> PackageDrawings { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerContact> CustomerContacts { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }
    public DbSet<EfficiencyRate> EfficiencyRates { get; set; }
    public DbSet<RoutingTemplate> RoutingTemplates { get; set; }
    public DbSet<RoutingOperation> RoutingOperations { get; set; }

    // Steel estimation entities
    public DbSet<Takeoff> Takeoffs { get; set; } // Previously TraceDrawings - renamed for clarity
    public DbSet<Takeoff> TraceDrawings => Takeoffs; // Backward compatibility alias
    public DbSet<TakeoffRevision> TakeoffRevisions { get; set; }
    public DbSet<TraceMeasurement> TraceMeasurements { get; set; }
    public DbSet<TraceBeamDetection> TraceBeamDetections { get; set; }
    public DbSet<TraceTakeoffItem> TraceTakeoffItems { get; set; }
    public DbSet<WeldingConnection> WeldingConnections { get; set; }

    // Trace Module Entities
    public DbSet<TraceRecord> TraceRecords { get; set; }
    public DbSet<TraceMaterial> TraceMaterials { get; set; }
    public DbSet<TraceProcess> TraceProcesses { get; set; }
    public DbSet<TraceParameter> TraceParameters { get; set; }
    public DbSet<TraceAssembly> TraceAssemblies { get; set; }
    public DbSet<TraceComponent> TraceComponents { get; set; }
    public DbSet<TraceDocument> TraceDocuments { get; set; }
    public DbSet<TraceTakeoff> TraceTakeoffs { get; set; }
    public DbSet<TraceTakeoffMeasurement> TraceTakeoffMeasurements { get; set; }
    public DbSet<TraceMaterialCatalogueLink> TraceMaterialCatalogueLinks { get; set; }
    public DbSet<TraceTakeoffAnnotation> TraceTakeoffAnnotations { get; set; }
    public DbSet<SurfaceCoating> SurfaceCoatings { get; set; }

    // Calibration entities
    public DbSet<CalibrationData> Calibrations { get; set; }

    // NEW: Workflow entities (Estimation → Order → WorkOrder system)
    public DbSet<Estimation> Estimations { get; set; }
    public DbSet<EstimationPackage> EstimationPackages { get; set; }
    public DbSet<Order> Orders { get; set; }

    // FabMate Module: Order → WorkPackage → WorkOrder
    public DbSet<WorkPackage> WorkPackages { get; set; }
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<Resource> Resources { get; set; }

    // FabMate Module: Business Central-style Routing System
    public DbSet<Routing> Routings { get; set; }
    public DbSet<RoutingLine> RoutingLines { get; set; }
    public DbSet<WorkOrderRouting> WorkOrderRoutings { get; set; }
    public DbSet<WorkOrderRoutingLine> WorkOrderRoutingLines { get; set; }

    // NEW: Authentication entities (Hybrid Cookie + JWT system)
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserAuthMethod> UserAuthMethods { get; set; }
    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }

    // NEW: View Preferences entities
    public DbSet<SavedViewPreference> SavedViewPreferences { get; set; }

    // Configuration and Settings entities
    public DbSet<NumberSeries> NumberSeries { get; set; }
    public DbSet<ModuleSettings> ModuleSettings { get; set; }
    public DbSet<GlobalSettings> GlobalSettings { get; set; }
    public DbSet<CompanySharePointSettings> CompanySharePointSettings { get; set; }

    // Module Licensing (per-tenant product licenses)
    public DbSet<ProductLicense> ProductLicenses { get; set; }

    // Manufacturing entities
    public DbSet<WorkCenter> WorkCenters { get; set; }
    public DbSet<MachineCenter> MachineCenters { get; set; }
    public DbSet<MachineCapability> MachineCapabilities { get; set; }
    public DbSet<MachineOperator> MachineOperators { get; set; }
    public DbSet<WorkCenterShift> WorkCenterShifts { get; set; }

    // NEW: Item Management entities (7,107 AU/NZ catalog items)
    public DbSet<Catalogue> Catalogues { get; set; }
    public DbSet<CatalogueItem> CatalogueItems { get; set; }
    public DbSet<CatalogueItemStandardLength> CatalogueItemStandardLengths { get; set; }
    public DbSet<GratingSpecification> GratingSpecifications { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<Assembly> Assemblies { get; set; }
    public DbSet<AssemblyComponent> AssemblyComponents { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems { get; set; }
    public DbSet<ITPMaterialInspection> ITPMaterialInspections { get; set; }

    // WorkOrder Journal/Entry Tables
    public DbSet<WorkOrderAssemblyEntry> WorkOrderAssemblyEntries { get; set; }
    public DbSet<WorkOrderMaterialEntry> WorkOrderMaterialEntries { get; set; }
    public DbSet<WorkOrderResourceEntry> WorkOrderResourceEntries { get; set; }

    // NEW: PDF Annotation entities (Calibration & Annotations persistence)
    public DbSet<PdfScaleCalibration> PdfScaleCalibrations { get; set; }
    public DbSet<PdfAnnotation> PdfAnnotations { get; set; }

    // NEW: PDF Edit Lock entities (Concurrent editing prevention)
    public DbSet<PdfEditLock> PdfEditLocks { get; set; }

    // NEW: QDocs Module entities (Quality Documentation System)
    // IFA/IFC Drawing Management
    public DbSet<QDocsDrawing> QDocsDrawings { get; set; }
    public DbSet<DrawingRevision> DrawingRevisions { get; set; }
    public DbSet<DrawingPart> DrawingParts { get; set; }
    public DbSet<DrawingAssembly> DrawingAssemblies { get; set; }
    public DbSet<DrawingFile> DrawingFiles { get; set; }

    // Inspection Test Plans
    public DbSet<InspectionTestPlan> InspectionTestPlans { get; set; }
    public DbSet<ITPAssembly> ITPAssemblies { get; set; }
    public DbSet<ITPInspectionPoint> ITPInspectionPoints { get; set; }

    // Material Traceability & Quality Documents
    public DbSet<MaterialTraceability> MaterialTraceability { get; set; }
    public DbSet<QualityDocument> QualityDocuments { get; set; }
    public DbSet<TestResult> TestResults { get; set; }

    // Asset Module Entities (Equipment, Maintenance, Certifications)
    public DbSet<EquipmentCategory> EquipmentCategories { get; set; }
    public DbSet<EquipmentType> EquipmentTypes { get; set; }
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
    public DbSet<EquipmentCertification> EquipmentCertifications { get; set; }
    public DbSet<EquipmentManual> EquipmentManuals { get; set; }

    // Asset Module: Equipment Kits (Templates, Kits, Checkout/Return)
    public DbSet<KitTemplate> KitTemplates { get; set; }
    public DbSet<KitTemplateItem> KitTemplateItems { get; set; }
    public DbSet<EquipmentKit> EquipmentKits { get; set; }
    public DbSet<EquipmentKitItem> EquipmentKitItems { get; set; }
    public DbSet<KitCheckout> KitCheckouts { get; set; }
    public DbSet<KitCheckoutItem> KitCheckoutItems { get; set; }

    // Asset Module: Locations (Physical Sites, Job Sites, Vehicles)
    public DbSet<Location> Locations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User relationships to avoid multiple cascade paths
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany(u => u.OwnedProjects)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.LastModifiedByUser)
            .WithMany(u => u.ModifiedProjects)
            .HasForeignKey(p => p.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Package>()
            .HasOne(p => p.CreatedByUser)
            .WithMany(u => u.CreatedPackages)
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Package>()
            .HasOne(p => p.LastModifiedByUser)
            .WithMany(u => u.ModifiedPackages)
            .HasForeignKey(p => p.LastModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure CalibrationData relationships
        modelBuilder.Entity<CalibrationData>()
            .HasOne(c => c.PackageDrawing)
            .WithMany(pd => pd.Calibrations)
            .HasForeignKey(c => c.PackageDrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure PdfAnnotation to TraceTakeoffMeasurement relationship
        // NO cascade delete here - we handle it manually in the service layer
        // to ensure proper bidirectional cleanup
        modelBuilder.Entity<PdfAnnotation>()
            .HasOne(pa => pa.TraceTakeoffMeasurement)
            .WithMany() // No navigation property on TraceTakeoffMeasurement side
            .HasForeignKey(pa => pa.TraceTakeoffMeasurementId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent DB-level cascade, we handle in code

        modelBuilder.Entity<MachineCenter>()
            .HasOne(mc => mc.CreatedByUser)
            .WithMany(u => u.CreatedMachineCenters)
            .HasForeignKey(mc => mc.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MachineCenter>()
            .HasOne(mc => mc.LastModifiedByUser)
            .WithMany(u => u.ModifiedMachineCenters)
            .HasForeignKey(mc => mc.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes for performance
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.Code)
            .IsUnique();

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.JobNumber)
            .IsUnique();

        modelBuilder.Entity<MachineCenter>()
            .HasIndex(mc => mc.MachineCode)
            .IsUnique();

        modelBuilder.Entity<WorkCenter>()
            .HasIndex(wc => wc.WorkCenterCode)
            .IsUnique();

        // Configure decimal precision
        modelBuilder.Entity<Project>()
            .Property(p => p.LaborRate)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Project>()
            .Property(p => p.ContingencyPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Package>()
            .Property(p => p.EstimatedHours)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Package>()
            .Property(p => p.EstimatedCost)
            .HasPrecision(18, 2);

        // Configure default values
        modelBuilder.Entity<Company>()
            .Property(c => c.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(u => u.CompanyId)
            .HasDefaultValue(1); // Default to default company

        modelBuilder.Entity<Company>()
            .Property(c => c.SubscriptionLevel)
            .HasDefaultValue("Standard");

        modelBuilder.Entity<Company>()
            .Property(c => c.MaxUsers)
            .HasDefaultValue(10);

        modelBuilder.Entity<Takeoff>()
            .Property(td => td.UploadDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<Takeoff>()
            .Property(td => td.ProcessingStatus)
            .HasDefaultValue("Pending");

        modelBuilder.Entity<Takeoff>()
            .Property(td => td.ScaleUnit)
            .HasDefaultValue("mm");

        modelBuilder.Entity<Takeoff>()
            .Property(td => td.OCRStatus)
            .HasDefaultValue("NotProcessed");

        // NEW: Workflow entity configurations

        // Estimation configurations
        modelBuilder.Entity<Estimation>()
            .Property(e => e.CreatedDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<Estimation>()
            .Property(e => e.Status)
            .HasDefaultValue("Draft");

        // Order configurations
        modelBuilder.Entity<Order>()
            .Property(o => o.OrderDate)
            .HasDefaultValueSql("getutcdate()");

        // WorkOrder configurations
        modelBuilder.Entity<WorkOrder>()
            .Property(wo => wo.CreatedDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<WorkOrder>()
            .Property(wo => wo.Status)
            .HasDefaultValue("Planning");

        // Package dual-path configurations
        modelBuilder.Entity<Package>()
            .Property(p => p.PackageSource)
            .HasDefaultValue("Project");

        // Configure relationships to avoid cascade conflicts
        modelBuilder.Entity<Estimation>()
            .HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkOrder relationships - explicitly configure to avoid shadow properties
        // IMPORTANT: [Column("PackageId")] attribute added to WorkOrder.PackageId to prevent conflict
        // EF was creating PackageId1 shadow property due to multiple entities having PackageId
        // This explicitly tells EF that THIS PackageId maps to WorkPackage.Id
        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.WorkPackage)
            .WithMany(wp => wp.WorkOrders)
            .HasForeignKey(wo => wo.PackageId)
            .IsRequired()
            .HasConstraintName("FK_WorkOrders_WorkPackages_PackageId")
            .OnDelete(DeleteBehavior.Restrict);

        // NEW: Authentication entity configurations
        
        // RefreshToken configurations
        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.CreatedAt)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();
            
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.ExpiresAt });

        // UserAuthMethod configurations
        modelBuilder.Entity<UserAuthMethod>()
            .Property(uam => uam.CreatedAt)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<UserAuthMethod>()
            .HasIndex(uam => new { uam.UserId, uam.Provider })
            .IsUnique();

        // AuthAuditLog configurations
        modelBuilder.Entity<AuthAuditLog>()
            .Property(aal => aal.Timestamp)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<AuthAuditLog>()
            .HasIndex(aal => new { aal.Email, aal.Timestamp });
            
        modelBuilder.Entity<AuthAuditLog>()
            .HasIndex(aal => aal.Timestamp);

        // Configure authentication relationships to avoid cascade conflicts
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAuthMethod>()
            .HasOne(uam => uam.User)
            .WithMany()
            .HasForeignKey(uam => uam.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuthAuditLog>()
            .HasOne(aal => aal.User)
            .WithMany()
            .HasForeignKey(aal => aal.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure CompanySharePointSettings relationships (multiple User FKs)
        modelBuilder.Entity<CompanySharePointSettings>()
            .HasOne(css => css.CreatedByUser)
            .WithMany()
            .HasForeignKey(css => css.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CompanySharePointSettings>()
            .HasOne(css => css.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(css => css.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure ProductLicense relationships (multiple User FKs)
        modelBuilder.Entity<ProductLicense>()
            .HasOne(pl => pl.CreatedByUser)
            .WithMany()
            .HasForeignKey(pl => pl.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductLicense>()
            .HasOne(pl => pl.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(pl => pl.ModifiedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // NEW: Item Management configurations
        
        // CatalogueItem configurations
        modelBuilder.Entity<CatalogueItem>()
            .Property(ci => ci.CreatedDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(ci => ci.ItemCode)
            .IsUnique();
            
        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(ci => ci.Category);
            
        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(ci => ci.Material);
            
        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(ci => ci.Profile);
            
        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(ci => ci.Grade);
            
        modelBuilder.Entity<CatalogueItem>()
            .HasIndex(ci => ci.Finish);

        // GratingSpecification configurations
        modelBuilder.Entity<GratingSpecification>()
            .HasOne(gs => gs.CatalogueItem)
            .WithOne(ci => ci.GratingSpecification)
            .HasForeignKey<GratingSpecification>(gs => gs.CatalogueItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // InventoryItem configurations
        modelBuilder.Entity<InventoryItem>()
            .Property(ii => ii.CreatedDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<InventoryItem>()
            .HasIndex(ii => ii.InventoryCode)
            .IsUnique();
            
        modelBuilder.Entity<InventoryItem>()
            .HasIndex(ii => ii.LotNumber);
            
        modelBuilder.Entity<InventoryItem>()
            .HasIndex(ii => ii.HeatNumber);

        // InventoryTransaction configurations
        modelBuilder.Entity<InventoryTransaction>()
            .Property(it => it.TransactionDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<InventoryTransaction>()
            .HasIndex(it => it.TransactionNumber)
            .IsUnique();

        // Assembly configurations
        modelBuilder.Entity<Assembly>()
            .Property(a => a.CreatedDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<Assembly>()
            .HasIndex(a => a.AssemblyNumber)
            .IsUnique();

        // AssemblyComponent configurations
        modelBuilder.Entity<AssemblyComponent>()
            .Property(ac => ac.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        // Configure item management relationships to avoid cascade conflicts
        modelBuilder.Entity<InventoryItem>()
            .HasOne(ii => ii.CatalogueItem)
            .WithMany(ci => ci.InventoryItems)
            .HasForeignKey(ii => ii.CatalogueItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(it => it.User)
            .WithMany()
            .HasForeignKey(it => it.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assembly>()
            .HasOne(a => a.ParentAssembly)
            .WithMany(a => a.SubAssemblies)
            .HasForeignKey(a => a.ParentAssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssemblyComponent>()
            .HasOne(ac => ac.CatalogueItem)
            .WithMany(ci => ci.AssemblyComponents)
            .HasForeignKey(ac => ac.CatalogueItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssemblyComponent>()
            .HasOne(ac => ac.ComponentAssembly)
            .WithMany()
            .HasForeignKey(ac => ac.ComponentAssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        // CompanySharePointSettings configurations
        modelBuilder.Entity<CompanySharePointSettings>()
            .Property(csps => csps.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<CompanySharePointSettings>()
            .Property(csps => csps.LastModifiedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<CompanySharePointSettings>()
            .HasIndex(csps => csps.CompanyId)
            .IsUnique();

        modelBuilder.Entity<CompanySharePointSettings>()
            .HasOne(csps => csps.Company)
            .WithMany()
            .HasForeignKey(csps => csps.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompanySharePointSettings>()
            .HasOne(csps => csps.CreatedByUser)
            .WithMany()
            .HasForeignKey(csps => csps.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanySharePointSettings>()
            .HasOne(csps => csps.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(csps => csps.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // PDF Annotation entity configurations

        // PdfScaleCalibration configurations
        modelBuilder.Entity<PdfScaleCalibration>()
            .Property(psc => psc.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<PdfScaleCalibration>()
            .HasIndex(psc => psc.PackageDrawingId);

        modelBuilder.Entity<PdfScaleCalibration>()
            .HasOne(psc => psc.PackageDrawing)
            .WithMany()
            .HasForeignKey(psc => psc.PackageDrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PdfScaleCalibration>()
            .HasOne(psc => psc.CreatedByUser)
            .WithMany()
            .HasForeignKey(psc => psc.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // PdfAnnotation configurations
        modelBuilder.Entity<PdfAnnotation>()
            .Property(pa => pa.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<PdfAnnotation>()
            .HasIndex(pa => new { pa.PackageDrawingId, pa.AnnotationId });

        modelBuilder.Entity<PdfAnnotation>()
            .HasIndex(pa => pa.PackageDrawingId);

        modelBuilder.Entity<PdfAnnotation>()
            .HasIndex(pa => pa.TraceTakeoffMeasurementId);

        modelBuilder.Entity<PdfAnnotation>()
            .HasOne(pa => pa.PackageDrawing)
            .WithMany()
            .HasForeignKey(pa => pa.PackageDrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PdfAnnotation>()
            .HasOne(pa => pa.TraceTakeoffMeasurement)
            .WithMany()
            .HasForeignKey(pa => pa.TraceTakeoffMeasurementId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PdfAnnotation>()
            .HasOne(pa => pa.CreatedByUser)
            .WithMany()
            .HasForeignKey(pa => pa.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // PdfEditLock configurations
        modelBuilder.Entity<PdfEditLock>()
            .Property(pel => pel.LockedAt)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<PdfEditLock>()
            .Property(pel => pel.LastHeartbeat)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<PdfEditLock>()
            .Property(pel => pel.LastActivityAt)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<PdfEditLock>()
            .Property(pel => pel.IsActive)
            .HasDefaultValue(true);

        // Index for fast lookup of active locks by drawing
        modelBuilder.Entity<PdfEditLock>()
            .HasIndex(pel => new { pel.PackageDrawingId, pel.IsActive });

        // Index for stale lock cleanup (find locks with old heartbeats)
        modelBuilder.Entity<PdfEditLock>()
            .HasIndex(pel => pel.LastHeartbeat)
            .HasFilter("[IsActive] = 1");

        // Index for session lookup
        modelBuilder.Entity<PdfEditLock>()
            .HasIndex(pel => pel.SessionId);

        // Configure relationships
        modelBuilder.Entity<PdfEditLock>()
            .HasOne(pel => pel.PackageDrawing)
            .WithMany()
            .HasForeignKey(pel => pel.PackageDrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PdfEditLock>()
            .HasOne(pel => pel.User)
            .WithMany()
            .HasForeignKey(pel => pel.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // SurfaceCoating configurations
        modelBuilder.Entity<SurfaceCoating>()
            .Property(sc => sc.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<SurfaceCoating>()
            .HasIndex(sc => new { sc.CompanyId, sc.CoatingCode })
            .IsUnique();

        modelBuilder.Entity<SurfaceCoating>()
            .HasIndex(sc => sc.DisplayOrder);

        modelBuilder.Entity<SurfaceCoating>()
            .HasOne(sc => sc.Company)
            .WithMany()
            .HasForeignKey(sc => sc.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // TraceTakeoffMeasurement configurations
        modelBuilder.Entity<TraceTakeoffMeasurement>()
            .Property(ttm => ttm.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<TraceTakeoffMeasurement>()
            .HasOne(ttm => ttm.SurfaceCoating)
            .WithMany()
            .HasForeignKey(ttm => ttm.SurfaceCoatingId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TraceTakeoffMeasurement>()
            .HasOne(ttm => ttm.CreatedByUser)
            .WithMany()
            .HasForeignKey(ttm => ttm.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TraceTakeoffMeasurement>()
            .HasOne(ttm => ttm.ModifiedByUser)
            .WithMany()
            .HasForeignKey(ttm => ttm.ModifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // QDocs Module entity configurations

        // QDocsDrawing configurations
        modelBuilder.Entity<QDocsDrawing>()
            .Property(qd => qd.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<QDocsDrawing>()
            .Property(qd => qd.CurrentStage)
            .HasDefaultValue("IFA");

        modelBuilder.Entity<QDocsDrawing>()
            .HasIndex(qd => qd.DrawingNumber);

        modelBuilder.Entity<QDocsDrawing>()
            .HasIndex(qd => qd.OrderId);

        modelBuilder.Entity<QDocsDrawing>()
            .HasIndex(qd => qd.WorkPackageId);

        modelBuilder.Entity<QDocsDrawing>()
            .HasIndex(qd => qd.AssemblyId);

        modelBuilder.Entity<QDocsDrawing>()
            .HasOne(qd => qd.Order)
            .WithMany()
            .HasForeignKey(qd => qd.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QDocsDrawing>()
            .HasOne(qd => qd.WorkPackage)
            .WithMany()
            .HasForeignKey(qd => qd.WorkPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QDocsDrawing>()
            .HasOne(qd => qd.Assembly)
            .WithMany()
            .HasForeignKey(qd => qd.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QDocsDrawing>()
            .HasOne(qd => qd.CreatedByUser)
            .WithMany()
            .HasForeignKey(qd => qd.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // DrawingRevision configurations
        modelBuilder.Entity<DrawingRevision>()
            .Property(dr => dr.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<DrawingRevision>()
            .Property(dr => dr.Status)
            .HasDefaultValue("Draft");

        modelBuilder.Entity<DrawingRevision>()
            .Property(dr => dr.IsActiveForProduction)
            .HasDefaultValue(false);

        modelBuilder.Entity<DrawingRevision>()
            .HasIndex(dr => dr.RevisionCode);

        modelBuilder.Entity<DrawingRevision>()
            .HasIndex(dr => new { dr.DrawingId, dr.RevisionType, dr.RevisionNumber });

        modelBuilder.Entity<DrawingRevision>()
            .HasOne(dr => dr.Drawing)
            .WithMany(qd => qd.Revisions)
            .HasForeignKey(dr => dr.DrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DrawingRevision>()
            .HasOne(dr => dr.SupersededByRevision)
            .WithMany()
            .HasForeignKey(dr => dr.SupersededById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DrawingRevision>()
            .HasOne(dr => dr.CreatedFromIFARevision)
            .WithMany()
            .HasForeignKey(dr => dr.CreatedFromIFARevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DrawingRevision>()
            .HasOne(dr => dr.CreatedByUser)
            .WithMany()
            .HasForeignKey(dr => dr.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // DrawingPart configurations
        modelBuilder.Entity<DrawingPart>()
            .HasIndex(dp => dp.DrawingId);

        modelBuilder.Entity<DrawingPart>()
            .HasIndex(dp => dp.DrawingRevisionId);

        modelBuilder.Entity<DrawingPart>()
            .HasIndex(dp => dp.PartType);

        modelBuilder.Entity<DrawingPart>()
            .HasIndex(dp => dp.AssemblyMark);

        modelBuilder.Entity<DrawingPart>()
            .HasIndex(dp => dp.ParentAssemblyId);

        modelBuilder.Entity<DrawingPart>()
            .HasOne(dp => dp.Drawing)
            .WithMany(qd => qd.Parts)
            .HasForeignKey(dp => dp.DrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DrawingPart>()
            .HasOne(dp => dp.DrawingRevision)
            .WithMany()
            .HasForeignKey(dp => dp.DrawingRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DrawingPart>()
            .HasOne(dp => dp.ParentAssembly)
            .WithMany(da => da.Parts)
            .HasForeignKey(dp => dp.ParentAssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DrawingPart>()
            .HasOne(dp => dp.CatalogueItem)
            .WithMany()
            .HasForeignKey(dp => dp.CatalogueItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // DrawingAssembly configurations
        modelBuilder.Entity<DrawingAssembly>()
            .Property(da => da.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<DrawingAssembly>()
            .HasIndex(da => da.DrawingId);

        modelBuilder.Entity<DrawingAssembly>()
            .HasIndex(da => da.DrawingRevisionId);

        modelBuilder.Entity<DrawingAssembly>()
            .HasIndex(da => da.AssemblyMark);

        modelBuilder.Entity<DrawingAssembly>()
            .HasIndex(da => new { da.DrawingId, da.AssemblyMark })
            .IsUnique();

        modelBuilder.Entity<DrawingAssembly>()
            .HasOne(da => da.Drawing)
            .WithMany()
            .HasForeignKey(da => da.DrawingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DrawingAssembly>()
            .HasOne(da => da.DrawingRevision)
            .WithMany()
            .HasForeignKey(da => da.DrawingRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DrawingAssembly>()
            .HasOne(da => da.Company)
            .WithMany()
            .HasForeignKey(da => da.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // InspectionTestPlan configurations
        modelBuilder.Entity<InspectionTestPlan>()
            .Property(itp => itp.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<InspectionTestPlan>()
            .Property(itp => itp.Status)
            .HasDefaultValue("Draft");

        modelBuilder.Entity<InspectionTestPlan>()
            .HasIndex(itp => itp.ITPNumber)
            .IsUnique();

        modelBuilder.Entity<InspectionTestPlan>()
            .HasIndex(itp => itp.OrderId);

        modelBuilder.Entity<InspectionTestPlan>()
            .HasIndex(itp => itp.WorkPackageId);

        modelBuilder.Entity<InspectionTestPlan>()
            .HasIndex(itp => itp.WorkOrderId);

        // Configure ITP relationships to avoid cascade conflicts
        modelBuilder.Entity<InspectionTestPlan>()
            .HasOne(itp => itp.Order)
            .WithMany()
            .HasForeignKey(itp => itp.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionTestPlan>()
            .HasOne(itp => itp.WorkPackage)
            .WithMany()
            .HasForeignKey(itp => itp.WorkPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionTestPlan>()
            .HasOne(itp => itp.WorkOrder)
            .WithMany()
            .HasForeignKey(itp => itp.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionTestPlan>()
            .HasOne(itp => itp.CreatedByUser)
            .WithMany()
            .HasForeignKey(itp => itp.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionTestPlan>()
            .HasOne(itp => itp.ApprovedByUser)
            .WithMany()
            .HasForeignKey(itp => itp.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ITPAssembly configurations
        modelBuilder.Entity<ITPAssembly>()
            .HasIndex(ia => ia.ITPId);

        modelBuilder.Entity<ITPAssembly>()
            .HasIndex(ia => ia.AssemblyId);

        modelBuilder.Entity<ITPAssembly>()
            .HasOne(ia => ia.ITP)
            .WithMany(itp => itp.CoveredAssemblies)
            .HasForeignKey(ia => ia.ITPId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ITPAssembly>()
            .HasOne(ia => ia.WorkOrderAssemblyEntry)
            .WithMany()
            .HasForeignKey(ia => ia.WorkOrderAssemblyEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ITPAssembly>()
            .HasOne(ia => ia.Assembly)
            .WithMany()
            .HasForeignKey(ia => ia.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        // ITPInspectionPoint configurations
        modelBuilder.Entity<ITPInspectionPoint>()
            .Property(ip => ip.Status)
            .HasDefaultValue("NotStarted");

        modelBuilder.Entity<ITPInspectionPoint>()
            .Property(ip => ip.InspectionType)
            .HasDefaultValue("Review");

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasIndex(ip => ip.ITPId);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasIndex(ip => ip.WorkOrderOperationId);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasIndex(ip => ip.AssemblyId);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasOne(ip => ip.ITP)
            .WithMany(itp => itp.InspectionPoints)
            .HasForeignKey(ip => ip.ITPId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasOne(ip => ip.WorkOrderOperation)
            .WithMany()
            .HasForeignKey(ip => ip.WorkOrderOperationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasOne(ip => ip.Assembly)
            .WithMany()
            .HasForeignKey(ip => ip.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasOne(ip => ip.Inspector)
            .WithMany()
            .HasForeignKey(ip => ip.InspectorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ITPInspectionPoint>()
            .HasOne(ip => ip.ReleasedByUser)
            .WithMany()
            .HasForeignKey(ip => ip.ReleasedById)
            .OnDelete(DeleteBehavior.Restrict);

        // MaterialTraceability configurations
        modelBuilder.Entity<MaterialTraceability>()
            .Property(mt => mt.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<MaterialTraceability>()
            .HasIndex(mt => mt.TraceNumber)
            .IsUnique();

        modelBuilder.Entity<MaterialTraceability>()
            .HasIndex(mt => mt.HeatNumber);

        modelBuilder.Entity<MaterialTraceability>()
            .HasIndex(mt => mt.CatalogueItemId);

        modelBuilder.Entity<MaterialTraceability>()
            .HasIndex(mt => mt.InventoryItemId);

        modelBuilder.Entity<MaterialTraceability>()
            .HasIndex(mt => mt.WorkOrderMaterialEntryId);

        modelBuilder.Entity<MaterialTraceability>()
            .HasOne(mt => mt.CatalogueItem)
            .WithMany()
            .HasForeignKey(mt => mt.CatalogueItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialTraceability>()
            .HasOne(mt => mt.InventoryItem)
            .WithMany()
            .HasForeignKey(mt => mt.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialTraceability>()
            .HasOne(mt => mt.WorkOrderMaterialEntry)
            .WithMany()
            .HasForeignKey(mt => mt.WorkOrderMaterialEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialTraceability>()
            .HasOne(mt => mt.ParentTrace)
            .WithMany(mt => mt.ChildTraces)
            .HasForeignKey(mt => mt.ParentTraceId)
            .OnDelete(DeleteBehavior.Restrict);

        // QualityDocument configurations
        modelBuilder.Entity<QualityDocument>()
            .Property(qd => qd.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<QualityDocument>()
            .Property(qd => qd.Status)
            .HasDefaultValue("Draft");

        modelBuilder.Entity<QualityDocument>()
            .Property(qd => qd.Version)
            .HasDefaultValue(1);

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.DocumentNumber)
            .IsUnique();

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.DocumentType);

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.OrderId);

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.WorkPackageId);

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.WorkOrderId);

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.AssemblyId);

        modelBuilder.Entity<QualityDocument>()
            .HasIndex(qd => qd.MaterialTraceabilityId);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.Order)
            .WithMany()
            .HasForeignKey(qd => qd.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.WorkPackage)
            .WithMany()
            .HasForeignKey(qd => qd.WorkPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.WorkOrder)
            .WithMany()
            .HasForeignKey(qd => qd.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.Assembly)
            .WithMany()
            .HasForeignKey(qd => qd.AssemblyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.MaterialTrace)
            .WithMany(mt => mt.Certificates)
            .HasForeignKey(qd => qd.MaterialTraceabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.ITPPoint)
            .WithMany()
            .HasForeignKey(qd => qd.ITPInspectionPointId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.CreatedByUser)
            .WithMany()
            .HasForeignKey(qd => qd.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.ApprovedByUser)
            .WithMany()
            .HasForeignKey(qd => qd.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QualityDocument>()
            .HasOne(qd => qd.SupersededByDocument)
            .WithMany()
            .HasForeignKey(qd => qd.SupersededById)
            .OnDelete(DeleteBehavior.Restrict);

        // TestResult configurations
        modelBuilder.Entity<TestResult>()
            .Property(tr => tr.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<TestResult>()
            .Property(tr => tr.Result)
            .HasDefaultValue("Pending");

        modelBuilder.Entity<TestResult>()
            .Property(tr => tr.NumberOfSamples)
            .HasDefaultValue(1);

        modelBuilder.Entity<TestResult>()
            .Property(tr => tr.NumberOfRetests)
            .HasDefaultValue(0);

        modelBuilder.Entity<TestResult>()
            .HasIndex(tr => tr.TestNumber)
            .IsUnique();

        modelBuilder.Entity<TestResult>()
            .HasIndex(tr => tr.TestType);

        modelBuilder.Entity<TestResult>()
            .HasIndex(tr => tr.HeatNumber);

        modelBuilder.Entity<TestResult>()
            .HasOne(tr => tr.QualityDocument)
            .WithMany(qd => qd.TestResults)
            .HasForeignKey(tr => tr.QualityDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestResult>()
            .HasOne(tr => tr.ITPPoint)
            .WithMany(ip => ip.TestResults)
            .HasForeignKey(tr => tr.ITPInspectionPointId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestResult>()
            .HasOne(tr => tr.MaterialTrace)
            .WithMany()
            .HasForeignKey(tr => tr.MaterialTraceabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestResult>()
            .HasOne(tr => tr.Tester)
            .WithMany()
            .HasForeignKey(tr => tr.TesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestResult>()
            .HasOne(tr => tr.Witness)
            .WithMany()
            .HasForeignKey(tr => tr.WitnessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Asset Module entity configurations

        // EquipmentCategory configurations
        modelBuilder.Entity<EquipmentCategory>()
            .Property(ec => ec.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<EquipmentCategory>()
            .Property(ec => ec.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<EquipmentCategory>()
            .HasIndex(ec => new { ec.CompanyId, ec.Name })
            .IsUnique();

        // EquipmentType configurations
        modelBuilder.Entity<EquipmentType>()
            .Property(et => et.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<EquipmentType>()
            .Property(et => et.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<EquipmentType>()
            .HasIndex(et => new { et.CategoryId, et.Name })
            .IsUnique();

        modelBuilder.Entity<EquipmentType>()
            .HasOne(et => et.EquipmentCategory)
            .WithMany(ec => ec.EquipmentTypes)
            .HasForeignKey(et => et.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Equipment configurations
        modelBuilder.Entity<Equipment>()
            .Property(e => e.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<Equipment>()
            .Property(e => e.Status)
            .HasDefaultValue(EquipmentStatus.Active);

        modelBuilder.Entity<Equipment>()
            .Property(e => e.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => e.EquipmentCode)
            .IsUnique();

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => e.QRCodeIdentifier)
            .IsUnique()
            .HasFilter("[QRCodeIdentifier] IS NOT NULL");

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => e.SerialNumber);

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => e.LocationId);

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<Equipment>()
            .HasIndex(e => new { e.CompanyId, e.IsDeleted });

        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.EquipmentCategory)
            .WithMany(ec => ec.Equipment)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.EquipmentType)
            .WithMany(et => et.Equipment)
            .HasForeignKey(e => e.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // MaintenanceSchedule configurations
        modelBuilder.Entity<MaintenanceSchedule>()
            .Property(ms => ms.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<MaintenanceSchedule>()
            .Property(ms => ms.Status)
            .HasDefaultValue(MaintenanceScheduleStatus.Active);

        modelBuilder.Entity<MaintenanceSchedule>()
            .Property(ms => ms.ReminderDaysBefore)
            .HasDefaultValue(7);

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasIndex(ms => ms.EquipmentId);

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasIndex(ms => ms.NextDue);

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasIndex(ms => ms.Status);

        modelBuilder.Entity<MaintenanceSchedule>()
            .HasOne(ms => ms.Equipment)
            .WithMany(e => e.MaintenanceSchedules)
            .HasForeignKey(ms => ms.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // MaintenanceRecord configurations
        modelBuilder.Entity<MaintenanceRecord>()
            .Property(mr => mr.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<MaintenanceRecord>()
            .Property(mr => mr.Status)
            .HasDefaultValue(MaintenanceRecordStatus.Scheduled);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasIndex(mr => mr.EquipmentId);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasIndex(mr => mr.ScheduledDate);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasIndex(mr => mr.Status);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasOne(mr => mr.Equipment)
            .WithMany(e => e.MaintenanceRecords)
            .HasForeignKey(mr => mr.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MaintenanceRecord>()
            .HasOne(mr => mr.Schedule)
            .WithMany(ms => ms.MaintenanceRecords)
            .HasForeignKey(mr => mr.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        // EquipmentCertification configurations
        modelBuilder.Entity<EquipmentCertification>()
            .Property(ec => ec.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<EquipmentCertification>()
            .Property(ec => ec.Status)
            .HasDefaultValue("Valid");

        modelBuilder.Entity<EquipmentCertification>()
            .HasIndex(ec => ec.EquipmentId);

        modelBuilder.Entity<EquipmentCertification>()
            .HasIndex(ec => ec.ExpiryDate);

        modelBuilder.Entity<EquipmentCertification>()
            .HasIndex(ec => ec.CertificationType);

        modelBuilder.Entity<EquipmentCertification>()
            .HasOne(ec => ec.Equipment)
            .WithMany(e => e.Certifications)
            .HasForeignKey(ec => ec.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // EquipmentManual configurations
        modelBuilder.Entity<EquipmentManual>()
            .Property(em => em.UploadedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<EquipmentManual>()
            .HasIndex(em => em.EquipmentId);

        modelBuilder.Entity<EquipmentManual>()
            .HasIndex(em => em.ManualType);

        modelBuilder.Entity<EquipmentManual>()
            .HasOne(em => em.Equipment)
            .WithMany(e => e.Manuals)
            .HasForeignKey(em => em.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Equipment Kit configurations

        // KitTemplate configurations
        modelBuilder.Entity<KitTemplate>()
            .Property(kt => kt.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<KitTemplate>()
            .Property(kt => kt.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<KitTemplate>()
            .Property(kt => kt.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<KitTemplate>()
            .Property(kt => kt.DefaultCheckoutDays)
            .HasDefaultValue(7);

        modelBuilder.Entity<KitTemplate>()
            .Property(kt => kt.RequiresSignature)
            .HasDefaultValue(true);

        modelBuilder.Entity<KitTemplate>()
            .Property(kt => kt.RequiresConditionCheck)
            .HasDefaultValue(true);

        modelBuilder.Entity<KitTemplate>()
            .HasIndex(kt => new { kt.CompanyId, kt.TemplateCode })
            .IsUnique();

        modelBuilder.Entity<KitTemplate>()
            .HasIndex(kt => new { kt.CompanyId, kt.IsDeleted });

        modelBuilder.Entity<KitTemplate>()
            .HasIndex(kt => kt.Category);

        // KitTemplateItem configurations
        modelBuilder.Entity<KitTemplateItem>()
            .Property(kti => kti.Quantity)
            .HasDefaultValue(1);

        modelBuilder.Entity<KitTemplateItem>()
            .Property(kti => kti.IsMandatory)
            .HasDefaultValue(true);

        modelBuilder.Entity<KitTemplateItem>()
            .HasIndex(kti => kti.KitTemplateId);

        modelBuilder.Entity<KitTemplateItem>()
            .HasOne(kti => kti.KitTemplate)
            .WithMany(kt => kt.TemplateItems)
            .HasForeignKey(kti => kti.KitTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitTemplateItem>()
            .HasOne(kti => kti.EquipmentType)
            .WithMany()
            .HasForeignKey(kti => kti.EquipmentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // EquipmentKit configurations
        modelBuilder.Entity<EquipmentKit>()
            .Property(ek => ek.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<EquipmentKit>()
            .Property(ek => ek.Status)
            .HasDefaultValue(KitStatus.Available);

        modelBuilder.Entity<EquipmentKit>()
            .Property(ek => ek.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<EquipmentKit>()
            .Property(ek => ek.HasMaintenanceFlag)
            .HasDefaultValue(false);

        modelBuilder.Entity<EquipmentKit>()
            .HasIndex(ek => new { ek.CompanyId, ek.KitCode })
            .IsUnique();

        modelBuilder.Entity<EquipmentKit>()
            .HasIndex(ek => ek.QRCodeIdentifier)
            .IsUnique()
            .HasFilter("[QRCodeIdentifier] IS NOT NULL");

        modelBuilder.Entity<EquipmentKit>()
            .HasIndex(ek => new { ek.CompanyId, ek.IsDeleted });

        modelBuilder.Entity<EquipmentKit>()
            .HasIndex(ek => ek.Status);

        modelBuilder.Entity<EquipmentKit>()
            .HasIndex(ek => ek.AssignedToUserId);

        modelBuilder.Entity<EquipmentKit>()
            .HasOne(ek => ek.KitTemplate)
            .WithMany(kt => kt.Kits)
            .HasForeignKey(ek => ek.KitTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // EquipmentKitItem configurations
        modelBuilder.Entity<EquipmentKitItem>()
            .Property(eki => eki.AddedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<EquipmentKitItem>()
            .Property(eki => eki.NeedsMaintenance)
            .HasDefaultValue(false);

        modelBuilder.Entity<EquipmentKitItem>()
            .HasIndex(eki => eki.KitId);

        modelBuilder.Entity<EquipmentKitItem>()
            .HasIndex(eki => eki.EquipmentId);

        modelBuilder.Entity<EquipmentKitItem>()
            .HasIndex(eki => new { eki.KitId, eki.EquipmentId })
            .IsUnique();

        modelBuilder.Entity<EquipmentKitItem>()
            .HasOne(eki => eki.Kit)
            .WithMany(ek => ek.KitItems)
            .HasForeignKey(eki => eki.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquipmentKitItem>()
            .HasOne(eki => eki.Equipment)
            .WithMany()
            .HasForeignKey(eki => eki.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EquipmentKitItem>()
            .HasOne(eki => eki.TemplateItem)
            .WithMany()
            .HasForeignKey(eki => eki.TemplateItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // KitCheckout configurations
        modelBuilder.Entity<KitCheckout>()
            .Property(kc => kc.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<KitCheckout>()
            .Property(kc => kc.CheckoutDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<KitCheckout>()
            .Property(kc => kc.Status)
            .HasDefaultValue(CheckoutStatus.Pending);

        modelBuilder.Entity<KitCheckout>()
            .Property(kc => kc.CheckoutOverallCondition)
            .HasDefaultValue(EquipmentCondition.Good);

        modelBuilder.Entity<KitCheckout>()
            .HasIndex(kc => kc.KitId);

        modelBuilder.Entity<KitCheckout>()
            .HasIndex(kc => kc.CheckedOutToUserId);

        modelBuilder.Entity<KitCheckout>()
            .HasIndex(kc => kc.Status);

        modelBuilder.Entity<KitCheckout>()
            .HasIndex(kc => kc.ExpectedReturnDate);

        modelBuilder.Entity<KitCheckout>()
            .HasIndex(kc => new { kc.CompanyId, kc.Status });

        modelBuilder.Entity<KitCheckout>()
            .HasOne(kc => kc.Kit)
            .WithMany(ek => ek.Checkouts)
            .HasForeignKey(kc => kc.KitId)
            .OnDelete(DeleteBehavior.Cascade);

        // KitCheckoutItem configurations
        modelBuilder.Entity<KitCheckoutItem>()
            .Property(kci => kci.WasPresentAtCheckout)
            .HasDefaultValue(true);

        modelBuilder.Entity<KitCheckoutItem>()
            .Property(kci => kci.DamageReported)
            .HasDefaultValue(false);

        modelBuilder.Entity<KitCheckoutItem>()
            .HasIndex(kci => kci.KitCheckoutId);

        modelBuilder.Entity<KitCheckoutItem>()
            .HasIndex(kci => kci.EquipmentId);

        modelBuilder.Entity<KitCheckoutItem>()
            .HasOne(kci => kci.KitCheckout)
            .WithMany(kc => kc.CheckoutItems)
            .HasForeignKey(kci => kci.KitCheckoutId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KitCheckoutItem>()
            .HasOne(kci => kci.KitItem)
            .WithMany()
            .HasForeignKey(kci => kci.KitItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<KitCheckoutItem>()
            .HasOne(kci => kci.Equipment)
            .WithMany()
            .HasForeignKey(kci => kci.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Location configurations (Physical Sites, Job Sites, Vehicles)
        modelBuilder.Entity<Location>()
            .Property(l => l.CreatedDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<Location>()
            .Property(l => l.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Location>()
            .Property(l => l.IsDeleted)
            .HasDefaultValue(false);

        modelBuilder.Entity<Location>()
            .Property(l => l.Type)
            .HasDefaultValue(LocationType.PhysicalSite);

        // Unique index: LocationCode must be unique per company
        modelBuilder.Entity<Location>()
            .HasIndex(l => new { l.CompanyId, l.LocationCode })
            .IsUnique();

        // Index for filtering by company and not deleted
        modelBuilder.Entity<Location>()
            .HasIndex(l => new { l.CompanyId, l.IsDeleted });

        // Index for filtering by location type
        modelBuilder.Entity<Location>()
            .HasIndex(l => l.Type);

        // Index for filtering by active status
        modelBuilder.Entity<Location>()
            .HasIndex(l => l.IsActive);

        // Equipment -> Location relationship
        modelBuilder.Entity<Equipment>()
            .HasOne(e => e.Location)
            .WithMany(l => l.Equipment)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // EquipmentKit -> Location relationship
        modelBuilder.Entity<EquipmentKit>()
            .HasOne(ek => ek.Location)
            .WithMany(l => l.Kits)
            .HasForeignKey(ek => ek.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
