using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Models.DTOs.QDocs;

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
    /// Parse an IFC file with identification tracking.
    /// Returns parts separated into identified (have part references) and unidentified (need user input).
    /// </summary>
    /// <param name="fileStream">IFC file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Parse result with identified/unidentified parts, assemblies with hierarchy</returns>
    Task<CadParseResultDto> ParseIfcFileWithIdentificationAsync(
        Stream fileStream,
        string fileName);

    /// <summary>
    /// Validate IFC file structure
    /// </summary>
    /// <param name="fileStream">IFC file stream</param>
    /// <returns>True if valid IFC file</returns>
    Task<bool> ValidateIfcFileAsync(Stream fileStream);
}

/// <summary>
/// Result of parsing a CAD file with identification tracking
/// </summary>
public class CadParseResultDto
{
    public string FileType { get; set; } = string.Empty; // IFC, SMLX

    // All parts with temp IDs and identification status
    public List<ParsedPartWithIdDto> AllParts { get; set; } = new();

    // Assembly hierarchy
    public List<ParsedAssemblyWithIdDto> Assemblies { get; set; } = new();

    // Loose parts (not in any assembly)
    public List<ParsedPartWithIdDto> LooseParts { get; set; } = new();

    // Summary counts
    public int TotalElementCount { get; set; }
    public int IdentifiedPartCount { get; set; }
    public int UnidentifiedPartCount { get; set; }
    public int AssemblyCount { get; set; }

    // For tracking distinct part types (for suggested references)
    public Dictionary<string, int> PartTypeCounters { get; set; } = new();
}
