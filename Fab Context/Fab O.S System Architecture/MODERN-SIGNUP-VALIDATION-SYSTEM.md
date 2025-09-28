# Modern Signup Validation System - Fab.OS Platform

## Overview

This document outlines the implementation of modern best practice signup validation for the Fab.OS multi-tenant platform, following patterns used by Slack, Microsoft Teams, and other leading SaaS platforms.

## Validation Strategy

### **Prevent + Redirect Approach**
- Email exists → Redirect to login
- Domain exists → Offer to join existing or create separate
- No conflicts → Create new tenant

## Implementation Components

### 1. Signup Validation Service

```csharp
public interface ISignupValidationService
{
    Task<SignupValidationResult> ValidateSignupAsync(SignupRequest request);
    Task<List<string>> GenerateCompanyCodeSuggestionsAsync(string requestedCode);
    Task<DomainAnalysisResult> AnalyzeDomainAsync(string email);
}

public class SignupValidationService : ISignupValidationService
{
    private readonly string _masterConnectionString;
    private readonly ILogger<SignupValidationService> _logger;
    
    public SignupValidationService(IConfiguration configuration, ILogger<SignupValidationService> logger)
    {
        _masterConnectionString = configuration.GetConnectionString("MasterDb")!;
        _logger = logger;
    }
    
    public async Task<SignupValidationResult> ValidateSignupAsync(SignupRequest request)
    {
        using var connection = new SqlConnection(_masterConnectionString);
        var result = new SignupValidationResult();
        
        // Step 1: Check if exact email already exists as tenant admin
        var existingAdmin = await connection.QueryFirstOrDefaultAsync<ExistingTenantInfo>(@"
            SELECT 
                tr.TenantId,
                tr.CompanyName,
                tr.CompanyCode,
                tr.AdminEmail,
                tr.CreatedAt
            FROM TenantRegistry tr 
            WHERE LOWER(tr.AdminEmail) = LOWER(@Email) 
            AND tr.IsActive = 1
        ", new { Email = request.Email });
        
        if (existingAdmin != null)
        {
            result.ConflictType = ConflictType.EmailExists;
            result.IsValid = false;
            result.Message = $"This email is already registered as admin for {existingAdmin.CompanyName}";
            result.ExistingTenant = existingAdmin;
            result.SuggestedActions = new[]
            {
                new SuggestedAction 
                { 
                    Type = "SignIn", 
                    Text = "Sign in to existing account",
                    Url = $"https://{existingAdmin.CompanyCode}.fab-os.com/login",
                    IsPrimary = true
                },
                new SuggestedAction 
                { 
                    Type = "Support", 
                    Text = "Contact support for help",
                    Url = "/support",
                    IsPrimary = false
                }
            };
            return result;
        }
        
        // Step 2: Check if company code is available
        var companyCodeExists = await connection.QueryFirstOrDefaultAsync<ExistingTenantInfo>(@"
            SELECT 
                tr.TenantId,
                tr.CompanyName,
                tr.CompanyCode,
                tr.AdminEmail
            FROM TenantRegistry tr 
            WHERE LOWER(tr.CompanyCode) = LOWER(@CompanyCode) 
            AND tr.IsActive = 1
        ", new { CompanyCode = request.CompanyCode });
        
        if (companyCodeExists != null)
        {
            result.ConflictType = ConflictType.CompanyCodeExists;
            result.IsValid = false;
            result.Message = $"Company code '{request.CompanyCode}' is already taken by {companyCodeExists.CompanyName}";
            result.CodeSuggestions = await GenerateCompanyCodeSuggestionsAsync(request.CompanyCode);
            return result;
        }
        
        // Step 3: Analyze email domain for potential existing workspaces
        var domainAnalysis = await AnalyzeDomainAsync(request.Email);
        if (domainAnalysis.HasExistingTenants)
        {
            result.ConflictType = ConflictType.DomainExists;
            result.IsValid = false;
            result.Message = $"Your company domain (@{domainAnalysis.Domain}) already has existing workspaces";
            result.DomainAnalysis = domainAnalysis;
            result.SuggestedActions = new[]
            {
                new SuggestedAction 
                { 
                    Type = "JoinExisting", 
                    Text = $"Join {domainAnalysis.ExistingTenants.First().CompanyName}",
                    Url = $"https://{domainAnalysis.ExistingTenants.First().CompanyCode}.fab-os.com/request-access",
                    IsPrimary = true
                },
                new SuggestedAction 
                { 
                    Type = "CreateSeparate", 
                    Text = "Create separate workspace anyway",
                    Url = "/signup/force-new",
                    IsPrimary = false
                }
            };
            return result;
        }
        
        // Step 4: All validations passed
        result.IsValid = true;
        result.Message = "Ready to create new workspace";
        result.PreviewUrl = $"https://{request.CompanyCode}.fab-os.com";
        
        return result;
    }
    
    public async Task<DomainAnalysisResult> AnalyzeDomainAsync(string email)
    {
        var domain = email.Split('@')[1].ToLower();
        using var connection = new SqlConnection(_masterConnectionString);
        
        var existingTenants = await connection.QueryAsync<ExistingTenantInfo>(@"
            SELECT 
                tr.TenantId,
                tr.CompanyName,
                tr.CompanyCode,
                tr.AdminEmail,
                tr.CreatedAt
            FROM TenantRegistry tr 
            WHERE LOWER(tr.AdminEmail) LIKE @DomainPattern
            AND tr.IsActive = 1
            ORDER BY tr.CreatedAt DESC
        ", new { DomainPattern = $"%@{domain}" });
        
        return new DomainAnalysisResult
        {
            Domain = domain,
            HasExistingTenants = existingTenants.Any(),
            ExistingTenants = existingTenants.ToList(),
            TenantCount = existingTenants.Count()
        };
    }
    
    public async Task<List<string>> GenerateCompanyCodeSuggestionsAsync(string requestedCode)
    {
        using var connection = new SqlConnection(_masterConnectionString);
        var suggestions = new List<string>();
        
        // Generate potential alternatives
        var candidates = new[]
        {
            $"{requestedCode}-2025",
            $"{requestedCode}-works",
            $"{requestedCode}-ltd",
            $"{requestedCode}-inc",
            $"{requestedCode}-corp",
            $"{requestedCode}-team"
        };
        
        foreach (var candidate in candidates)
        {
            var exists = await connection.QueryFirstOrDefaultAsync<int>(@"
                SELECT COUNT(*) FROM TenantRegistry 
                WHERE LOWER(CompanyCode) = LOWER(@Code) AND IsActive = 1
            ", new { Code = candidate });
            
            if (exists == 0)
            {
                suggestions.Add(candidate);
            }
            
            if (suggestions.Count >= 3) break;
        }
        
        return suggestions;
    }
}
```

### 2. Data Transfer Objects

```csharp
public class SignupRequest
{
    public string Email { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> SelectedModules { get; set; } = new();
    public bool ForceCreateSeparate { get; set; } = false;
}

public class SignupValidationResult
{
    public bool IsValid { get; set; }
    public ConflictType ConflictType { get; set; } = ConflictType.None;
    public string Message { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public ExistingTenantInfo? ExistingTenant { get; set; }
    public DomainAnalysisResult? DomainAnalysis { get; set; }
    public List<string> CodeSuggestions { get; set; } = new();
    public SuggestedAction[] SuggestedActions { get; set; } = Array.Empty<SuggestedAction>();
}

public class ExistingTenantInfo
{
    public string TenantId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DomainAnalysisResult
{
    public string Domain { get; set; } = string.Empty;
    public bool HasExistingTenants { get; set; }
    public List<ExistingTenantInfo> ExistingTenants { get; set; } = new();
    public int TenantCount { get; set; }
}

public class SuggestedAction
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public enum ConflictType
{
    None,
    EmailExists,
    CompanyCodeExists,
    DomainExists
}
```

### 3. Signup Controller with Validation

```csharp
[ApiController]
[Route("api/[controller]")]
public class SignupController : ControllerBase
{
    private readonly ISignupValidationService _validationService;
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ILogger<SignupController> _logger;
    
    public SignupController(
        ISignupValidationService validationService,
        ITenantProvisioningService provisioningService,
        ILogger<SignupController> logger)
    {
        _validationService = validationService;
        _provisioningService = provisioningService;
        _logger = logger;
    }
    
    [HttpPost("validate")]
    public async Task<ActionResult<SignupValidationResult>> ValidateSignup([FromBody] SignupRequest request)
    {
        try
        {
            var result = await _validationService.ValidateSignupAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signup for {Email}", request.Email);
            return StatusCode(500, new { message = "Validation error occurred" });
        }
    }
    
    [HttpPost("create")]
    public async Task<ActionResult<TenantCreationResult>> CreateTenant([FromBody] SignupRequest request)
    {
        try
        {
            // Always validate before creating (unless force flag is set)
            if (!request.ForceCreateSeparate)
            {
                var validation = await _validationService.ValidateSignupAsync(request);
                if (!validation.IsValid)
                {
                    return BadRequest(new { 
                        message = "Validation failed", 
                        validation = validation 
                    });
                }
            }
            
            // Proceed with tenant creation
            var result = await _provisioningService.CreateTenantAsync(request);
            
            _logger.LogInformation("Tenant created successfully: {TenantId} for {Email}", 
                result.TenantId, request.Email);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant for {Email}", request.Email);
            return StatusCode(500, new { message = "Tenant creation failed" });
        }
    }
    
    [HttpGet("suggestions/{companyCode}")]
    public async Task<ActionResult<List<string>>> GetCompanyCodeSuggestions(string companyCode)
    {
        try
        {
            var suggestions = await _validationService.GenerateCompanyCodeSuggestionsAsync(companyCode);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestions for {CompanyCode}", companyCode);
            return StatusCode(500, new { message = "Failed to generate suggestions" });
        }
    }
}
```

### 4. Frontend Signup Form with Real-time Validation

```razor
@page "/signup"
@inject ISignupValidationService ValidationService
@inject NavigationManager Navigation
@inject IJSRuntime JSRuntime

<div class="signup-container">
    <div class="signup-form">
        <h2>Create Your Fab.OS Workspace</h2>
        
        <EditForm Model="signupRequest" OnValidSubmit="HandleSignup">
            <DataAnnotationsValidator />
            
            <!-- Email Field -->
            <div class="form-group">
                <label>Work Email</label>
                <InputText @bind-Value="signupRequest.Email" 
                          @oninput="OnEmailChanged"
                          class="form-control" 
                          placeholder="john@company.com" />
                <ValidationMessage For="@(() => signupRequest.Email)" />
            </div>
            
            <!-- Company Name -->
            <div class="form-group">
                <label>Company Name</label>
                <InputText @bind-Value="signupRequest.CompanyName" 
                          @oninput="OnCompanyNameChanged"
                          class="form-control" 
                          placeholder="ACME Steel Works" />
                <ValidationMessage For="@(() => signupRequest.CompanyName)" />
            </div>
            
            <!-- Company Code (URL) -->
            <div class="form-group">
                <label>Choose Your URL</label>
                <div class="input-group">
                    <InputText @bind-Value="signupRequest.CompanyCode"
                              @oninput="OnCompanyCodeChanged" 
                              class="form-control" 
                              placeholder="acme-steel" />
                    <div class="input-group-append">
                        <span class="input-group-text">.fab-os.com</span>
                    </div>
                </div>
                
                @if (isValidating)
                {
                    <small class="text-info">
                        <i class="spinner-border spinner-border-sm"></i> Checking availability...
                    </small>
                }
                else if (validationResult != null)
                {
                    @if (validationResult.IsValid)
                    {
                        <small class="text-success">
                            ✅ Available! Your workspace will be: @validationResult.PreviewUrl
                        </small>
                    }
                    else
                    {
                        <div class="validation-error">
                            <small class="text-danger">❌ @validationResult.Message</small>
                            
                            @if (validationResult.ConflictType == ConflictType.EmailExists)
                            {
                                <div class="conflict-resolution">
                                    <p>This email is already registered.</p>
                                    @foreach (var action in validationResult.SuggestedActions)
                                    {
                                        <a href="@action.Url" 
                                           class="btn @(action.IsPrimary ? "btn-primary" : "btn-outline-secondary")">
                                            @action.Text
                                        </a>
                                    }
                                </div>
                            }
                            else if (validationResult.ConflictType == ConflictType.CompanyCodeExists)
                            {
                                <div class="suggestions">
                                    <p>Try these alternatives:</p>
                                    @foreach (var suggestion in validationResult.CodeSuggestions)
                                    {
                                        <button type="button" 
                                                class="btn btn-sm btn-outline-primary" 
                                                @onclick="() => UseSuggestion(suggestion)">
                                            @suggestion
                                        </button>
                                    }
                                </div>
                            }
                            else if (validationResult.ConflictType == ConflictType.DomainExists)
                            {
                                <div class="domain-conflict">
                                    <p>Your company already has @validationResult.DomainAnalysis!.TenantCount workspace(s):</p>
                                    @foreach (var tenant in validationResult.DomainAnalysis.ExistingTenants)
                                    {
                                        <div class="existing-tenant">
                                            <strong>@tenant.CompanyName</strong>
                                            <small>(@tenant.CompanyCode.fab-os.com)</small>
                                        </div>
                                    }
                                    
                                    @foreach (var action in validationResult.SuggestedActions)
                                    {
                                        <button type="button" 
                                                class="btn @(action.IsPrimary ? "btn-primary" : "btn-outline-secondary")"
                                                @onclick="() => HandleSuggestedAction(action)">
                                            @action.Text
                                        </button>
                                    }
                                </div>
                            }
                        </div>
                    }
                }
            </div>
            
            <!-- Module Selection -->
            <div class="form-group">
                <label>Select Modules to Start With</label>
                <div class="module-selection">
                    <div class="form-check">
                        <InputCheckbox @bind-Value="estimateSelected" class="form-check-input" />
                        <label class="form-check-label">
                            <strong>Estimate</strong> - Steel estimation and pricing
                        </label>
                    </div>
                    <div class="form-check">
                        <InputCheckbox @bind-Value="traceSelected" class="form-check-input" />
                        <label class="form-check-label">
                            <strong>Trace</strong> - Drawing takeoffs and measurements
                        </label>
                    </div>
                    <div class="form-check">
                        <InputCheckbox @bind-Value="fabmateSelected" class="form-check-input" />
                        <label class="form-check-label">
                            <strong>Fabmate</strong> - Work order management
                        </label>
                    </div>
                    <div class="form-check">
                        <InputCheckbox @bind-Value="qdocsSelected" class="form-check-input" />
                        <label class="form-check-label">
                            <strong>QDocs</strong> - Quality documentation
                        </label>
                    </div>
                </div>
            </div>
            
            <!-- Personal Details -->
            <div class="row">
                <div class="col-md-6">
                    <div class="form-group">
                        <label>First Name</label>
                        <InputText @bind-Value="signupRequest.FirstName" class="form-control" />
                        <ValidationMessage For="@(() => signupRequest.FirstName)" />
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="form-group">
                        <label>Last Name</label>
                        <InputText @bind-Value="signupRequest.LastName" class="form-control" />
                        <ValidationMessage For="@(() => signupRequest.LastName)" />
                    </div>
                </div>
            </div>
            
            <!-- Submit Button -->
            <div class="form-group">
                <button type="submit" 
                        class="btn btn-primary btn-lg btn-block" 
                        disabled="@(isCreating || (validationResult != null && !validationResult.IsValid))">
                    @if (isCreating)
                    {
                        <i class="spinner-border spinner-border-sm"></i>
                        <text>Creating your workspace...</text>
                    }
                    else
                    {
                        <text>Create Workspace</text>
                    }
                </button>
            </div>
        </EditForm>
    </div>
</div>

@code {
    private SignupRequest signupRequest = new();
    private SignupValidationResult? validationResult;
    private bool isValidating = false;
    private bool isCreating = false;
    private Timer? validationTimer;
    
    // Module selections
    private bool estimateSelected = true;
    private bool traceSelected = false;
    private bool fabmateSelected = false;
    private bool qdocsSelected = false;
    
    private async Task OnEmailChanged(ChangeEventArgs e)
    {
        signupRequest.Email = e.Value?.ToString() ?? "";
        await ValidateWithDelay();
    }
    
    private async Task OnCompanyNameChanged(ChangeEventArgs e)
    {
        signupRequest.CompanyName = e.Value?.ToString() ?? "";
        // Auto-generate company code from name
        signupRequest.CompanyCode = GenerateCompanyCode(signupRequest.CompanyName);
        await ValidateWithDelay();
    }
    
    private async Task OnCompanyCodeChanged(ChangeEventArgs e)
    {
        signupRequest.CompanyCode = e.Value?.ToString() ?? "";
        await ValidateWithDelay();
    }
    
    private async Task ValidateWithDelay()
    {
        validationTimer?.Dispose();
        validationTimer = new Timer(async _ => await ValidateSignup(), null, 500, Timeout.Infinite);
    }
    
    private async Task ValidateSignup()
    {
        if (string.IsNullOrEmpty(signupRequest.Email) || string.IsNullOrEmpty(signupRequest.CompanyCode))
            return;
            
        isValidating = true;
        await InvokeAsync(StateHasChanged);
        
        try
        {
            validationResult = await ValidationService.ValidateSignupAsync(signupRequest);
        }
        catch (Exception ex)
        {
            validationResult = new SignupValidationResult 
            { 
                IsValid = false, 
                Message = "Validation error occurred" 
            };
        }
        finally
        {
            isValidating = false;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private async Task HandleSignup()
    {
        // Collect selected modules
        signupRequest.SelectedModules.Clear();
        if (estimateSelected) signupRequest.SelectedModules.Add("Estimate");
        if (traceSelected) signupRequest.SelectedModules.Add("Trace");
        if (fabmateSelected) signupRequest.SelectedModules.Add("Fabmate");
        if (qdocsSelected) signupRequest.SelectedModules.Add("QDocs");
        
        isCreating = true;
        
        try
        {
            var result = await SignupService.CreateTenantAsync(signupRequest);
            
            if (result.Success)
            {
                // Redirect to success page or new tenant
                Navigation.NavigateTo($"https://{signupRequest.CompanyCode}.fab-os.com/welcome");
            }
            else
            {
                // Show error
                await JSRuntime.InvokeVoidAsync("alert", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", "Signup failed. Please try again.");
        }
        finally
        {
            isCreating = false;
        }
    }
    
    private void UseSuggestion(string suggestion)
    {
        signupRequest.CompanyCode = suggestion;
        _ = ValidateSignup();
    }
    
    private async Task HandleSuggestedAction(SuggestedAction action)
    {
        if (action.Type == "CreateSeparate")
        {
            signupRequest.ForceCreateSeparate = true;
            validationResult = new SignupValidationResult { IsValid = true };
        }
        else
        {
            Navigation.NavigateTo(action.Url);
        }
    }
    
    private string GenerateCompanyCode(string companyName)
    {
        return companyName
            .ToLower()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Trim('-');
    }
}
```

## User Experience Flow

### **Scenario 1: Email Already Exists**
```
User enters: john@acme.com
System response: "This email is already registered as admin for ACME Steel Works"
Actions: [Sign in to existing account] [Contact support]
```

### **Scenario 2: Domain Has Existing Workspace**
```
User enters: jane@acme.com
System response: "Your company domain (@acme.com) already has existing workspaces"
Shows: ACME Steel Works (acme-steel.fab-os.com)
Actions: [Join ACME Steel Works] [Create separate workspace anyway]
```

### **Scenario 3: Company Code Taken**
```
User enters company code: "acme-steel" 
System response: "Company code 'acme-steel' is already taken by ACME Steel Works"
Suggestions: [acme-steel-2025] [acme-steel-works] [acme-steel-ltd]
```

### **Scenario 4: All Clear**
```
User enters: sarah@newcompany.com, "new-steel-co"
System response: "✅ Available! Your workspace will be: https://new-steel-co.fab-os.com"
Action: [Create Workspace]
```

This implements the modern best practice approach, preventing duplicate signups while providing clear, user-friendly paths to resolution.