# Razor Source Generator Error Troubleshooting Guide

## Overview

Razor Source Generator errors are typically **project configuration issues** rather than SDK bugs. The source generator is stable in .NET SDK 8.0.413, but certain project setups can cause dependency resolution failures during the build process.

## How the Razor Source Generator Works

The Razor Source Generator follows this process during build:

1. **Scans .razor files** throughout the project
2. **Generates C# classes** from Razor markup and components
3. **Resolves component references** and dependencies
4. **Creates the final compiled output**

When step 3 fails (dependency resolution), you encounter the common CS0246 errors.

## Common Error Types and Causes

### 1. Namespace Resolution Problems

**Error Messages:**
```
Error CS0246: The type or namespace name 'ComponentName' could not be found
Error CS0400: The type or namespace name 'ComponentName' could not be found in the global namespace
```

**Root Causes:**
- Mixed project types (MVC + Blazor) with conflicting namespaces
- Missing `@using` directives in `_Imports.razor`
- Components in different assemblies not properly referenced
- Blazor components not following proper namespace conventions
- Assembly references missing from project file

**Solutions:**
- Add missing `@using` statements to `_Imports.razor`
- Ensure proper namespace declarations in component files
- Verify all required project references are included
- Use fully qualified names for components from external assemblies

### 2. Generic Type Constraint Issues

**Error Messages:**
```
Error CS0246: The type or namespace name 'IDataItem' could not be found
Error CS0118: 'InterfaceName' is a namespace but is used like a type
```

**Root Causes:**
- Generic Blazor components with interface constraints
- Source generator unable to resolve interface types in certain compilation contexts
- Particularly affects MAUI Blazor Hybrid projects
- Complex inheritance hierarchies confusing dependency resolution

**Solutions:**
- Move interface definitions to separate shared projects
- Use concrete types instead of interfaces where possible
- Ensure interfaces are in the same namespace as implementing components
- Add explicit assembly references for interface definitions

### 3. Build Order Dependencies

**Error Messages:**
```
Error CS1002: ; expected
Error CS0103: The name 'ComponentName' does not exist in the current context
```

**Root Causes:**
- Source generator runs before all dependencies are built
- Circular references between projects
- Missing assembly references in multi-project solutions
- Incorrect project build order in solution file

**Solutions:**
- Review and fix circular project dependencies
- Ensure proper project reference hierarchy
- Use `<ProjectReference>` instead of `<Reference>` for local projects
- Clean and rebuild entire solution to reset build order

### 4. File Path and Naming Issues

**Error Messages:**
```
Error CS1504: Source file 'path' could not be opened
Warning CS8785: Generator 'RazorSourceGenerator' failed to generate source
```

**Root Causes:**
- Component files with invalid characters in names
- Very long file paths exceeding Windows 260-character limit
- Razor files located in unexpected directories outside `Pages/` or `Components/`
- Special characters or Unicode in file names
- Inconsistent file naming conventions

**Solutions:**
- Keep file paths under 260 characters
- Use only alphanumeric characters and hyphens in file names
- Place Razor components in standard `Pages/`, `Components/`, or `Shared/` folders
- Avoid special characters, spaces, or Unicode in file names
- Follow PascalCase naming convention for component files

### 5. MSBuild Configuration Conflicts

**Error Messages:**
```
Warning CS8785: Generator 'RazorSourceGenerator' failed to generate source
Error MSB4018: The "RazorGenerate" task failed unexpectedly
```

**Root Causes:**
- Custom MSBuild targets interfering with Razor compilation
- Multiple target frameworks with conflicting configurations
- Incorrect `<EnableDefaultRazorGenerateItems>` settings
- Conflicting source generator configurations
- Third-party MSBuild extensions causing interference

**Solutions:**
- Review custom MSBuild targets for Razor conflicts
- Ensure consistent configuration across target frameworks
- Verify `<EnableDefaultRazorGenerateItems>` is set correctly
- Temporarily disable third-party MSBuild extensions to isolate issues
- Use standard Blazor project templates as configuration reference

### 6. NuGet Package Conflicts

**Error Messages:**
```
Error CS0433: The type 'ComponentName' exists in both assemblies
Error CS0246: The type or namespace name could not be found
```

**Root Causes:**
- Multiple NuGet packages providing similar Blazor components
- Version conflicts between Blazor component libraries
- Transitive dependencies with conflicting component names
- Outdated packages not compatible with current .NET version

**Solutions:**
- Review PackageReference versions for conflicts
- Use `dotnet list package --outdated` to identify version issues
- Consolidate component libraries to avoid naming conflicts
- Update all packages to compatible versions
- Use package aliases for conflicting component names

### 7. Authentication and Authorization Issues

**Error Messages:**
```
Error CS0246: The type or namespace name 'AuthenticationStateProvider' could not be found
Error CS0103: The name 'AuthorizeView' does not exist in the current context
```

**Root Causes:**
- Missing authentication service registrations
- Incorrect `_Imports.razor` configuration for auth components
- Authentication packages not properly referenced
- Server vs WebAssembly authentication model mismatches

**Solutions:**
- Add `Microsoft.AspNetCore.Components.Authorization` package reference
- Include `@using Microsoft.AspNetCore.Components.Authorization` in `_Imports.razor`
- Verify authentication services are registered in `Program.cs`
- Ensure authentication components match hosting model (Server vs WebAssembly)

## Quick Diagnostic Steps

### Step 1: Clean Build Environment
```bash
# Clean all build artifacts
dotnet clean

# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Rebuild solution
dotnet build
```

### Step 2: Verify Project Structure
- Check that all `.razor` files are in appropriate folders
- Verify `_Imports.razor` exists and contains necessary `@using` statements
- Ensure project references are correct and not circular
- Confirm file names follow PascalCase convention

### Step 3: Review Generated Files
Generated files are located in:
```
obj/Debug/net8.0/generated/Microsoft.NET.Sdk.Razor.SourceGenerators/
```

Examine these files to understand what the source generator is producing and identify where resolution fails.

### Step 4: Isolate the Problem
- Create a minimal reproduction case
- Remove custom MSBuild targets temporarily
- Disable third-party packages one by one
- Test with a fresh Blazor project template

## Configuration Best Practices

### _Imports.razor Configuration
```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using YourProject.Components
@using YourProject.Shared
```

### Project File Best Practices
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDefaultRazorGenerateItems>true</EnableDefaultRazorGenerateItems>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.11" />
  </ItemGroup>
</Project>
```

### Namespace Organization
- Keep all components in consistent namespaces
- Use project root namespace + folder structure
- Avoid deeply nested namespace hierarchies
- Group related components in same folders/namespaces

## Advanced Troubleshooting

### Enable Detailed Build Logging
```bash
dotnet build --verbosity diagnostic > build.log
```

Search the log file for:
- `RazorSourceGenerator` entries
- Error messages with full stack traces
- Dependency resolution failures

### MSBuild Property Debugging
Add to project file for detailed MSBuild information:
```xml
<PropertyGroup>
  <RazorLangVersion>Latest</RazorLangVersion>
  <RazorCompileOnBuild>true</RazorCompileOnBuild>
  <BlazorEnableCompression>false</BlazorEnableCompression>
</PropertyGroup>
```

### Source Generator Debugging
Temporarily disable source generator to isolate issues:
```xml
<PropertyGroup>
  <UseRazorSourceGenerator>false</UseRazorSourceGenerator>
</PropertyGroup>
```

## When to Seek Further Help

Contact Microsoft support or file GitHub issues when:
- Errors persist after following all troubleshooting steps
- Same configuration works in .NET 7 but fails in .NET 8
- Minimal reproduction case still demonstrates the problem
- Error occurs with standard Blazor project templates

## Prevention Strategies

1. **Use standard project templates** as starting points
2. **Keep dependencies up to date** and compatible
3. **Follow Blazor naming conventions** consistently
4. **Avoid complex MSBuild customizations** unless necessary
5. **Test with clean environments** regularly
6. **Document custom configurations** for team members

## Summary

Most Razor Source Generator errors stem from project configuration issues rather than SDK bugs. Systematic troubleshooting starting with clean builds and progressing through dependency verification usually resolves these issues. The source generator in .NET SDK 8.0.413 is stable and reliable when properly configured.