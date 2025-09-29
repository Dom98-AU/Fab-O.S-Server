using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Services.Implementations;
using FabOS.WebServer.Services;
using FabOS.WebServer.Authentication;
using FabOS.WebServer.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FabOS.WebServer.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<FabOS.WebServer.Services.BreadcrumbService>();
builder.Services.AddScoped<IViewPreferencesService, ViewPreferencesService>();
builder.Services.AddScoped<IViewStateManager, ViewStateManager>();

// Register OCR service with all dependencies for multi-tenant support
builder.Services.AddHttpClient<AzureOcrService>();
builder.Services.AddScoped<IAzureOcrService, AzureOcrService>();
builder.Services.AddHttpContextAccessor(); // For multi-tenant context

// Register ABN Lookup service
builder.Services.AddHttpClient<IAbnLookupService, AbnLookupService>();
builder.Services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

// Register SharePoint services
builder.Services.Configure<FabOS.WebServer.Models.Configuration.SharePointSettings>(
    builder.Configuration.GetSection("SharePoint"));
builder.Services.AddScoped<ISharePointService, SharePointService>();

// Add MVC services (includes controllers, views, and all necessary services)
builder.Services.AddMvc();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add Session middleware before Authentication
app.UseSession();

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

app.Run();
