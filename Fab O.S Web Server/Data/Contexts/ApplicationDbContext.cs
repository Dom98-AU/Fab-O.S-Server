using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Models.Entities;
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
    public DbSet<TraceDrawing> TraceDrawings { get; set; }
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

    // Calibration entities
    public DbSet<CalibrationData> Calibrations { get; set; }

    // NEW: Workflow entities (Quote → Order → WorkOrder system)
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<QuoteLineItem> QuoteLineItems { get; set; }
    public DbSet<Estimation> Estimations { get; set; }
    public DbSet<EstimationPackage> EstimationPackages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<WorkOrder> WorkOrders { get; set; }
    public DbSet<WorkOrderOperation> WorkOrderOperations { get; set; }
    public DbSet<Resource> Resources { get; set; }

    // NEW: Authentication entities (Hybrid Cookie + JWT system)
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserAuthMethod> UserAuthMethods { get; set; }
    public DbSet<AuthAuditLog> AuthAuditLogs { get; set; }

    // NEW: View Preferences entities
    public DbSet<SavedViewPreference> SavedViewPreferences { get; set; }

    // Manufacturing entities
    public DbSet<WorkCenter> WorkCenters { get; set; }
    public DbSet<MachineCenter> MachineCenters { get; set; }
    public DbSet<MachineCapability> MachineCapabilities { get; set; }
    public DbSet<MachineOperator> MachineOperators { get; set; }
    public DbSet<WorkCenterShift> WorkCenterShifts { get; set; }

    // NEW: Item Management entities (7,107 AU/NZ catalog items)
    public DbSet<CatalogueItem> CatalogueItems { get; set; }
    public DbSet<GratingSpecification> GratingSpecifications { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<Assembly> Assemblies { get; set; }
    public DbSet<AssemblyComponent> AssemblyComponents { get; set; }

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

        modelBuilder.Entity<Company>()
            .Property(c => c.SubscriptionLevel)
            .HasDefaultValue("Standard");

        modelBuilder.Entity<Company>()
            .Property(c => c.MaxUsers)
            .HasDefaultValue(10);

        modelBuilder.Entity<TraceDrawing>()
            .Property(td => td.UploadDate)
            .HasDefaultValueSql("getutcdate()");

        modelBuilder.Entity<TraceDrawing>()
            .Property(td => td.ProcessingStatus)
            .HasDefaultValue("Pending");

        modelBuilder.Entity<TraceDrawing>()
            .Property(td => td.ScaleUnit)
            .HasDefaultValue("mm");

        modelBuilder.Entity<TraceDrawing>()
            .Property(td => td.OCRStatus)
            .HasDefaultValue("NotProcessed");

        // NEW: Workflow entity configurations
        
        // Quote configurations
        modelBuilder.Entity<Quote>()
            .Property(q => q.QuoteDate)
            .HasDefaultValueSql("getutcdate()");
            
        modelBuilder.Entity<Quote>()
            .Property(q => q.Status)
            .HasDefaultValue("Draft");
            
        modelBuilder.Entity<Quote>()
            .Property(q => q.ValidUntil)
            .HasDefaultValueSql("dateadd(day, 30, getutcdate())");

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
            
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasDefaultValue("Pending");

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
        modelBuilder.Entity<Quote>()
            .HasOne(q => q.CreatedByUser)
            .WithMany()
            .HasForeignKey(q => q.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Estimation>()
            .HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkOrder>()
            .HasOne(wo => wo.CreatedByUser)
            .WithMany()
            .HasForeignKey(wo => wo.CreatedBy)
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
    }
}
