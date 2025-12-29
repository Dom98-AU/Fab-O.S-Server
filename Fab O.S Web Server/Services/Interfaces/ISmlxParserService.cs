using FabOS.WebServer.Models.DTOs;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service for parsing SMLX (Autodesk Advanced Steel) files to extract assemblies and parts
/// </summary>
public interface ISmlxParserService
{
    /// <summary>
    /// Parse an SMLX file (ZIP archive containing XML) and extract assemblies and parts
    /// </summary>
    /// <param name="fileStream">SMLX file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Parsed assemblies and loose parts with all geometry data</returns>
    Task<(List<ParsedAssemblyDto> Assemblies, List<ParsedPartDto> LooseParts)> ParseSmlxFileAsync(
        Stream fileStream,
        string fileName);

    /// <summary>
    /// Parse an SMLX file with identification tracking.
    /// Returns parts separated into identified (have part references) and unidentified (need user input).
    /// </summary>
    /// <param name="fileStream">SMLX file stream</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Parse result with identified/unidentified parts, assemblies with hierarchy</returns>
    Task<CadParseResultDto> ParseSmlxFileWithIdentificationAsync(
        Stream fileStream,
        string fileName);

    /// <summary>
    /// Validate SMLX file structure
    /// </summary>
    /// <param name="fileStream">SMLX file stream</param>
    /// <returns>True if valid SMLX file</returns>
    Task<bool> ValidateSmlxFileAsync(Stream fileStream);
}
