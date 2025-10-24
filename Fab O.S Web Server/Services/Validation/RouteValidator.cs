using System.Text.RegularExpressions;

namespace FabOS.WebServer.Services.Validation
{
    /// <summary>
    /// Validates routes against Fab.OS multi-tenant URL architecture standards.
    /// See: /Fab O.S System Architecture/URL-ROUTING-STANDARDS.md
    /// </summary>
    public static class RouteValidator
    {
        private static readonly string[] ValidModules = { "trace", "estimate", "fabmate", "qdocs", "settings" };
        private static readonly Regex TenantSlugPattern = new(@"^\{tenantSlug\}/?", RegexOptions.IgnoreCase);
        private static readonly Regex RouteConstraintPattern = new(@"\{[^}]+:int\}", RegexOptions.IgnoreCase);
        private static readonly Regex UppercasePattern = new(@"[A-Z]");
        private static readonly Regex UnderscorePattern = new(@"_");
        private static readonly Regex CamelCasePattern = new(@"[a-z][A-Z]");

        /// <summary>
        /// Validates that a route follows all multi-tenant URL standards.
        /// </summary>
        public static bool IsValidMultiTenantRoute(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            var violations = new List<string>();

            if (!StartsWithTenantSlug(route))
                violations.Add("Route must start with /{tenantSlug}");

            if (!HasValidModuleName(route))
                violations.Add("Route must have a valid module name (trace, estimate, fabmate, qdocs, settings)");

            if (!UsesPluralResourceNames(route))
                violations.Add("Resource names must be plural");

            if (!IsLowercase(route))
                violations.Add("Route must be lowercase (no uppercase letters)");

            if (HasUnderscores(route))
                violations.Add("Route must use hyphens, not underscores");

            if (HasCamelCase(route))
                violations.Add("Route must use kebab-case, not camelCase");

            if (!HasProperDepth(route))
                violations.Add("Route depth must not exceed 5 levels");

            if (HasIdParameter(route) && !HasRequiredConstraints(route))
                violations.Add("ID parameters must have route constraints (:int)");

            return violations.Count == 0;
        }

        /// <summary>
        /// Gets all validation violations for a route.
        /// </summary>
        public static List<string> GetValidationViolations(string route)
        {
            var violations = new List<string>();

            if (string.IsNullOrWhiteSpace(route))
            {
                violations.Add("Route cannot be empty");
                return violations;
            }

            if (!StartsWithTenantSlug(route))
                violations.Add("❌ Route must start with /{tenantSlug}");

            if (!HasValidModuleName(route))
                violations.Add("❌ Invalid module name. Must be one of: trace, estimate, fabmate, qdocs, settings");

            if (!IsLowercase(route))
                violations.Add("❌ Route contains uppercase letters. Must be lowercase.");

            if (HasUnderscores(route))
                violations.Add("❌ Route contains underscores. Use hyphens instead (kebab-case).");

            if (HasCamelCase(route))
                violations.Add("❌ Route uses camelCase. Use kebab-case instead.");

            if (!HasProperDepth(route))
                violations.Add("❌ Route depth exceeds 5 levels");

            if (HasIdParameter(route) && !HasRequiredConstraints(route))
                violations.Add("❌ ID parameters missing route constraints. Use {id:int} instead of {id}");

            return violations;
        }

        /// <summary>
        /// Checks if route starts with /{tenantSlug}
        /// </summary>
        public static bool StartsWithTenantSlug(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            route = route.TrimStart('/');
            return TenantSlugPattern.IsMatch(route);
        }

        /// <summary>
        /// Checks if route has a valid module name.
        /// </summary>
        public static bool HasValidModuleName(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            var parts = route.TrimStart('/').Split('/');
            if (parts.Length < 2)
                return false;

            // Second part should be the module name
            var moduleName = parts[1].ToLower();
            return ValidModules.Contains(moduleName);
        }

        /// <summary>
        /// Checks if a specific module name is valid.
        /// </summary>
        public static bool HasValidModuleName(string route, string expectedModule)
        {
            if (string.IsNullOrWhiteSpace(route) || string.IsNullOrWhiteSpace(expectedModule))
                return false;

            var parts = route.TrimStart('/').Split('/');
            if (parts.Length < 2)
                return false;

            return parts[1].Equals(expectedModule, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if resource names are plural (basic heuristic).
        /// </summary>
        public static bool UsesPluralResourceNames(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            var parts = route.TrimStart('/').Split('/');

            // Check resource segments (typically index 2 and beyond, skipping parameters)
            for (int i = 2; i < parts.Length; i++)
            {
                var part = parts[i];

                // Skip parameters, route constraints, and known actions
                if (part.StartsWith("{") || part == "new" || part == "edit" || part == "settings")
                    continue;

                // Basic plural check - ends with 's' (not perfect but catches most cases)
                if (!part.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                {
                    // Allow some exceptions like "viewer", "dashboard", etc.
                    var exceptions = new[] { "viewer", "dashboard", "search" };
                    if (!exceptions.Contains(part.ToLower()))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if route is entirely lowercase.
        /// </summary>
        public static bool IsLowercase(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            // Exclude parameter names and route constraints from check
            var routeWithoutParams = Regex.Replace(route, @"\{[^}]+\}", "");
            return !UppercasePattern.IsMatch(routeWithoutParams);
        }

        /// <summary>
        /// Checks if route contains underscores.
        /// </summary>
        public static bool HasUnderscores(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            // Exclude parameter names from check
            var routeWithoutParams = Regex.Replace(route, @"\{[^}]+\}", "");
            return UnderscorePattern.IsMatch(routeWithoutParams);
        }

        /// <summary>
        /// Checks if route uses camelCase.
        /// </summary>
        public static bool HasCamelCase(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            // Exclude parameter names from check
            var routeWithoutParams = Regex.Replace(route, @"\{[^}]+\}", "");
            return CamelCasePattern.IsMatch(routeWithoutParams);
        }

        /// <summary>
        /// Checks if route has proper depth (max 5 levels).
        /// </summary>
        public static bool HasProperDepth(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            var parts = route.TrimStart('/').Split('/').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            return parts.Length <= 5; // Max 5 levels: {tenantSlug}/{module}/{resource}/{id}/{sub-resource}
        }

        /// <summary>
        /// Checks if route has ID parameters.
        /// </summary>
        public static bool HasIdParameter(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            return Regex.IsMatch(route, @"\{[^}]*id[^}]*\}", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Checks if ID parameters have required constraints (:int).
        /// </summary>
        public static bool HasRequiredConstraints(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            // Find all ID parameters
            var idMatches = Regex.Matches(route, @"\{[^}]*id[^}]*\}", RegexOptions.IgnoreCase);

            if (idMatches.Count == 0)
                return true; // No ID parameters, no constraints needed

            // Check that all ID parameters have :int constraint
            foreach (Match match in idMatches)
            {
                if (!match.Value.Contains(":int", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Suggests a corrected version of an invalid route.
        /// </summary>
        public static string SuggestCorrection(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return "/{tenantSlug}/{module}/{resource}";

            var corrected = route;

            // Add tenant slug if missing
            if (!StartsWithTenantSlug(corrected))
            {
                corrected = "/{tenantSlug}" + (corrected.StartsWith("/") ? corrected : "/" + corrected);
            }

            // Convert to lowercase
            var parts = corrected.Split(new[] { '{', '}' });
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 0) // Only lowercase the non-parameter parts
                {
                    parts[i] = parts[i].ToLower();
                }
            }
            corrected = string.Join("", parts.Select((p, i) => i % 2 == 0 ? p : "{" + p + "}"));

            // Replace underscores with hyphens
            corrected = Regex.Replace(corrected, @"([^{]*)_([^}]*)", "$1-$2");

            // Add :int constraint to ID parameters
            corrected = Regex.Replace(corrected, @"\{([^}]*id[^}]*)\}(?!:int)", "{$1:int}", RegexOptions.IgnoreCase);

            return corrected;
        }

        /// <summary>
        /// Validates route and returns detailed report.
        /// </summary>
        public static ValidationReport Validate(string route)
        {
            return new ValidationReport
            {
                Route = route,
                IsValid = IsValidMultiTenantRoute(route),
                Violations = GetValidationViolations(route),
                SuggestedCorrection = SuggestCorrection(route)
            };
        }
    }

    /// <summary>
    /// Detailed validation report for a route.
    /// </summary>
    public class ValidationReport
    {
        public string Route { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> Violations { get; set; } = new();
        public string SuggestedCorrection { get; set; } = string.Empty;

        public override string ToString()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Route: {Route}");
            report.AppendLine($"Status: {(IsValid ? "✅ Valid" : "❌ Invalid")}");

            if (!IsValid)
            {
                report.AppendLine("\nViolations:");
                foreach (var violation in Violations)
                {
                    report.AppendLine($"  {violation}");
                }
                report.AppendLine($"\nSuggested Correction:");
                report.AppendLine($"  {SuggestedCorrection}");
            }

            return report.ToString();
        }
    }
}
