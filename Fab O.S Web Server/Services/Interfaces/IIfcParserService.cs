using FabOS.WebServer.Models.DTOs;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for parsing IFC (Industry Foundation Classes) files to extract assemblies and parts
/// </summary>
public interface IIfcParserService
{
    /// <summary>
    /// Parse an IFC file (STEP text format) and extract assemblies and parts
    /// </summary>
    /// <param name="fileStream">IFC file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Parsed assemblies and loose parts with all geometry data</returns>
    Task<(List<ParsedAssemblyDto> Assemblies, List<ParsedPartDto> LooseParts)> ParseIfcFileAsync(
        Stream fileStream,
        string fileName);

    /// <summary>
    /// Validate IFC file structure
    /// </summary>
    /// <param name="fileStream">IFC file stream</param>
    /// <returns>True if valid IFC file</returns>
    Task<bool> ValidateIfcFileAsync(Stream fileStream);
}
