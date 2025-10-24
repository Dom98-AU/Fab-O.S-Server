using System.Text;
using System.Text.RegularExpressions;

namespace FabOS.WebServer.Tools
{
    /// <summary>
    /// Tool to migrate existing single-tenant pages to multi-tenant URL architecture.
    /// Usage: dotnet run --project Tools/MigrateToMultiTenant -- --scan
    /// </summary>
    public class MigrateToMultiTenant
    {
        private readonly string _baseDirectory;
        private readonly List<MigrationItem> _migrationItems = new();

        public MigrateToMultiTenant(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public void ScanForMigration()
        {
            Console.WriteLine("🔍 Scanning for pages requiring migration...");
            Console.WriteLine();

            var pagesDir = Path.Combine(_baseDirectory, "Components/Pages");
            if (!Directory.Exists(pagesDir))
            {
                Console.WriteLine($"❌ ERROR: Pages directory not found: {pagesDir}");
                return;
            }

            var razorFiles = Directory.GetFiles(pagesDir, "*.razor", SearchOption.AllDirectories);
            Console.WriteLine($"📂 Found {razorFiles.Length} Razor files");
            Console.WriteLine();

            foreach (var file in razorFiles)
            {
                AnalyzeFile(file);
            }

            DisplayResults();
        }

        private void AnalyzeFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');

            // Find @page directives
            var pageDirectives = new List<(int lineNumber, string directive, string route)>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("@page"))
                {
                    var match = Regex.Match(line, @"@page\s+""([^""]*)""");
                    if (match.Success)
                    {
                        var route = match.Groups[1].Value;
                        pageDirectives.Add((i + 1, line, route));
                    }
                }
            }

            // Analyze each @page directive
            foreach (var (lineNumber, directive, route) in pageDirectives)
            {
                if (!route.StartsWith("/{tenantSlug}"))
                {
                    var migrationItem = new MigrationItem
                    {
                        FilePath = filePath,
                        RelativePath = Path.GetRelativePath(_baseDirectory, filePath),
                        LineNumber = lineNumber,
                        CurrentRoute = route,
                        SuggestedRoute = SuggestMultiTenantRoute(route),
                        IssuesFound = AnalyzeRoute(route)
                    };

                    _migrationItems.Add(migrationItem);
                }
            }
        }

        private string SuggestMultiTenantRoute(string route)
        {
            // Add /{tenantSlug} prefix
            if (!route.StartsWith("/"))
                route = "/" + route;

            var suggested = "/{tenantSlug}" + route;

            // Fix other issues
            suggested = suggested.ToLower();
            suggested = Regex.Replace(suggested, @"_", "-");
            suggested = Regex.Replace(suggested, @"\{([^}]*id[^}]*)\}(?!:int)", "{$1:int}", RegexOptions.IgnoreCase);

            return suggested;
        }

        private List<string> AnalyzeRoute(string route)
        {
            var issues = new List<string>();

            if (!route.StartsWith("/{tenantSlug}"))
                issues.Add("Missing /{tenantSlug} prefix");

            if (Regex.IsMatch(route, @"[A-Z]"))
                issues.Add("Contains uppercase letters");

            if (route.Contains("_"))
                issues.Add("Contains underscores (should use hyphens)");

            if (Regex.IsMatch(route, @"\{[^}]*id[^}]*\}") && !route.Contains(":int"))
                issues.Add("ID parameter missing :int constraint");

            return issues;
        }

        private void DisplayResults()
        {
            Console.WriteLine("📊 Migration Analysis Results");
            Console.WriteLine("=============================");
            Console.WriteLine();

            if (_migrationItems.Count == 0)
            {
                Console.WriteLine("✅ All pages already follow multi-tenant URL architecture!");
                return;
            }

            Console.WriteLine($"Found {_migrationItems.Count} pages requiring migration:");
            Console.WriteLine();

            foreach (var item in _migrationItems)
            {
                Console.WriteLine($"📄 {item.RelativePath}:{item.LineNumber}");
                Console.WriteLine($"   Current:   {item.CurrentRoute}");
                Console.WriteLine($"   Suggested: {item.SuggestedRoute}");
                Console.WriteLine($"   Issues:");
                foreach (var issue in item.IssuesFound)
                {
                    Console.WriteLine($"      • {issue}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("=============================");
            Console.WriteLine($"Total pages to migrate: {_migrationItems.Count}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Review suggested routes above");
            Console.WriteLine("  2. Run with --migrate flag to apply changes");
            Console.WriteLine("  3. Update NavigationManager.NavigateTo() calls");
            Console.WriteLine("  4. Update NavLink href attributes");
            Console.WriteLine("  5. Test all navigation flows");
        }

        public void PerformMigration(bool dryRun = true)
        {
            Console.WriteLine($"🚀 {(dryRun ? "DRY RUN:" : "")} Migrating pages to multi-tenant architecture...");
            Console.WriteLine();

            if (_migrationItems.Count == 0)
            {
                Console.WriteLine("ℹ️  Run --scan first to identify pages requiring migration");
                return;
            }

            var migrationPlan = new StringBuilder();
            migrationPlan.AppendLine("# Migration Plan");
            migrationPlan.AppendLine();
            migrationPlan.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            migrationPlan.AppendLine($"Pages to migrate: {_migrationItems.Count}");
            migrationPlan.AppendLine();

            foreach (var item in _migrationItems)
            {
                Console.WriteLine($"Processing: {item.RelativePath}");

                try
                {
                    var content = File.ReadAllText(item.FilePath);
                    var updatedContent = MigrateFileContent(content, item);

                    if (!dryRun)
                    {
                        // Backup original
                        var backupPath = item.FilePath + ".backup";
                        File.Copy(item.FilePath, backupPath, true);
                        Console.WriteLine($"  ✓ Backed up to: {backupPath}");

                        // Write updated content
                        File.WriteAllText(item.FilePath, updatedContent);
                        Console.WriteLine($"  ✓ Updated: {item.FilePath}");
                    }
                    else
                    {
                        Console.WriteLine($"  ℹ️  Would update route: {item.CurrentRoute} → {item.SuggestedRoute}");
                    }

                    // Add to migration plan
                    migrationPlan.AppendLine($"## {item.RelativePath}");
                    migrationPlan.AppendLine($"- **Old Route:** `{item.CurrentRoute}`");
                    migrationPlan.AppendLine($"- **New Route:** `{item.SuggestedRoute}`");
                    migrationPlan.AppendLine($"- **Issues Fixed:** {string.Join(", ", item.IssuesFound)}");
                    migrationPlan.AppendLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ ERROR: {ex.Message}");
                }
            }

            // Save migration plan
            var planPath = Path.Combine(_baseDirectory, "Tools/Generated/MigrationPlan.md");
            Directory.CreateDirectory(Path.GetDirectoryName(planPath)!);
            File.WriteAllText(planPath, migrationPlan.ToString());
            Console.WriteLine();
            Console.WriteLine($"📋 Migration plan saved to: {planPath}");

            if (dryRun)
            {
                Console.WriteLine();
                Console.WriteLine("This was a DRY RUN. No files were modified.");
                Console.WriteLine("Run with --migrate --execute to apply changes.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("✅ Migration complete!");
                Console.WriteLine("⚠️  Manual steps still required:");
                Console.WriteLine("  1. Update NavigationManager.NavigateTo() calls");
                Console.WriteLine("  2. Update NavLink href attributes");
                Console.WriteLine("  3. Add tenant validation in OnInitializedAsync()");
                Console.WriteLine("  4. Update breadcrumbs to include tenant context");
                Console.WriteLine("  5. Test all navigation flows");
            }
        }

        private string MigrateFileContent(string content, MigrationItem item)
        {
            // Replace @page directive
            content = Regex.Replace(
                content,
                @"@page\s+""" + Regex.Escape(item.CurrentRoute) + @"""",
                $@"@page ""{item.SuggestedRoute}"""
            );

            // Add [Parameter] for TenantSlug if not present
            if (!content.Contains("public string TenantSlug"))
            {
                var codeBlockMatch = Regex.Match(content, @"@code\s*\{");
                if (codeBlockMatch.Success)
                {
                    var insertPosition = codeBlockMatch.Index + codeBlockMatch.Length;
                    content = content.Insert(insertPosition, @"
    [Parameter]
    public string TenantSlug { get; set; } = string.Empty;
");
                }
            }

            // Add ITenantContext injection if not present
            if (!content.Contains("ITenantContext"))
            {
                var firstInject = content.IndexOf("@inject");
                if (firstInject >= 0)
                {
                    content = content.Insert(firstInject, "@inject ITenantContext TenantContext\n");
                }
                else
                {
                    // Add after @page directive
                    var pageDirective = Regex.Match(content, @"@page[^\n]*\n");
                    if (pageDirective.Success)
                    {
                        var insertPosition = pageDirective.Index + pageDirective.Length;
                        content = content.Insert(insertPosition, "@inject ITenantContext TenantContext\n");
                    }
                }
            }

            return content;
        }

        // CLI Entry Point
        public static void Main(string[] args)
        {
            Console.WriteLine("🔄 Fab.OS Multi-Tenant Migration Tool");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return;
            }

            try
            {
                var baseDir = GetArgValue(args, "--directory") ?? GetArgValue(args, "-d") ?? Directory.GetCurrentDirectory();
                var tool = new MigrateToMultiTenant(baseDir);

                if (args.Contains("--scan") || args.Contains("-s"))
                {
                    tool.ScanForMigration();
                }
                else if (args.Contains("--migrate") || args.Contains("-m"))
                {
                    tool.ScanForMigration(); // Scan first
                    var execute = args.Contains("--execute") || args.Contains("-x");
                    tool.PerformMigration(dryRun: !execute);
                }
                else
                {
                    Console.WriteLine("❌ ERROR: No action specified");
                    ShowHelp();
                }
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
            Console.WriteLine("  dotnet run --project Tools/MigrateToMultiTenant -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -s, --scan                Scan for pages requiring migration");
            Console.WriteLine("  -m, --migrate             Perform migration (dry run by default)");
            Console.WriteLine("  -x, --execute             Execute migration (apply changes)");
            Console.WriteLine("  -d, --directory <path>    Base directory (default: current directory)");
            Console.WriteLine("  -h, --help                Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Scan for pages requiring migration");
            Console.WriteLine("  dotnet run -- --scan");
            Console.WriteLine();
            Console.WriteLine("  # Dry run migration (no changes applied)");
            Console.WriteLine("  dotnet run -- --migrate");
            Console.WriteLine();
            Console.WriteLine("  # Execute migration (apply changes)");
            Console.WriteLine("  dotnet run -- --migrate --execute");
            Console.WriteLine();
            Console.WriteLine("⚠️  IMPORTANT: Always commit your work before running with --execute");
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

    public class MigrationItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string CurrentRoute { get; set; } = string.Empty;
        public string SuggestedRoute { get; set; } = string.Empty;
        public List<string> IssuesFound { get; set; } = new();
    }
}
