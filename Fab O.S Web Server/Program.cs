using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Data.Seeders;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Services.Implementations;
using FabOS.WebServer.Services;
using FabOS.WebServer.Authentication;
using FabOS.WebServer.Middleware;
using FabOS.WebServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FabOS.WebServer.Components;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SignalR (Blazor Server) with increased timeouts for large PDF transfers
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10); // Keep disconnected circuits for 10 minutes
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2); // Increased from 30s default to 2min for PDF export
        options.MaxBufferedUnacknowledgedRenderBatches = 10;
    })
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(10); // Server waits 10 minutes for client pings before disconnecting
        options.HandshakeTimeout = TimeSpan.FromMinutes(1); // Allow 1 minute for initial handshake
        options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Send keep-alive pings every 15 seconds
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32 MB max message size for large PDF transfers
    });

// Add Pooled DbContext Factory for scenarios requiring separate DbContext instances (e.g., breadcrumb builders)
// This must come BEFORE AddDbContext to avoid scoped options conflict
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Add Entity Framework with connection resiliency for components and services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Add Memory Cache and Distributed Cache for sessions
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Add Hybrid Authentication System
builder.Services.AddAuthentication(options =>
{
    // Default to cookie authentication for web requests
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // Cookie configuration for web applications
    options.Cookie.Name = ".FabOS.Auth";
    options.Cookie.HttpOnly = true;  // XSS protection
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.ReturnUrlParameter = "returnUrl";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // JWT configuration for mobile and API requests
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"))),
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Allow token from query string for SignalR connections
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            // Add any additional token validation logic here
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            // Log authentication failures
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        }
    };
})
.AddMicrosoftAccount(options =>
{
    // Microsoft/Entra ID authentication for invited users
    var microsoftAuth = builder.Configuration.GetSection("MicrosoftAuth");
    options.ClientId = microsoftAuth["ClientId"] ?? "";
    options.ClientSecret = microsoftAuth["ClientSecret"] ?? "";
    options.CallbackPath = "/signin-microsoft";

    options.SaveTokens = true;

    options.Events.OnCreatingTicket = async context =>
    {
        var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return;
        }

        // Get invitation service from DI
        var invitationService = context.HttpContext.RequestServices
            .GetRequiredService<FabOS.WebServer.Services.Interfaces.IInvitationService>();

        // Check if there's a pending invitation for this email
        var invitation = await invitationService.ValidateByEmailAsync(email);

        if (invitation != null && invitation.Company != null)
        {
            // Add company claims for tenant routing
            var identity = (System.Security.Claims.ClaimsIdentity)context.Principal!.Identity!;
            identity.AddClaim(new System.Security.Claims.Claim("company_id", invitation.CompanyId.ToString()));
            identity.AddClaim(new System.Security.Claims.Claim("company_code", invitation.Company.Code));
            identity.AddClaim(new System.Security.Claims.Claim("company_name", invitation.Company.Name));
            identity.AddClaim(new System.Security.Claims.Claim("pending_invitation", "true"));
            identity.AddClaim(new System.Security.Claims.Claim("invitation_token", invitation.Token));
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    // Default policy requires authentication, except for test routes
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
        
    // Allow anonymous access to test routes
    options.AddPolicy("AllowAnonymous", policy =>
        policy.RequireAssertion(_ => true));
        
    // API policy specifically for JWT tokens
    options.AddPolicy("ApiPolicy", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser());
              
    // Web policy specifically for cookies
    options.AddPolicy("WebPolicy", policy =>
        policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser());
});

// Add custom authentication services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICookieAuthenticationService, CookieAuthenticationService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// Add custom services
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IModuleFeatureService, ModuleFeatureService>();

// Register Modern Breadcrumb Navigation Service (URL-based, hierarchical)
builder.Services.AddScoped<FabOS.WebServer.Services.BreadcrumbNavigationService>();

// Register Sidebar and Navigation Services
builder.Services.AddScoped<FabOS.WebServer.Services.SidebarService>();
builder.Services.AddScoped<FabOS.WebServer.Services.NavigationService>();

builder.Services.AddScoped<IViewPreferencesService, ViewPreferencesService>();
builder.Services.AddScoped<IViewStateManager, ViewStateManager>();
builder.Services.AddScoped<NumberSeriesService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Add Tenant Service for multi-tenancy
builder.Services.AddScoped<ITenantService, TenantService>();

// Add Signup and Invitation Services for user registration
builder.Services.AddScoped<ISignupValidationService, SignupValidationService>();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Register OCR service with all dependencies for multi-tenant support
builder.Services.AddHttpClient<AzureOcrService>();
builder.Services.AddScoped<IAzureOcrService, AzureOcrService>();
builder.Services.AddHttpContextAccessor(); // For multi-tenant context

// Register Nutrient DWS (Document Web Service) session service for cloud-hosted PDF viewing
builder.Services.AddHttpClient(); // Ensure HttpClientFactory is available
builder.Services.AddScoped<INutrientDwsSessionService, NutrientDwsSessionService>();

// Register ABN Lookup service
builder.Services.AddHttpClient<IAbnLookupService, AbnLookupService>();
builder.Services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

// Register QDocs CAD Parser services
builder.Services.AddScoped<ISmlxParserService, SmlxParserService>();
builder.Services.AddScoped<IIfcParserService, IfcParserService>();

// Register SharePoint services
builder.Services.Configure<FabOS.WebServer.Models.Configuration.SharePointSettings>(
    builder.Configuration.GetSection("SharePoint"));
builder.Services.AddScoped<ISharePointService, SharePointService>();
builder.Services.AddScoped<ISharePointSyncService, SharePointSyncService>();

// Register Cloud Storage Providers (Multi-provider support for SharePoint, GoogleDrive, Dropbox, AzureBlob)
builder.Services.AddScoped<FabOS.WebServer.Services.Implementations.CloudStorage.SharePointStorageProvider>();
builder.Services.AddScoped<FabOS.WebServer.Services.Implementations.CloudStorage.GoogleDriveStorageProvider>();
builder.Services.AddScoped<FabOS.WebServer.Services.Implementations.CloudStorage.DropboxStorageProvider>();
builder.Services.AddScoped<FabOS.WebServer.Services.Implementations.CloudStorage.AzureBlobStorageProvider>();
builder.Services.AddScoped<FabOS.WebServer.Services.Implementations.CloudStorage.CloudStorageProviderFactory>();

// Register Trace and Takeoff services
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<ITraceService, TraceService>();
builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();
builder.Services.AddScoped<ITakeoffService, TakeoffService>();
builder.Services.AddScoped<ITakeoffRevisionService, TakeoffRevisionService>();
builder.Services.AddScoped<IPackageDrawingService, PackageDrawingService>();
builder.Services.AddScoped<IScaleCalibrationService, ScaleCalibrationService>();
builder.Services.AddScoped<ITakeoffCatalogueService, TakeoffCatalogueService>();
builder.Services.AddScoped<ICatalogueService, CatalogueService>();
builder.Services.AddScoped<IPdfCalibrationService, PdfCalibrationService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IPdfAnnotationService, PdfAnnotationService>();
builder.Services.AddScoped<IPdfLockService, PdfLockService>();
builder.Services.AddScoped<IMeasurementExportService, MeasurementExportService>();
builder.Services.AddScoped<ISurfaceCoatingService, SurfaceCoatingService>();

// Register SignalR Hub for real-time measurement updates (cross-tab/cross-user notifications)
builder.Services.AddSignalR();
builder.Services.AddScoped<FabOS.WebServer.Hubs.IMeasurementHubService, FabOS.WebServer.Hubs.MeasurementHubService>();

// Register background services
builder.Services.AddHostedService<FabOS.WebServer.Services.BackgroundServices.PdfLockCleanupService>();

// Register Asset Module Services
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IEquipmentService, FabOS.WebServer.Services.Implementations.Assets.EquipmentService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IMaintenanceService, FabOS.WebServer.Services.Implementations.Assets.MaintenanceService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IEquipmentCategoryService, FabOS.WebServer.Services.Implementations.Assets.EquipmentCategoryService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IQRCodeService, FabOS.WebServer.Services.Implementations.Assets.QRCodeService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.ILabelPrintingService, FabOS.WebServer.Services.Implementations.Assets.LabelPrintingService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.ICertificationService, FabOS.WebServer.Services.Implementations.Assets.CertificationService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IEquipmentManualService, FabOS.WebServer.Services.Implementations.Assets.EquipmentManualService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IKitTemplateService, FabOS.WebServer.Services.Implementations.Assets.KitTemplateService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IEquipmentKitService, FabOS.WebServer.Services.Implementations.Assets.EquipmentKitService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.IKitCheckoutService, FabOS.WebServer.Services.Implementations.Assets.KitCheckoutService>();
builder.Services.AddScoped<FabOS.WebServer.Services.Interfaces.ILocationService, FabOS.WebServer.Services.Implementations.Assets.LocationService>();

// Add MVC services (includes controllers, views, and all necessary services)
builder.Services.AddMvc();

// Add AutoMapper - using explicit configuration to avoid ambiguity
builder.Services.AddSingleton(provider => new MapperConfiguration(cfg =>
{
    cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
}).CreateMapper());

// Add Swagger/OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Fab.OS Trace & Takeoff API",
        Version = "v1",
        Description = "API for material traceability and PDF takeoff operations",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Fab.OS Support",
            Email = "support@fabos.com"
        }
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Enable annotations (commented out - requires Swashbuckle.AspNetCore.Annotations package)
    // c.EnableAnnotations();
});

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Initialize database and seed default data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("[Program] Initializing database...");

        var seeder = new DatabaseSeeder(context, scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>());
        await seeder.SeedAsync();

        logger.LogInformation("[Program] Database initialization completed");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "[Program] Error during database initialization");
        // Don't throw - allow app to continue, it will fail later if DB is really broken
    }
}

// Apply pending migrations (one-time operation) - COMPLETED
// TakeoffRevisionSystem migration applied successfully on 2025-10-05
/*
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrationRunner = new MigrationRunner(context);

    // First, sync migration history for earlier migrations
    await migrationRunner.SyncMigrationHistoryAsync();

    // Then apply the TakeoffRevisionSystem migration
    await migrationRunner.ApplyRevisionMigrationAsync();
}
*/

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fab.OS Trace & Takeoff API v1");
    c.RoutePrefix = "api-docs"; // Access at /api-docs
});

app.UseHttpsRedirection();

// Disable caching for static files in development to ensure JavaScript updates are loaded
var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
// Add MIME type mapping for Foxit SDK .brotli files (precompressed WASM files)
provider.Mappings[".brotli"] = "application/wasm";
provider.Mappings[".wasm"] = "application/wasm";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            // Aggressive cache-busting in development for JavaScript, CSS, and WASM files
            var path = ctx.Context.Request.Path.Value?.ToLower() ?? "";
            if (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".wasm") || path.EndsWith(".brotli"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
                // Add ETag removal to force fresh content
                ctx.Context.Response.Headers.Remove("ETag");
            }
        }
        else
        {
            // In production, use long cache for versioned files
            var path = ctx.Context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("?v="))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
            }
        }
    }
});

// Add Session middleware before Authentication
app.UseSession();

// CRITICAL: UseRouting must come before UseAuthorization when using FallbackPolicy
// This allows the authorization middleware to know which endpoint is being accessed
app.UseRouting();

// Add Hybrid Authentication middleware
app.UseMiddleware<HybridAuthenticationMiddleware>();

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Anti-forgery
app.UseAntiforgery();

// Map Controllers for both API and MVC endpoints
app.MapControllers();

// Map Razor Pages
app.MapRazorPages();

// Map default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Razor Components for web application
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map SignalR Hub for real-time measurement updates
app.MapHub<FabOS.WebServer.Hubs.MeasurementHub>("/measurementHub");

app.Run();
