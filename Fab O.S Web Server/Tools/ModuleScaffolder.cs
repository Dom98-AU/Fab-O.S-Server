using System.Text;
using FabOS.WebServer.Services.Validation;

namespace FabOS.WebServer.Tools
{
    /// <summary>
    /// CLI tool to scaffold new modules following Fab.OS multi-tenant URL architecture.
    /// Usage: dotnet run --project Tools/ModuleScaffolder -- --module QDocs --entities Document,Folder
    /// </summary>
    public class ModuleScaffolder
    {
        private readonly string _baseDirectory;
        private readonly string _moduleName;
        private readonly string _moduleNameLower;
        private readonly List<string> _entities;

        public ModuleScaffolder(string baseDirectory, string moduleName, List<string> entities)
        {
            _baseDirectory = baseDirectory;
            _moduleName = moduleName;
            _moduleNameLower = moduleName.ToLower();
            _entities = entities;
        }

        public void Scaffold()
        {
            Console.WriteLine($"🏗️  Scaffolding module: {_moduleName}");
            Console.WriteLine($"📁 Base directory: {_baseDirectory}");
            Console.WriteLine($"📦 Entities: {string.Join(", ", _entities)}");
            Console.WriteLine();

            // Validate module name
            var testRoute = $"/{{tenantSlug}}/{_moduleNameLower}/test";
            if (!RouteValidator.HasValidModuleName(testRoute))
            {
                Console.WriteLine($"❌ ERROR: '{_moduleName}' is not a valid module name.");
                Console.WriteLine($"   Valid modules: trace, estimate, fabmate, qdocs, settings");
                return;
            }

            try
            {
                CreateDirectoryStructure();
                CreateModuleServices();
                CreateModulePages();
                CreateNavigationItems();
                CreateTests();
                CreateReadme();

                Console.WriteLine();
                Console.WriteLine("✅ Module scaffolding complete!");
                Console.WriteLine();
                Console.WriteLine("📋 Next steps:");
                Console.WriteLine("1. Register services in Program.cs");
                Console.WriteLine("2. Add module to NavigationService.GetAppModules()");
                Console.WriteLine("3. Implement business logic in services");
                Console.WriteLine("4. Run tests: dotnet test --filter Category=RouteValidation");
                Console.WriteLine("5. Review generated code and customize as needed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
            }
        }

        private void CreateDirectoryStructure()
        {
            Console.WriteLine("📁 Creating directory structure...");

            var directories = new[]
            {
                $"Components/Pages/{_moduleName}",
                $"Services/{_moduleName}",
                $"Models/{_moduleName}",
                $"Data/Entities/{_moduleName}",
                $"Tests/RouteValidation"
            };

            foreach (var dir in directories)
            {
                var fullPath = Path.Combine(_baseDirectory, dir);
                Directory.CreateDirectory(fullPath);
                Console.WriteLine($"  ✓ Created: {dir}");
            }
        }

        private void CreateModuleServices()
        {
            Console.WriteLine();
            Console.WriteLine("🔧 Creating services...");

            foreach (var entity in _entities)
            {
                CreateServiceInterface(entity);
                CreateServiceImplementation(entity);
            }
        }

        private void CreateServiceInterface(string entity)
        {
            var content = GenerateServiceInterface(entity);
            var filePath = Path.Combine(_baseDirectory, $"Services/{_moduleName}/I{entity}Service.cs");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Services/{_moduleName}/I{entity}Service.cs");
        }

        private void CreateServiceImplementation(string entity)
        {
            var content = GenerateServiceImplementation(entity);
            var filePath = Path.Combine(_baseDirectory, $"Services/{_moduleName}/{entity}Service.cs");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Services/{_moduleName}/{entity}Service.cs");
        }

        private void CreateModulePages()
        {
            Console.WriteLine();
            Console.WriteLine("📄 Creating pages...");

            // Dashboard
            CreateDashboardPage();

            // Entity pages
            foreach (var entity in _entities)
            {
                CreateListPage(entity);
                CreateDetailPage(entity);
                CreateCreatePage(entity);
            }

            // Settings page
            CreateSettingsPage();
        }

        private void CreateDashboardPage()
        {
            var content = GenerateDashboardPage();
            var filePath = Path.Combine(_baseDirectory, $"Components/Pages/{_moduleName}/Dashboard.razor");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Components/Pages/{_moduleName}/Dashboard.razor");
        }

        private void CreateListPage(string entity)
        {
            var content = GenerateListPage(entity);
            var pluralEntity = Pluralize(entity);
            var filePath = Path.Combine(_baseDirectory, $"Components/Pages/{_moduleName}/{pluralEntity}List.razor");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Components/Pages/{_moduleName}/{pluralEntity}List.razor");
        }

        private void CreateDetailPage(string entity)
        {
            var content = GenerateDetailPage(entity);
            var filePath = Path.Combine(_baseDirectory, $"Components/Pages/{_moduleName}/{entity}Detail.razor");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Components/Pages/{_moduleName}/{entity}Detail.razor");
        }

        private void CreateCreatePage(string entity)
        {
            var content = GenerateCreatePage(entity);
            var filePath = Path.Combine(_baseDirectory, $"Components/Pages/{_moduleName}/{entity}Create.razor");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Components/Pages/{_moduleName}/{entity}Create.razor");
        }

        private void CreateSettingsPage()
        {
            var content = GenerateSettingsPage();
            var filePath = Path.Combine(_baseDirectory, $"Components/Pages/{_moduleName}/Settings.razor");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Components/Pages/{_moduleName}/Settings.razor");
        }

        private void CreateNavigationItems()
        {
            Console.WriteLine();
            Console.WriteLine("🧭 Creating navigation items...");

            var content = GenerateNavigationServiceSnippet();
            var filePath = Path.Combine(_baseDirectory, $"Tools/Generated/{_moduleName}NavigationItems.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Tools/Generated/{_moduleName}NavigationItems.cs");
            Console.WriteLine($"  ℹ️  Copy this code to NavigationService.GetModuleItems()");
        }

        private void CreateTests()
        {
            Console.WriteLine();
            Console.WriteLine("🧪 Creating tests...");

            var content = GenerateRouteTests();
            var filePath = Path.Combine(_baseDirectory, $"Tests/RouteValidation/{_moduleName}RouteTests.cs");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Tests/RouteValidation/{_moduleName}RouteTests.cs");
        }

        private void CreateReadme()
        {
            Console.WriteLine();
            Console.WriteLine("📝 Creating README...");

            var content = GenerateModuleReadme();
            var filePath = Path.Combine(_baseDirectory, $"Components/Pages/{_moduleName}/README.md");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"  ✓ Created: Components/Pages/{_moduleName}/README.md");
        }

        #region Code Generation Methods

        private string GenerateServiceInterface(string entity)
        {
            return $@"using FabOS.WebServer.Data.Entities.{_moduleName};

namespace FabOS.WebServer.Services.{_moduleName}
{{
    /// <summary>
    /// Service interface for managing {entity} entities in the {_moduleName} module.
    /// </summary>
    public interface I{entity}Service
    {{
        Task<List<{entity}>> GetAllAsync();
        Task<{entity}?> GetByIdAsync(int id);
        Task<{entity}> CreateAsync({entity} entity);
        Task UpdateAsync({entity} entity);
        Task DeleteAsync(int id);
    }}
}}
";
        }

        private string GenerateServiceImplementation(string entity)
        {
            return $@"using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Data.Entities.{_moduleName};
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.{_moduleName}
{{
    /// <summary>
    /// Service implementation for managing {entity} entities.
    /// Automatically scoped to current tenant via TenantContext.
    /// </summary>
    public class {entity}Service : I{entity}Service
    {{
        private readonly ApplicationDbContext _context;
        private readonly ITenantContext _tenantContext;

        public {entity}Service(ApplicationDbContext context, ITenantContext tenantContext)
        {{
            _context = context;
            _tenantContext = tenantContext;
        }}

        public async Task<List<{entity}>> GetAllAsync()
        {{
            return await _context.{Pluralize(entity)}
                .Where(e => e.CompanyId == _tenantContext.CurrentCompanyId)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }}

        public async Task<{entity}?> GetByIdAsync(int id)
        {{
            return await _context.{Pluralize(entity)}
                .Where(e => e.Id == id && e.CompanyId == _tenantContext.CurrentCompanyId)
                .FirstOrDefaultAsync();
        }}

        public async Task<{entity}> CreateAsync({entity} entity)
        {{
            entity.CompanyId = _tenantContext.CurrentCompanyId;
            entity.CreatedDate = DateTime.UtcNow;

            _context.{Pluralize(entity)}.Add(entity);
            await _context.SaveChangesAsync();

            return entity;
        }}

        public async Task UpdateAsync({entity} entity)
        {{
            entity.ModifiedDate = DateTime.UtcNow;
            _context.{Pluralize(entity)}.Update(entity);
            await _context.SaveChangesAsync();
        }}

        public async Task DeleteAsync(int id)
        {{
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {{
                _context.{Pluralize(entity)}.Remove(entity);
                await _context.SaveChangesAsync();
            }}
        }}
    }}
}}
";
        }

        private string GenerateDashboardPage()
        {
            return $@"@page ""/{{tenantSlug}}/{_moduleNameLower}""
@using FabOS.WebServer.Services.Interfaces
@inject ITenantContext TenantContext
@inject BreadcrumbService BreadcrumbService

<div class=""container-fluid px-0"">
    <div class=""px-3 py-4"">
        <h1>{_moduleName} Dashboard</h1>
        <p class=""text-muted"">Welcome to the {_moduleName} module</p>

        <div class=""row mt-4"">
            <!-- Add dashboard widgets here -->
            <div class=""col-md-6"">
                <div class=""card"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Quick Stats</h5>
                        <p class=""card-text"">Module statistics will appear here</p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {{
    [Parameter]
    public string TenantSlug {{ get; set; }} = string.Empty;

    protected override async Task OnInitializedAsync()
    {{
        await TenantContext.ValidateTenantSlugAsync(TenantSlug);

        await BreadcrumbService.SetBreadcrumbsWithTenantAsync(
            TenantContext.CurrentTenant.Name,
            new List<BreadcrumbItem>
            {{
                new() {{ Label = ""{_moduleName}"", IsActive = true }}
            }}
        );
    }}
}}
";
        }

        private string GenerateListPage(string entity)
        {
            var pluralEntity = Pluralize(entity);
            var pluralLower = pluralEntity.ToLower();
            var entityLower = entity.ToLower();

            return $@"@page ""/{{tenantSlug}}/{_moduleNameLower}/{pluralLower}""
@using FabOS.WebServer.Services.{_moduleName}
@using FabOS.WebServer.Data.Entities.{_moduleName}
@using FabOS.WebServer.Services.Interfaces
@inject ITenantContext TenantContext
@inject I{entity}Service {entity}Service
@inject BreadcrumbService BreadcrumbService
@inject NavigationManager Navigation
@implements IToolbarActionProvider
@implements IFilterProvider<{entity}>
@implements ISaveableViewState

<div class=""container-fluid px-0"">
    <!-- StandardToolbar -->
    <StandardToolbar ActionProvider=""@this"" />

    <!-- FilterSystem -->
    <div class=""px-3 mt-3"">
        <FilterSystem TItem=""{entity}""
                     FilterProvider=""@this""
                     Items=""@items""
                     OnFilteredItemsChanged=""@HandleFilteredItemsChanged""
                     ShowSearch=""true""
                     SearchPlaceholder=""Search {pluralLower}...""
                     @bind-ActiveFilters=""activeFilters"" />
    </div>

    <!-- Main content with GenericViewSwitcher -->
    <div class=""card mx-3"">
        <div class=""card-body"">
            <GenericViewSwitcher
                @bind-CurrentView=""viewMode""
                ItemCount=""@filteredItems.Count()""
                TableTemplate=""@tableTemplate""
                CardTemplate=""@cardTemplate""
                ListTemplate=""@listTemplate"" />
        </div>
    </div>
</div>

@code {{
    [Parameter]
    public string TenantSlug {{ get; set; }} = string.Empty;

    private List<{entity}> items = new();
    private IEnumerable<{entity}> filteredItems = Enumerable.Empty<{entity}>();
    private ViewMode viewMode = ViewMode.Table;
    private Dictionary<string, object> activeFilters = new();
    private HashSet<{entity}> selectedRows = new();

    protected override async Task OnInitializedAsync()
    {{
        await TenantContext.ValidateTenantSlugAsync(TenantSlug);

        await BreadcrumbService.SetBreadcrumbsWithTenantAsync(
            TenantContext.CurrentTenant.Name,
            new List<BreadcrumbItem>
            {{
                new() {{ Label = ""{_moduleName}"", Url = $""/{{TenantSlug}}/{_moduleNameLower}"" }},
                new() {{ Label = ""{pluralEntity}"", IsActive = true }}
            }}
        );

        items = await {entity}Service.GetAllAsync();
        filteredItems = items;
    }}

    // IToolbarActionProvider implementation
    public ToolbarActionGroup GetActions() => new()
    {{
        PrimaryActions = new List<ToolbarAction>
        {{
            new() {{ Text = ""Add {entity}"", Icon = ""fas fa-plus"", Action = Create{entity}, Style = ToolbarActionStyle.Primary }}
        }},
        MenuActions = new List<ToolbarAction>
        {{
            new() {{ Text = ""Export"", Icon = ""fas fa-download"", Action = Export }},
            new() {{ Text = ""Delete"", Icon = ""fas fa-trash"", Action = DeleteSelected, RequiresSelection = true }}
        }}
    }};

    private async Task Create{entity}()
    {{
        Navigation.NavigateTo($""/{{TenantSlug}}/{_moduleNameLower}/{pluralLower}/new"");
    }}

    private async Task Export()
    {{
        // TODO: Implement export
    }}

    private async Task DeleteSelected()
    {{
        // TODO: Implement delete
    }}

    // IFilterProvider implementation
    public List<FilterDefinition> GetAvailableFilters() => new()
    {{
        // TODO: Add filter definitions
    }};

    public Func<{entity}, bool> BuildFilterPredicate(Dictionary<string, object> filters)
    {{
        return item => true; // TODO: Implement filtering logic
    }}

    private void HandleFilteredItemsChanged(IEnumerable<{entity}> items)
    {{
        filteredItems = items;
        StateHasChanged();
    }}

    // ISaveableViewState implementation
    public string GetPageIdentifier() => ""{_moduleNameLower}-{pluralLower}"";
    public string GetCurrentViewState() => """";
    public async Task ApplyViewState(string viewState) {{ }}
    public bool HasUnsavedChanges => false;
    public event EventHandler? ViewStateChanged;

    // View templates
    private RenderFragment tableTemplate => @<DataTable TItem=""{entity}""
                                               Items=""@filteredItems""
                                               ShowSelection=""true""
                                               SelectedItems=""@selectedRows"" />;

    private RenderFragment cardTemplate => @<GenericCardView TItem=""{entity}""
                                             Items=""@filteredItems"" />;

    private RenderFragment listTemplate => @<GenericListView TItem=""{entity}""
                                             Items=""@filteredItems"" />;
}}
";
        }

        private string GenerateDetailPage(string entity)
        {
            var pluralLower = Pluralize(entity).ToLower();

            return $@"@page ""/{{tenantSlug}}/{_moduleNameLower}/{pluralLower}/{{id:int}}""
@using FabOS.WebServer.Services.{_moduleName}
@using FabOS.WebServer.Data.Entities.{_moduleName}
@using FabOS.WebServer.Services.Interfaces
@inject ITenantContext TenantContext
@inject I{entity}Service {entity}Service
@inject BreadcrumbService BreadcrumbService
@inject NavigationManager Navigation
@implements IToolbarActionProvider

<div class=""container-fluid px-0"">
    <StandardToolbar ActionProvider=""@this"" />

    @if (entity != null)
    {{
        <div class=""card mx-3"">
            <div class=""card-header"">
                <h3>@entity.Name</h3>
            </div>
            <div class=""card-body"">
                <!-- TODO: Add entity details -->
                <dl class=""row"">
                    <dt class=""col-sm-3"">ID:</dt>
                    <dd class=""col-sm-9"">@entity.Id</dd>

                    <dt class=""col-sm-3"">Created:</dt>
                    <dd class=""col-sm-9"">@entity.CreatedDate.ToString(""d"")</dd>
                </dl>
            </div>
        </div>
    }}
</div>

@code {{
    [Parameter]
    public string TenantSlug {{ get; set; }} = string.Empty;

    [Parameter]
    public int Id {{ get; set; }}

    private {entity}? entity;

    protected override async Task OnInitializedAsync()
    {{
        await TenantContext.ValidateTenantSlugAsync(TenantSlug);

        entity = await {entity}Service.GetByIdAsync(Id);

        if (entity == null)
        {{
            Navigation.NavigateTo($""/{{TenantSlug}}/{_moduleNameLower}/{pluralLower}"");
            return;
        }}

        await BreadcrumbService.SetBreadcrumbsWithTenantAsync(
            TenantContext.CurrentTenant.Name,
            new List<BreadcrumbItem>
            {{
                new() {{ Label = ""{_moduleName}"", Url = $""/{{TenantSlug}}/{_moduleNameLower}"" }},
                new() {{ Label = ""{Pluralize(entity)}"", Url = $""/{{TenantSlug}}/{_moduleNameLower}/{pluralLower}"" }},
                new() {{ Label = entity.Name, IsActive = true }}
            }}
        );
    }}

    public ToolbarActionGroup GetActions() => new()
    {{
        PrimaryActions = new List<ToolbarAction>
        {{
            new() {{ Text = ""Edit"", Icon = ""fas fa-edit"", Action = Edit, Style = ToolbarActionStyle.Primary }}
        }},
        MenuActions = new List<ToolbarAction>
        {{
            new() {{ Text = ""Delete"", Icon = ""fas fa-trash"", Action = Delete }}
        }}
    }};

    private async Task Edit()
    {{
        // TODO: Navigate to edit page or show edit modal
    }}

    private async Task Delete()
    {{
        // TODO: Implement delete
    }}
}}
";
        }

        private string GenerateCreatePage(string entity)
        {
            var pluralLower = Pluralize(entity).ToLower();

            return $@"@page ""/{{tenantSlug}}/{_moduleNameLower}/{pluralLower}/new""
@using FabOS.WebServer.Services.{_moduleName}
@using FabOS.WebServer.Data.Entities.{_moduleName}
@using FabOS.WebServer.Services.Interfaces
@inject ITenantContext TenantContext
@inject I{entity}Service {entity}Service
@inject BreadcrumbService BreadcrumbService
@inject NavigationManager Navigation
@implements IToolbarActionProvider

<div class=""container-fluid px-0"">
    <StandardToolbar ActionProvider=""@this"" />

    <div class=""card mx-3"">
        <div class=""card-header"">
            <h3>Create New {entity}</h3>
        </div>
        <div class=""card-body"">
            <EditForm Model=""@entity"" OnValidSubmit=""@HandleSubmit"">
                <DataAnnotationsValidator />
                <ValidationSummary />

                <!-- TODO: Add form fields -->
                <div class=""mb-3"">
                    <label class=""form-label"">Name</label>
                    <InputText @bind-Value=""entity.Name"" class=""form-control"" />
                </div>
            </EditForm>
        </div>
    </div>
</div>

@code {{
    [Parameter]
    public string TenantSlug {{ get; set; }} = string.Empty;

    private {entity} entity = new();

    protected override async Task OnInitializedAsync()
    {{
        await TenantContext.ValidateTenantSlugAsync(TenantSlug);

        await BreadcrumbService.SetBreadcrumbsWithTenantAsync(
            TenantContext.CurrentTenant.Name,
            new List<BreadcrumbItem>
            {{
                new() {{ Label = ""{_moduleName}"", Url = $""/{{TenantSlug}}/{_moduleNameLower}"" }},
                new() {{ Label = ""{Pluralize(entity)}"", Url = $""/{{TenantSlug}}/{_moduleNameLower}/{pluralLower}"" }},
                new() {{ Label = ""New {entity}"", IsActive = true }}
            }}
        );
    }}

    public ToolbarActionGroup GetActions() => new()
    {{
        PrimaryActions = new List<ToolbarAction>
        {{
            new() {{ Text = ""Save"", Icon = ""fas fa-save"", Action = HandleSubmit, Style = ToolbarActionStyle.Primary }},
            new() {{ Text = ""Cancel"", Icon = ""fas fa-times"", Action = Cancel }}
        }}
    }};

    private async Task HandleSubmit()
    {{
        try
        {{
            await {entity}Service.CreateAsync(entity);
            Navigation.NavigateTo($""/{{TenantSlug}}/{_moduleNameLower}/{pluralLower}"");
        }}
        catch (Exception ex)
        {{
            // TODO: Show error message
            Console.WriteLine($""Error creating {entity}: {{ex.Message}}"");
        }}
    }}

    private void Cancel()
    {{
        Navigation.NavigateTo($""/{{TenantSlug}}/{_moduleNameLower}/{pluralLower}"");
    }}
}}
";
        }

        private string GenerateSettingsPage()
        {
            return $@"@page ""/{{tenantSlug}}/{_moduleNameLower}/settings""
@using FabOS.WebServer.Services.Interfaces
@inject ITenantContext TenantContext
@inject BreadcrumbService BreadcrumbService
@implements IToolbarActionProvider

<div class=""container-fluid px-0"">
    <StandardToolbar ActionProvider=""@this"" />

    <div class=""card mx-3"">
        <div class=""card-header"">
            <h3>{_moduleName} Settings</h3>
        </div>
        <div class=""card-body"">
            <EditForm Model=""@settings"" OnValidSubmit=""@SaveSettings"">
                <DataAnnotationsValidator />

                <!-- TODO: Add settings fields -->
                <div class=""mb-3"">
                    <label class=""form-label"">Setting 1</label>
                    <InputText @bind-Value=""settings.Setting1"" class=""form-control"" />
                </div>
            </EditForm>
        </div>
    </div>
</div>

@code {{
    [Parameter]
    public string TenantSlug {{ get; set; }} = string.Empty;

    private {_moduleName}Settings settings = new();

    protected override async Task OnInitializedAsync()
    {{
        await TenantContext.ValidateTenantSlugAsync(TenantSlug);

        await BreadcrumbService.SetBreadcrumbsWithTenantAsync(
            TenantContext.CurrentTenant.Name,
            new List<BreadcrumbItem>
            {{
                new() {{ Label = ""{_moduleName}"", Url = $""/{{TenantSlug}}/{_moduleNameLower}"" }},
                new() {{ Label = ""Settings"", IsActive = true }}
            }}
        );

        // TODO: Load settings
    }}

    public ToolbarActionGroup GetActions() => new()
    {{
        PrimaryActions = new List<ToolbarAction>
        {{
            new() {{ Text = ""Save"", Icon = ""fas fa-save"", Action = SaveSettings, Style = ToolbarActionStyle.Primary }}
        }}
    }};

    private async Task SaveSettings()
    {{
        // TODO: Save settings
    }}

    public class {_moduleName}Settings
    {{
        public string Setting1 {{ get; set; }} = string.Empty;
    }}
}}
";
        }

        private string GenerateNavigationServiceSnippet()
        {
            var items = new StringBuilder();
            foreach (var entity in _entities)
            {
                var pluralLower = Pluralize(entity).ToLower();
                items.AppendLine($"            new() {{ Label = \"{Pluralize(entity)}\", Icon = \"file-text\", Url = \"/{_moduleNameLower}/{pluralLower}\" }},");
            }

            return $@"// Add this to NavigationService.GetModuleItems() case statement

case ""{_moduleNameLower}"":
    return new List<NavigationItem>
    {{
{items}        new() {{ Label = ""Settings"", Icon = ""settings"", Url = ""/{_moduleNameLower}/settings"" }}
    }};
";
        }

        private string GenerateRouteTests()
        {
            var tests = new StringBuilder();

            // Dashboard test
            tests.AppendLine($"        [InlineData(\"/{{tenantSlug}}/{_moduleNameLower}\")]");

            // Entity tests
            foreach (var entity in _entities)
            {
                var pluralLower = Pluralize(entity).ToLower();
                tests.AppendLine($"        [InlineData(\"/{{tenantSlug}}/{_moduleNameLower}/{pluralLower}\")]");
                tests.AppendLine($"        [InlineData(\"/{{tenantSlug}}/{_moduleNameLower}/{pluralLower}/new\")]");
                tests.AppendLine($"        [InlineData(\"/{{tenantSlug}}/{_moduleNameLower}/{pluralLower}/{{id:int}}\")]");
            }

            // Settings test
            tests.AppendLine($"        [InlineData(\"/{{tenantSlug}}/{_moduleNameLower}/settings\")]");

            return $@"using Xunit;
using FabOS.WebServer.Services.Validation;

namespace FabOS.WebServer.Tests.RouteValidation
{{
    /// <summary>
    /// Route validation tests for {_moduleName} module.
    /// Generated by ModuleScaffolder.
    /// </summary>
    public class {_moduleName}RouteTests
    {{
        [Theory]
{tests}        public void {_moduleName}Module_AllRoutes_AreValid(string route)
        {{
            var report = RouteValidator.Validate(route);
            Assert.True(report.IsValid, $""{_moduleName} module route '{{route}}' failed validation:\n{{report}}"");
        }}

        [Fact]
        public void {_moduleName}Routes_StartWithTenantSlug()
        {{
            var route = ""/{{tenantSlug}}/{_moduleNameLower}"";
            Assert.True(RouteValidator.StartsWithTenantSlug(route));
        }}

        [Fact]
        public void {_moduleName}Routes_HaveValidModuleName()
        {{
            var route = ""/{{tenantSlug}}/{_moduleNameLower}"";
            Assert.True(RouteValidator.HasValidModuleName(route, ""{_moduleNameLower}""));
        }}
    }}
}}
";
        }

        private string GenerateModuleReadme()
        {
            var entitiesList = string.Join("\n", _entities.Select(e => $"- {e}"));

            return $@"# {_moduleName} Module

Auto-generated module scaffolding following Fab.OS multi-tenant URL architecture.

## Module Information

**Module Name:** {_moduleName}
**Route Prefix:** `/{{tenantSlug}}/{_moduleNameLower}`
**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

## Entities

{entitiesList}

## URL Structure

```
/{{tenantSlug}}/{_moduleNameLower}                     → Dashboard
{string.Join("\n", _entities.Select(e => $"/{{tenantSlug}}/{_moduleNameLower}/{Pluralize(e).ToLower()}           → {e} List"))}
/{{tenantSlug}}/{_moduleNameLower}/settings           → Module Settings
```

## Files Generated

### Services
{string.Join("\n", _entities.Select(e => $"- `Services/{_moduleName}/I{e}Service.cs`\n- `Services/{_moduleName}/{e}Service.cs`"))}

### Pages
- `Components/Pages/{_moduleName}/Dashboard.razor`
{string.Join("\n", _entities.Select(e => $"- `Components/Pages/{_moduleName}/{Pluralize(e)}List.razor`\n- `Components/Pages/{_moduleName}/{e}Detail.razor`\n- `Components/Pages/{_moduleName}/{e}Create.razor`"))}
- `Components/Pages/{_moduleName}/Settings.razor`

### Tests
- `Tests/RouteValidation/{_moduleName}RouteTests.cs`

## Next Steps

1. **Register Services** (Program.cs):
   ```csharp
{string.Join("\n   ", _entities.Select(e => $"builder.Services.AddScoped<I{e}Service, {e}Service>();"))}
   ```

2. **Add Navigation Items** (NavigationService.cs):
   See `Tools/Generated/{_moduleName}NavigationItems.cs` for code to copy.

3. **Create Database Entities**:
   Define entity models in `Data/Entities/{_moduleName}/`

4. **Implement Business Logic**:
   Fill in TODO comments in service implementations

5. **Customize Pages**:
   - Add form fields
   - Implement filters
   - Add validation
   - Customize UI

6. **Run Tests**:
   ```bash
   dotnet test --filter ""FullyQualifiedName~{_moduleName}RouteTests""
   ```

## Architecture Compliance

All generated code follows:
- ✅ Multi-tenant URL architecture
- ✅ URL routing standards
- ✅ Page type patterns
- ✅ Service layer patterns
- ✅ Breadcrumb requirements
- ✅ Testing requirements

See: `/Fab O.S System Architecture/URL-ROUTING-STANDARDS.md`
";
        }

        #endregion

        #region Helper Methods

        private string Pluralize(string word)
        {
            // Simple pluralization (can be enhanced)
            if (word.EndsWith("y"))
                return word.Substring(0, word.Length - 1) + "ies";
            if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("ch") || word.EndsWith("sh"))
                return word + "es";
            return word + "s";
        }

        #endregion

        // CLI Entry Point
        public static void Main(string[] args)
        {
            Console.WriteLine("🏗️  Fab.OS Module Scaffolder");
            Console.WriteLine("=============================");
            Console.WriteLine();

            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return;
            }

            try
            {
                var moduleName = GetArgValue(args, "--module") ?? GetArgValue(args, "-m");
                var entitiesArg = GetArgValue(args, "--entities") ?? GetArgValue(args, "-e");
                var baseDir = GetArgValue(args, "--directory") ?? GetArgValue(args, "-d") ?? Directory.GetCurrentDirectory();

                if (string.IsNullOrWhiteSpace(moduleName))
                {
                    Console.WriteLine("❌ ERROR: --module is required");
                    ShowHelp();
                    return;
                }

                if (string.IsNullOrWhiteSpace(entitiesArg))
                {
                    Console.WriteLine("❌ ERROR: --entities is required");
                    ShowHelp();
                    return;
                }

                var entities = entitiesArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .ToList();

                var scaffolder = new ModuleScaffolder(baseDir, moduleName, entities);
                scaffolder.Scaffold();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack trace:");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project Tools/ModuleScaffolder -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -m, --module <name>        Module name (e.g., QDocs, Estimate)");
            Console.WriteLine("  -e, --entities <list>      Comma-separated entity names (e.g., Document,Folder)");
            Console.WriteLine("  -d, --directory <path>     Base directory (default: current directory)");
            Console.WriteLine("  -h, --help                 Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Create QDocs module with Document and Folder entities");
            Console.WriteLine("  dotnet run -- --module QDocs --entities Document,Folder");
            Console.WriteLine();
            Console.WriteLine("  # Create Estimate module");
            Console.WriteLine("  dotnet run -- -m Estimate -e Project,Quote,LineItem");
        }

        private static string? GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
