# Fab.OS Multi-Tenant Architecture - Complete ERD Documentation

## Executive Summary

This document presents the complete Entity Relationship Diagrams (ERDs) for the Fab.OS multi-tenant platform architecture. The design implements Microsoft Azure's recommended "Tenant Catalog" pattern with database-per-tenant isolation, providing enterprise-grade scalability, security, and maintainability.

### Key Architecture Decisions
- **Direct Registry Access**: Dev Portal queries Master Tenant Registry directly (no data duplication)
- **Module-Based Licensing**: Granular control over product access (Estimate, Trace, Fabmate, QDocs)
- **Database-Per-Tenant**: Complete data isolation for security and compliance
- **Single Source of Truth**: Master Tenant Registry is the authoritative source for all tenant information

## Table of Contents
1. [Dev Portal Management Database ERD](#1-dev-portal-management-database-erd-direct-registry-access)
2. [Master Tenant Registry ERD](#2-master-tenant-registry-erd-enhanced-with-module-tracking)
3. [Individual Tenant Database ERD](#3-individual-tenant-database-erd-per-tenant-schema)
4. [Complete Multi-Tenant Architecture Overview](#4-complete-multi-tenant-architecture-overview-direct-registry-access)
5. [Multi-Tenant Authentication & Authorization Flow](#5-multi-tenant-authentication--authorization-flow)
6. [Microsoft Azure Best Practices Alignment](#6-microsoft-azure-best-practices-alignment)
7. [Implementation Guidelines](#7-implementation-guidelines)

## 1. Dev Portal Management Database ERD (Direct Registry Access)

The Dev Portal serves as the central management interface, implementing the "Direct Registry Access" pattern for real-time tenant information.

```mermaid
erDiagram
    PortalUsers ||--o{ AuditLogs : creates
    BillingAccounts ||--o{ ModuleBillingAssignments : assigns
    BillingAccounts ||--o{ Invoices : generates
    Invoices ||--o{ InvoiceLineItems : contains
    Invoices ||--o{ PaymentHistory : tracks
    TenantMetricsCache ||--o{ ModuleMetricsCache : contains

    PortalUsers {
        int Id PK
        string Email UK
        string PasswordHash
        string PasswordSalt
        string FirstName
        string LastName
        string Role "SuperAdmin|Support|Developer|BillingAdmin|Viewer"
        bool IsActive
        datetime CreatedDate
        datetime LastLoginDate
        int FailedLoginAttempts
        datetime LockedUntil
    }

    BillingAccounts {
        int Id PK
        string TenantId FK "from Master Registry"
        string AccountCode UK "e.g., ACME-IT-001"
        string AccountName "IT Department"
        string BillingEmail
        string PaymentMethodId
        string Currency "AUD|USD|EUR"
        string CostCenter "optional"
        string PurchaseOrderNumber "optional"
        bool IsActive
        bool IsDefault "default for new modules"
        bool AutoRenew
        datetime NextBillingDate
        string BillingAddress "JSON"
        string TaxId "optional VAT/GST number"
        datetime CreatedDate
    }

    ModuleBillingAssignments {
        int Id PK
        string TenantId FK "from Master Registry"
        string ProductName "Estimate|Trace|Fabmate|QDocs"
        int BillingAccountId FK
        datetime EffectiveFrom
        datetime EffectiveTo "null for current"
        string Notes
        datetime CreatedDate
        int CreatedBy FK
    }

    Invoices {
        int Id PK
        string InvoiceNumber UK
        int BillingAccountId FK
        decimal SubTotal
        decimal TaxAmount
        decimal TotalAmount
        string Currency
        string Status "Draft|Pending|Sent|Paid|Overdue|Cancelled"
        date IssueDate
        date DueDate
        datetime PaidDate
        string BillingPeriod "Monthly|Quarterly|Annual"
        datetime CreatedDate
    }

    InvoiceLineItems {
        int Id PK
        int InvoiceId FK
        string ProductName "Estimate|Trace|Fabmate|QDocs"
        string Description
        int Quantity "user licenses"
        decimal UnitPrice
        decimal Discount
        decimal TaxRate
        decimal LineTotal
        string LicenseType
        datetime ServiceStartDate
        datetime ServiceEndDate
    }

    PaymentHistory {
        int Id PK
        int InvoiceId FK
        decimal Amount
        string PaymentMethod
        string TransactionId
        string Status "Pending|Completed|Failed|Refunded"
        datetime ProcessedDate
        string Notes
    }

    TenantMetricsCache {
        int Id PK
        string TenantId UK "from Master Registry"
        datetime MetricDate
        decimal DatabaseSizeGB
        int TotalUsers
        int TotalProjects
        int ActiveConnections
        decimal DTUUsagePercent
        decimal StorageUsagePercent
        bigint TotalQueries
        datetime CollectedAt
        int TTLMinutes "cache expiry"
    }

    ModuleMetricsCache {
        int Id PK
        int TenantMetricsCacheId FK
        string ProductName "Estimate|Trace|Fabmate|QDocs"
        int ActiveUsers
        int EstimationCount "for Estimate"
        int TakeoffCount "for Trace"
        int WorkOrderCount "for Fabmate"
        int DocumentCount "for QDocs"
        decimal ModuleStorageGB
        datetime CollectedAt
    }

    AuditLogs {
        int Id PK
        int UserId FK
        string Action
        string EntityType
        string EntityId
        string OldValues "JSON"
        string NewValues "JSON"
        datetime Timestamp
        string IpAddress
        string UserAgent
    }
```

### Direct Access Architecture with Enhanced Billing

```mermaid
flowchart LR
    subgraph "Dev Portal Database"
        A[BillingAccounts<br/>Multiple per tenant]
        B[ModuleBillingAssignments]
        C[TenantMetricsCache]
        D[Portal Admin Data]
    end
    
    subgraph "Master Database"
        E[TenantRegistry]
        F[TenantProductModule]
        G[TenantModuleUsage]
    end
    
    subgraph "Tenant Databases"
        H[Tenant A DB]
        I[Tenant B DB]
        J[Tenant C DB]
    end
    
    A -.""TenantId reference"".-> E
    B -.""TenantId + ProductName"".-> F
    C -.""TenantId reference"".-> E
    D ==""Direct Query""==> E
    D ==""Direct Query""==> F
    D ==""Direct Query""==> G
    D -.""Metrics Collection"".-> H
    D -.""Metrics Collection"".-> I
    D -.""Metrics Collection"".-> J
```

## 2. Master Tenant Registry ERD (Enhanced with Module Tracking)

The Master Tenant Registry implements Microsoft's "Tenant Catalog" pattern, serving as the single source of truth for all tenant information.

```mermaid
erDiagram
    TenantRegistry ||--o{ TenantProductModule : has
    TenantRegistry ||--o{ TenantUsageLog : tracks
    TenantProductModule ||--o{ TenantModuleUsage : monitors
    TenantRegistry ||--|| Company : "maps to"

    TenantRegistry {
        int Id PK
        string TenantId UK "unique tenant identifier"
        string DatabaseName UK "database name in Azure"
        string CompanyName
        string CompanyCode UK "for subdomain routing - signup validation"
        string AdminEmail UK "tenant admin email - signup validation"
        datetime CreatedAt
        datetime LastModified
        bool IsActive
        int MaxUsers "global user limit across all modules"
        datetime LastActiveDate "for retention policies"
        string ConnectionStringKeyVaultName "Key Vault secret name"
        string DatabaseServer "Azure SQL server name"
        string ElasticPoolName "Azure Elastic Pool"
        string DefaultBillingAccountCode "optional default billing"
        string Settings "JSON dictionary"
    }

    TenantProductModule {
        int Id PK
        int TenantRegistryId FK
        string ProductName "Estimate|Trace|Fabmate|QDocs"
        string LicenseType "Trial|Standard|Professional|Enterprise"
        bool IsActive
        datetime ActivatedDate
        datetime ExpiryDate
        int MaxUsers "module-specific user limit"
        int MaxConcurrentUsers
        string Features "JSON array of enabled features"
        decimal MonthlyPrice
        string Currency "AUD|USD|EUR"
        datetime CreatedAt
        datetime LastModified
    }

    TenantModuleUsage {
        int Id PK
        int TenantProductModuleId FK
        datetime UsageDate
        int ActiveUsers
        int TotalSessions
        int ApiCalls
        decimal StorageUsedGB "module-specific storage"
        int DocumentsProcessed "for QDocs"
        int TakeoffsCreated "for Trace"
        int WorkOrdersProcessed "for Fabmate"
        int EstimationsCreated "for Estimate"
        datetime CreatedAt
    }

    TenantUsageLog {
        int Id PK
        int TenantRegistryId FK
        string TenantId FK
        datetime LogDate
        decimal DatabaseSizeGB
        int ActiveUsers
        int TotalProjects
        int EstimationsCreated
        int ApiCallCount
        decimal StorageUsedGB
        decimal ComputeHours
        datetime CreatedAt
    }

    Company {
        int Id PK "Links to tenant database"
        string Name
        string Code UK "matches TenantRegistry.CompanyCode"
        bool IsActive
        string SubscriptionLevel
        int MaxUsers
        datetime CreatedDate
        datetime LastModified
    }
```

## 3. Individual Tenant Database ERD (Per-Tenant Schema)

Each tenant receives a complete, isolated database with full business logic and data structures.

```mermaid
erDiagram
    %% Authentication & Authorization
    Company ||--o{ User : employs
    User ||--o{ UserRole : has
    Role ||--o{ UserRole : grants
    User ||--o{ UserAuthMethod : authenticates_via
    User ||--|| UserProfile : has
    User ||--|| UserPreference : has
    User ||--o{ UserActivity : performs

    %% Product Licensing (Fab.OS Products)
    Company ||--o{ ProductLicense : owns
    ProductLicense ||--o{ UserProductAccess : grants
    User ||--o{ UserProductAccess : receives
    ProductRole ||--o{ UserProductRole : defines
    User ||--o{ UserProductRole : assigned

    %% Project Management
    Company ||--o{ Project : owns
    User ||--o{ Project : creates
    Customer ||--o{ Project : commissions
    Project ||--o{ ProjectUser : shared_with
    User ||--o{ ProjectUser : accesses
    Project ||--o{ Package : contains
    Package ||--o{ PackageWorksheet : includes
    
    %% Estimation Entities
    PackageWorksheet ||--o{ ProcessingItem : tracks
    PackageWorksheet ||--o{ WeldingItem : estimates
    ProcessingItem ||--o{ DeliveryBundle : grouped_in
    ProcessingItem ||--o{ PackBundle : packed_in
    WeldingItem ||--o{ WeldingItemConnection : uses
    WeldingConnection ||--o{ WeldingItemConnection : defines
    Package ||--o{ WeldingConnection : configures
    
    %% Efficiency & Time Tracking
    Company ||--o{ EfficiencyRate : defines
    Package ||--o{ EfficiencyRate : uses
    Project ||--o{ EstimationTimeLog : tracked_in
    User ||--o{ EstimationTimeLog : logs

    %% Customer Management
    Company ||--o{ Customer : manages
    Customer ||--o{ Contact : has
    Address ||--o{ Customer : billing_address
    Address ||--o{ Customer : shipping_address

    %% Comment & Notification System
    User ||--o{ Comment : creates
    Comment ||--o{ CommentMention : mentions
    Comment ||--o{ CommentReaction : reacts_to
    User ||--o{ Notification : receives

    %% Core Entities Detail
    Company {
        int Id PK
        string Name
        string Code UK "tenant identifier"
        bool IsActive
        int MaxUsers
        datetime CreatedDate
        datetime LastModified
    }

    User {
        int Id PK
        string Username UK
        string Email UK
        string PasswordHash
        string PasswordSalt
        string SecurityStamp
        string AuthProvider "Local|Microsoft|Google|LinkedIn"
        string ExternalUserId
        string FirstName
        string LastName
        int CompanyId FK
        string JobTitle
        string PhoneNumber
        bool IsActive
        bool IsEmailConfirmed
        datetime LastLoginDate
        datetime CreatedDate
        datetime LastModified
    }

    Role {
        int Id PK
        string RoleName UK "Administrator|Project Manager|Senior Estimator|Estimator|Viewer"
        string Description
        bool CanCreateProjects
        bool CanEditProjects
        bool CanDeleteProjects
        bool CanViewAllProjects
        bool CanManageUsers
        bool CanExportData
        bool CanImportData
    }

    UserRole {
        int UserId PK_FK
        int RoleId PK_FK
        int AssignedBy FK
        datetime AssignedDate
    }

    ProductLicense {
        int Id PK
        int CompanyId FK
        string ProductName "Estimate|Trace|Fabmate|QDocs"
        string LicenseType "Subscription|Perpetual|Trial"
        bool IsActive
        datetime ValidFrom
        datetime ValidUntil
        int MaxUsers
        int MaxConcurrentUsers
        string Features "JSON array"
        int CreatedBy FK
        int ModifiedBy FK
        datetime CreatedDate
        datetime ModifiedDate
    }

    UserProductAccess {
        int Id PK
        int UserId FK
        int ProductLicenseId FK
        bool IsCurrentlyActive
        datetime LastAccessDate
        datetime GrantedDate
    }

    Project {
        int Id PK
        string ProjectName
        string JobNumber
        string Description
        int OwnerId FK
        int CustomerId FK
        string Status "Draft|Active|Completed|Archived"
        decimal ContingencyPercentage
        decimal LaborRate
        decimal EstimatedHours
        bool IsDeleted
        int LastModifiedBy FK
        datetime CreatedDate
        datetime LastModified
    }

    Package {
        int Id PK
        int ProjectId FK
        string PackageNumber
        string Description
        string Status "Draft|In Progress|Completed"
        decimal LaborRatePerHour
        decimal ProcessingEfficiency "percentage"
        int EfficiencyRateId FK
        decimal EstimatedHours
        decimal ActualHours
        bool IsDeleted
        int CreatedBy FK
        datetime CreatedDate
        datetime LastModified
    }

    Customer {
        int Id PK
        int CompanyId FK
        string CompanyName
        string ABN
        string Website
        int BillingAddressId FK
        int ShippingAddressId FK
        bool IsActive
        int CreatedById FK
        int ModifiedById FK
        datetime CreatedDate
        datetime ModifiedDate
    }

    EfficiencyRate {
        int Id PK
        int CompanyId FK
        string Name
        string Description
        decimal EfficiencyPercentage
        bool IsDefault
        bool IsActive
        datetime CreatedDate
        datetime LastModified
    }
```

## 4. Complete Multi-Tenant Architecture Overview (Direct Registry Access)

```mermaid
graph TB
    subgraph "Azure Cloud Infrastructure"
        subgraph "Application Layer"
            FabOS[Fab.OS Multi-tenant App<br/>Single Blazor Server Application<br/>• fabosplatform.com (marketing/signup/login)<br/>• {tenant}.fabosplatform.com (direct tenant access)]  
            DevPortal[Dev Portal Web App<br/>admin.fabosplatform.com<br/>- Tenant Management<br/>- Billing & Analytics<br/>- Support Interface]
            DevPortalDB[(Dev Portal Database<br/>sqldb-fabos-devportal<br/>Billing & Metrics Cache Only)]
            
            DevPortal -.-> DevPortalDB
        end
        
        subgraph "Tenant Registry Layer"
            MasterDB[(Master Database<br/>- TenantRegistry<br/>- TenantProductModule<br/>- TenantModuleUsage)]
            KeyVault[Azure Key Vault<br/>Connection Strings per Tenant]
            
            FabOS ==>|Validate Signup & Get Tenant Config| MasterDB
            DevPortal ==>|Direct Query| MasterDB
            FabOS -.-> KeyVault
            DevPortal -.-> KeyVault
        end
        
        subgraph "Tenant Provisioning"
            ProvisioningFunc[Azure Functions<br/>- Tenant Provisioning<br/>- Database Creation<br/>- Schema Deployment]
            AzureSQL[Azure SQL Server<br/>nwiapps.database.windows.net]
            
            ProvisioningFunc -.-> AzureSQL
            ProvisioningFunc -.-> KeyVault
            ProvisioningFunc -.-> MasterDB
        end
        
        subgraph "Tenant Databases (Isolated)"
            TenantA_DB[(Tenant A Database<br/>sqldb-tenant-acme<br/>ACME Steel Works)]
            TenantB_DB[(Tenant B Database<br/>sqldb-tenant-steelco<br/>SteelCo Industries)]
            TenantC_DB[(Tenant C Database<br/>sqldb-tenant-metalworks<br/>MetalWorks Ltd)]
        end
        
        subgraph "Shared Services"
            FabOSAuth[Fab.OS Auth Service<br/>- JWT with Tenant Context<br/>- Product Licensing<br/>- Social Login]
            AppGateway[Application Gateway<br/>- Subdomain Routing<br/>- SSL Termination<br/>- WAF]
            CDN[Azure CDN<br/>Static Assets]
        end
    end
    
    subgraph "External Users"
        NewUser[New Users<br/>Via marketing site]
        AdminUser[Support Admin<br/>DevPortal Access]
        ExistingUser[Existing Users<br/>Direct bookmark or login]
    end
    
    %% User Access Flows
    NewUser -->|"fabosplatform.com"| AppGateway
    ExistingUser -->|"Bookmark: acme.fabosplatform.com"| AppGateway
    ExistingUser -->|"Or: fabosplatform.com/login"| AppGateway
    AdminUser -->|"admin.fabosplatform.com"| AppGateway
    
    %% Gateway Routing based on subdomain
    AppGateway -->|"fabosplatform.com/*"| FabOS
    AppGateway -->|"{tenant}.fabosplatform.com"| FabOS
    AppGateway -->|"admin.fabosplatform.com"| DevPortal
    
    %% Tenant Context Flow
    FabOS -->|"Subdomain: acme"| TenantA_DB
    FabOS -->|"Subdomain: steelco"| TenantB_DB
    FabOS -->|"Subdomain: metalworks"| TenantC_DB
    
    %% Signup and Provisioning Flow
    FabOS -->|"Trigger Provisioning"| ProvisioningFunc
    
    %% Authentication Flow
    FabOS -.->|"Get tenant context"| FabOSAuth
    FabOSAuth -.->|"Validate against tenant DB"| TenantA_DB
    FabOSAuth -.->|"Validate against tenant DB"| TenantB_DB
    FabOSAuth -.->|"Validate against tenant DB"| TenantC_DB
    
    %% Direct Registry Access
    DevPortal ==>|"Query all tenants"| MasterDB
    FabOS ==>|"Update tenant status"| MasterDB
    
    %% Monitoring Flow
    DevPortal -.->|"Metrics Collection"| TenantA_DB
    DevPortal -.->|"Metrics Collection"| TenantB_DB
    DevPortal -.->|"Metrics Collection"| TenantC_DB
    
    %% Data Isolation Boundaries
    classDef multiTenantApp fill:#ffeb3b,stroke:#f57f17,stroke-width:3px
    classDef tenantDB fill:#e1f5fe,stroke:#01579b,stroke-width:3px
    classDef managementLayer fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef sharedService fill:#e8f5e8,stroke:#1b5e20,stroke-width:2px
    classDef directAccess stroke:#ff6b6b,stroke-width:4px,stroke-dasharray: 5 5
    classDef user fill:#fff3e0,stroke:#ff6f00,stroke-width:2px
    
    class FabOS multiTenantApp
    class TenantA_DB,TenantB_DB,TenantC_DB tenantDB
    class DevPortal,DevPortalDB managementLayer
    class MasterDB directAccess,managementLayer
    class FabOSAuth,AppGateway,CDN sharedService
    class NewUser,AdminUser,TenantUsers user
```

### Key Architecture Changes

1. **Single Multi-tenant Application**: One Fab.OS app serves ALL tenants based on subdomain
2. **Subdomain-based Routing**: Runtime tenant detection (fab-os.com vs {tenant}.fab-os.com)
3. **Direct Registry Access**: Both Fab.OS app and Dev Portal query Master Registry directly
4. **No Duplicate Data**: Dev Portal only stores billing data and metrics cache
5. **Dynamic Database Connection**: Connection string resolved per request based on subdomain
6. **Enhanced Billing**: Multiple billing accounts per tenant with module-level assignment
7. **True SaaS Architecture**: Massive cost savings with single application deployment

## 5. Multi-Tenant Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant NewUser as New User
    participant User as Tenant User
    participant Gateway as Application Gateway
    participant MainApp as Main Fab.OS App
    participant TenantApp as Tenant Application
    participant FabOSAuth as Fab.OS Auth Service
    participant TenantDB as Tenant Database
    participant KeyVault as Azure Key Vault
    participant DevPortal as Dev Portal
    participant MasterDB as Master Registry
    participant ProvFunc as Provisioning Function

    Note over NewUser,MasterDB: Signup Flow
    
    NewUser->>Gateway: 1. Visit fabosplatform.com (marketing)
    Gateway->>FabOS: 2. Route to Fab.OS App
    FabOS->>NewUser: 3. Show landing page
    NewUser->>FabOS: 4. Click "Sign Up" button
    FabOS->>NewUser: 5. Redirect to fabosplatform.com/signup
    NewUser->>FabOS: 6. Fill signup form
    FabOS->>MasterDB: 7. Validate email/domain/code
    MasterDB-->>FabOS: 8. Validation results  
    NewUser->>FabOS: 9. Complete signup
    FabOS->>ProvFunc: 10. Trigger provisioning
    ProvFunc->>MasterDB: 11. Create tenant entry
    ProvFunc->>TenantDB: 12. Create database
    ProvFunc-->>FabOS: 13. Provisioning complete
    FabOS-->>NewUser: 14. Redirect to acme.fabosplatform.com/welcome

    Note over User,MasterDB: Direct Tenant Access (Bookmarked)

    User->>Gateway: 15. Direct access: acme.fabosplatform.com
    Gateway->>FabOS: 16. Route to Fab.OS App
    FabOS->>FabOS: 17. Extract tenant from subdomain<br/>("acme")
    FabOS->>MasterDB: 18. Resolve tenant info & connection string
    MasterDB-->>FabOS: 19. Return tenant metadata
    FabOS->>User: 20. Show tenant login page
    User->>FabOS: 21. Enter credentials
    FabOS->>FabOSAuth: 22. Authenticate with tenant context
    
    Note over User,MasterDB: Central Login Flow
    
    User->>Gateway: 23. Visit fabosplatform.com/login
    Gateway->>FabOS: 24. Route to central login
    FabOS->>User: 25. Show login form with tenant selection
    User->>FabOS: 26. Enter email/password
    FabOS->>MasterDB: 27. Identify user's tenant(s)
    MasterDB-->>FabOS: 28. Return tenant list
    FabOS-->>User: 29. Redirect to tenant: acme.fabosplatform.com
    
    User->>TenantApp: 6. Submit credentials
    TenantApp->>FabOSAuth: 7. Authenticate(email, password, tenantId)
    
    FabOSAuth->>KeyVault: 8. Get tenant connection string
    KeyVault-->>FabOSAuth: 9. Return connection string
    
    FabOSAuth->>TenantDB: 10. Query user + company data
    TenantDB-->>FabOSAuth: 11. Return user info
    
    FabOSAuth->>FabOSAuth: 12. Generate JWT with claims:<br/>- UserId<br/>- CompanyId<br/>- TenantId<br/>- ProductAccess[]
    
    FabOSAuth-->>TenantApp: 13. Return AuthResult + JWT
    TenantApp-->>User: 14. Set auth cookie + redirect

    Note over User,MasterDB: Product Access Check

    User->>TenantApp: 15. Access Estimate module
    FabOS->>FabOS: 16. Validate JWT claims<br/>Check Product.Estimate access
    
    alt Has Product Access
        FabOS->>TenantDB: 17. Query estimation data<br/>(using tenant-specific connection)
        TenantDB-->>FabOS: 18. Return company-scoped data
        FabOS-->>User: 19. Display estimation interface
    else No Product Access
        FabOS-->>User: 20. Access Denied
    end

    Note over User,MasterDB: Dev Portal Monitoring

    DevPortal->>MasterDB: 21. Get all active tenants
    MasterDB-->>DevPortal: 22. Return tenant list
    
    loop For each tenant
        DevPortal->>TenantDB: 23. Collect metrics<br/>(read-only connection)
        TenantDB-->>DevPortal: 24. Return usage statistics
    end
    
    DevPortal->>DevPortal: 25. Aggregate cross-tenant analytics
```

## Authentication Architecture

### Dual Authentication Strategy

The platform implements a dual authentication strategy optimized for different client types:

#### **Cookie Authentication (Blazor Web Apps)**
- Used for: Fab.OS main app, Dev Portal, all web interfaces
- Benefits: Stateful, sliding expiration, XSS protection via httpOnly
- Session: 8-hour timeout with sliding window
- Perfect for SignalR/Blazor Server connections

#### **JWT Authentication (Mobile & API)**
- Used for: Android/iOS apps, external API integrations
- Benefits: Stateless, offline support, standard mobile pattern
- Token lifetime: 4 hours with refresh token support
- Contains tenant context and module permissions

### JWT Token Structure (Mobile/API Only)

```json
{
  "sub": "user123",
  "email": "john@acme.com",
  "companyId": "42",
  "tenantId": "acme-steel-works",
  "tenantCode": "acme",
  "products": [
    {
      "name": "Estimate",
      "role": "Senior Estimator",
      "features": ["time-tracking", "welding-dashboard"]
    },
    {
      "name": "Trace", 
      "role": "Viewer",
      "features": ["basic-tracking"]
    }
  ],
  "roles": ["Senior Estimator"],
  "deviceId": "android-device-123",  // For mobile tracking
  "iat": 1640995200,
  "exp": 1641009600,  // 4 hours for mobile
  "iss": "FabOS",
  "aud": "api.fabosplatform.com"
}
```

## 6. Microsoft Azure Best Practices Alignment

Our architecture aligns with Microsoft's documented best practices for multi-tenant SaaS applications:

### Tenant Catalog Pattern ✅
Microsoft recommends: *"The catalog is a database that maintains the mapping between tenants and their data"*
- Our **Master Tenant Registry** implements this pattern
- Direct queries avoid data corruption risks
- Single source of truth for tenant information

### Database-Per-Tenant Isolation ✅
Microsoft states: *"Provides strong tenant isolation... easier to customize schema for individual tenants"*
- Complete data isolation per tenant
- Simplified compliance and security
- Per-tenant backup and restore capabilities

### Elastic Pool Support ✅
Microsoft guidance: *"Elastic pools enable you to share compute resources between multiple databases"*
- `ElasticPoolName` field in registry
- Cost optimization for small tenants
- Resource sharing with isolation

### Module-Based Licensing ✅
Implements Microsoft's "Hybrid approach" allowing:
- Flexible pricing per module
- Gradual feature rollout
- Per-tenant customization

### Security Best Practices ✅
- Row-level security within tenant databases
- Key Vault for connection string management
- Read-only access for monitoring
- Comprehensive audit logging

## 7. Implementation Guidelines

### Phase 1: Core Infrastructure
1. Deploy Master Tenant Registry database
2. Implement TenantRegistry and TenantProductModule tables
3. Set up Azure Key Vault integration
4. Configure cross-database access permissions
5. **Implement Signup Validation Service** for modern conflict detection

### Phase 2: Platform Development
1. Build Main Fab.OS App with landing page at fab-os.com
2. Implement integrated signup flow with validation
3. Remove MonitoredDatabases table from Dev Portal
4. Implement TenantRegistryService for direct queries
5. Add caching layer for performance

### Phase 3: Tenant Provisioning
1. Build Azure Functions for automated provisioning
2. Implement database creation templates
3. Automate schema deployment
4. Create tenant onboarding workflows

### Phase 4: Module Management
1. Implement module activation/deactivation
2. Build usage tracking per module
3. Create billing integration with multiple accounts
4. Add module-specific analytics
5. Implement module-to-billing-account assignment
6. Build billing account management UI

### Key Implementation Notes

#### Direct Registry Access Benefits
- **Real-time data**: Always current tenant information
- **No sync issues**: Eliminates data duplication problems
- **Simplified architecture**: Fewer moving parts
- **Better performance**: Targeted queries with caching

#### Caching Strategy
- Cache expensive metrics (database size, user counts)
- Short TTL (60 minutes) for freshness
- Cache invalidation on tenant updates
- Separate cache per metric type

#### Security Considerations
- Dev Portal uses read-only access to tenant databases
- All updates go through proper APIs
- Connection strings stored in Key Vault
- Tenant isolation enforced at database level

## Summary

This architecture provides a robust, scalable foundation for the Fab.OS multi-tenant platform:

### Key Benefits
1. **Complete Isolation**: Database-per-tenant ensures data security
2. **Flexible Licensing**: Module-based approach supports various business models
3. **Enterprise Billing**: Multiple billing accounts with module-level assignment
4. **Modern Signup Experience**: Prevents duplicates with user-friendly conflict resolution
5. **Operational Excellence**: Centralized management with distributed execution
6. **Azure Alignment**: Follows Microsoft's documented best practices
7. **Future-Proof**: Supports growth from tens to thousands of tenants
8. **Department Control**: Different departments can manage their own module costs

### Architecture Principles
- **Single Source of Truth**: Master Tenant Registry enables real-time validation
- **Direct Access**: No data duplication between systems
- **Module Granularity**: Fine-grained control over features
- **Cache for Performance**: Strategic caching of expensive operations
- **Security by Design**: Multiple layers of isolation and access control
- **Modern UX**: Email/domain conflict detection with clear resolution paths

### Signup Validation Integration
The Master Tenant Registry's unique constraints naturally support:
- **Email conflict detection**: Query `AdminEmail` field
- **Domain analysis**: Pattern matching on email domains  
- **Company code validation**: Check `CompanyCode` uniqueness
- **Real-time suggestions**: Generate alternatives for taken codes

### True Multi-tenant SaaS Architecture
The single Fab.OS application provides:
- **Marketing website** (fabosplatform.com): Landing page with signup/login links
- **Integrated signup** (fabosplatform.com/signup): Full validation and provisioning
- **Dual login options**:
  - Central login (fabosplatform.com/login) - identifies tenant and redirects
  - Direct tenant login ({tenant}.fabosplatform.com) - bookmarkable for regular users
- **Subdomain-based tenant detection**: Runtime resolution of tenant context
- **Dynamic database switching**: Connection resolved per request based on subdomain
- **Massive cost savings**: One application serves unlimited tenants
- **Simplified operations**: Single deployment, update, and monitoring

### User Experience Flows
1. **New Users**: Marketing site → Signup → Provision → Redirect to tenant
2. **Existing Users (Bookmark)**: Direct to acme.fabosplatform.com → Tenant login
3. **Existing Users (Central)**: fabosplatform.com/login → Identify tenant → Redirect

The architecture implements a true multi-tenant SaaS pattern with flexible access patterns, supporting unlimited growth while maintaining operational simplicity, security, and cost efficiency.