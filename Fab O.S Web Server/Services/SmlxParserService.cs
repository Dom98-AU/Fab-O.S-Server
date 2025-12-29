using System.IO.Compression;
using System.Xml.Linq;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services;

/// <summary>
/// Parser for SMLX (Autodesk Advanced Steel) files
/// SMLX is a ZIP archive containing XML files with CGREX objects
/// </summary>
public class SmlxParserService : ISmlxParserService
{
    private readonly ILogger<SmlxParserService> _logger;

    public SmlxParserService(ILogger<SmlxParserService> logger)
    {
        _logger = logger;
    }

    public async Task<(List<ParsedAssemblyDto> Assemblies, List<ParsedPartDto> LooseParts)> ParseSmlxFileAsync(
        Stream fileStream,
        string fileName)
    {
        _logger.LogInformation("Parsing SMLX file: {FileName}", fileName);

        var assemblies = new Dictionary<string, ParsedAssemblyDto>();
        var looseParts = new List<ParsedPartDto>();

        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

            // ZIP bomb protection: limit total uncompressed size to 100MB
            const long MaxUncompressedSize = 100 * 1024 * 1024; // 100MB
            var totalUncompressedSize = archive.Entries.Sum(e => e.Length);
            if (totalUncompressedSize > MaxUncompressedSize)
            {
                throw new InvalidOperationException($"SMLX file too large. Uncompressed size {totalUncompressedSize / 1024 / 1024}MB exceeds maximum of {MaxUncompressedSize / 1024 / 1024}MB");
            }

            // Find the main XML file in Contents folder
            var mainEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.StartsWith("Contents/") && e.FullName.EndsWith(".xml"));

            if (mainEntry == null)
            {
                throw new InvalidOperationException("No XML file found in Contents folder");
            }

            // Additional size check for the main XML entry
            if (mainEntry.Length > MaxUncompressedSize)
            {
                throw new InvalidOperationException($"XML file too large. Size {mainEntry.Length / 1024 / 1024}MB exceeds maximum");
            }

            _logger.LogInformation("Found main XML: {FullName} ({Size} bytes)", mainEntry.FullName, mainEntry.Length);

            using var entryStream = mainEntry.Open();
            var doc = await XDocument.LoadAsync(entryStream, LoadOptions.None, CancellationToken.None);

            // Parse all structural element types: CGREXBeam, CGREXPlate, CGREXFoldedPlate, CGREXSpecialPart
            var structuralClasses = new[] { "CGREXBeam", "CGREXPlate", "CGREXFoldedPlate", "CGREXSpecialPart" };
            var elements = doc.Descendants("Object")
                .Where(obj => structuralClasses.Contains(obj.Attribute("class")?.Value));

            foreach (var element in elements)
            {
                var elementClass = element.Attribute("class")?.Value ?? "";
                var part = ParseStructuralElement(element, elementClass);
                if (part != null)
                {
                    // Check if part belongs to an assembly
                    if (!string.IsNullOrEmpty(part.AssemblyMark))
                    {
                        if (!assemblies.ContainsKey(part.AssemblyMark))
                        {
                            assemblies[part.AssemblyMark] = new ParsedAssemblyDto
                            {
                                AssemblyMark = part.AssemblyMark,
                                AssemblyName = part.AssemblyMark, // Default to mark, can be customized
                                Parts = new List<ParsedPartDto>()
                            };
                        }
                        assemblies[part.AssemblyMark].Parts.Add(part);
                    }
                    else
                    {
                        // Loose part (not in any assembly)
                        looseParts.Add(part);
                    }
                }
            }

            // Calculate assembly totals
            foreach (var assembly in assemblies.Values)
            {
                assembly.TotalWeight = assembly.Parts.Sum(p => p.Weight ?? 0);
            }

            _logger.LogInformation(
                "SMLX parsing complete: {AssemblyCount} assemblies, {PartCount} total parts, {LoosePartCount} loose parts",
                assemblies.Count,
                assemblies.Values.Sum(a => a.Parts.Count) + looseParts.Count,
                looseParts.Count);

            return (assemblies.Values.ToList(), looseParts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing SMLX file: {FileName}", fileName);
            throw;
        }
    }

    private ParsedPartDto? ParseStructuralElement(XElement element, string elementClass)
    {
        try
        {
            // SMLX structure: CGREXBeam/CGREXPlate/CGREXFoldedPlate > ... > CGREXMember > CGREXObject > Members
            // Data is spread across multiple Members elements at different nesting levels:
            // - CGREXMember/Members: m_strName (description), m_strMark (assembly), m_strRole, m_strCoating, m_dfWeight
            // - Element/Members: m_strSinglePartMark (part ref), m_Length, m_Width, m_Height, m_Volume, m_PaintedArea

            // Get ALL Members elements within this element
            var allMembers = element.Descendants("Members").ToList();
            if (!allMembers.Any()) return null;

            // Find part reference from ANY Members element
            var partReference = FindStringValueInAnyMembers(allMembers, "m_strSinglePartMark");
            if (string.IsNullOrEmpty(partReference)) return null;

            // Get assembly mark from m_strMark (in CGREXMember/Members)
            var assemblyMark = FindStringValueInAnyMembers(allMembers, "m_strMark");

            // Get part description from m_strName
            var description = FindStringValueInAnyMembers(allMembers, "m_strName") ?? "";

            // Get part type from m_strRole (Beam, Plate, etc.) or derive from element class
            var partType = FindStringValueInAnyMembers(allMembers, "m_strRole");
            if (string.IsNullOrEmpty(partType))
            {
                partType = elementClass switch
                {
                    "CGREXBeam" => "Beam",
                    "CGREXPlate" => "Plate",
                    "CGREXFoldedPlate" => "FoldedPlate",
                    "CGREXSpecialPart" => "SpecialPart",
                    _ => "Unknown"
                };
            }

            // Get material data from CGREXMaterial object
            var materialGrade = GetMaterialGrade(element);
            var materialStandard = GetMaterialStandard(element);

            // Get coating from m_strCoating
            var coating = FindStringValueInAnyMembers(allMembers, "m_strCoating");

            // Get dimensions (in meters, need to convert to mm)
            var length = FindDoubleValueInAnyMembers(allMembers, "m_Length") * 1000; // Convert m to mm
            var width = FindDoubleValueInAnyMembers(allMembers, "m_Width") * 1000;
            var height = FindDoubleValueInAnyMembers(allMembers, "m_Height") * 1000;
            var thickness = FindDoubleValueInAnyMembers(allMembers, "m_Thickness") * 1000; // For plates

            // Get weight - try m_dfWeight first, then m_ExactWeight
            var weight = FindDoubleValueInAnyMembers(allMembers, "m_dfWeight");
            if (weight <= 0)
                weight = FindDoubleValueInAnyMembers(allMembers, "m_ExactWeight");

            var volume = FindDoubleValueInAnyMembers(allMembers, "m_Volume"); // Already in m³
            var paintedArea = FindDoubleValueInAnyMembers(allMembers, "m_PaintedArea"); // Already in m²

            // Get section geometry for flange/web dimensions (mainly for beams)
            var (flangeWidth, flangeThickness, webThickness, webDepth) = GetSectionGeometry(element);

            return new ParsedPartDto
            {
                PartReference = partReference,
                Description = description,
                PartType = partType,
                MaterialGrade = materialGrade,
                MaterialStandard = materialStandard,
                Coating = coating,

                // Dimensions (mm)
                Length = length > 0 ? (decimal?)length : null,
                Width = width > 0 ? (decimal?)width : null,
                Thickness = thickness > 0 ? (decimal?)thickness : null, // For plates
                WebDepth = height > 0 ? (decimal?)height : null, // Height maps to WebDepth for beams

                // Section geometry (mm)
                FlangeWidth = flangeWidth > 0 ? (decimal?)flangeWidth : null,
                FlangeThickness = flangeThickness > 0 ? (decimal?)flangeThickness : null,
                WebThickness = webThickness > 0 ? (decimal?)webThickness : null,

                // CAD data
                Weight = weight > 0 ? (decimal?)weight : null,
                Volume = volume > 0 ? (decimal?)volume : null,
                PaintedArea = paintedArea > 0 ? (decimal?)paintedArea : null,

                // Assembly tracking
                AssemblyMark = !string.IsNullOrEmpty(assemblyMark) ? assemblyMark : null,

                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing structural element: {ElementClass}", elementClass);
            return null;
        }
    }

    private string? FindStringValueInAnyMembers(List<XElement> allMembers, string elementName)
    {
        foreach (var members in allMembers)
        {
            var value = GetStringValue(members, elementName);
            if (!string.IsNullOrEmpty(value)) return value;
        }
        return null;
    }

    private double FindDoubleValueInAnyMembers(List<XElement> allMembers, string elementName)
    {
        foreach (var members in allMembers)
        {
            var value = GetDoubleValue(members, elementName);
            if (value > 0) return value;
        }
        return 0;
    }

    private string? GetStringValue(XElement members, string elementName)
    {
        var element = members.Elements(elementName).FirstOrDefault();
        return element?.Attribute("string")?.Value;
    }

    private double GetDoubleValue(XElement members, string elementName)
    {
        var element = members.Elements(elementName).FirstOrDefault();
        var value = element?.Attribute("double")?.Value;
        return double.TryParse(value, out var result) ? result : 0;
    }

    private string? GetMaterialGrade(XElement beamElement)
    {
        // Find CGREXMaterial object and get m_strName
        var materialObject = beamElement.Descendants("Object")
            .FirstOrDefault(obj => obj.Attribute("class")?.Value == "CGREXMaterial");

        if (materialObject != null)
        {
            var members = materialObject.Elements("Members").FirstOrDefault();
            if (members != null)
            {
                return GetStringValue(members, "m_strName");
            }
        }

        return null;
    }

    private string? GetMaterialStandard(XElement beamElement)
    {
        // Find CGREXSection object and get m_strStandard
        var sectionObject = beamElement.Descendants("Object")
            .FirstOrDefault(obj => obj.Attribute("class")?.Value == "CGREXSection");

        if (sectionObject != null)
        {
            var members = sectionObject.Elements("Members").FirstOrDefault();
            if (members != null)
            {
                return GetStringValue(members, "m_strStandard");
            }
        }

        return null;
    }

    private (double flangeWidth, double flangeThickness, double webThickness, double webDepth) GetSectionGeometry(XElement beamElement)
    {
        // Find CGREXSection object and calculate from geometry points
        var sectionObject = beamElement.Descendants("Object")
            .FirstOrDefault(obj => obj.Attribute("class")?.Value == "CGREXSection");

        if (sectionObject != null)
        {
            var members = sectionObject.Elements("Members").FirstOrDefault();
            if (members != null)
            {
                // For a channel (PFC), we can calculate dimensions from geometry points
                // Point pattern for PFC: outer flange edge → inner flange → web → inner flange → outer flange
                var points = members.Elements()
                    .Where(e => e.Name.LocalName.StartsWith("m_tabGeometryPoints_"))
                    .Select(e => e.Descendants("Members").FirstOrDefault())
                    .Where(m => m != null)
                    .Select(m => new
                    {
                        X = GetDoubleValue(m!, "m_dfX"),
                        Y = GetDoubleValue(m!, "m_dfY"),
                        Z = GetDoubleValue(m!, "m_dfZ")
                    })
                    .ToList();

                if (points.Count >= 9) // PFC has 9 points
                {
                    // Calculate flange width (Y direction, convert m to mm)
                    var flangeWidth = (points.Max(p => p.Y) - points.Min(p => p.Y)) * 1000;

                    // Calculate web depth (Z direction, convert m to mm)
                    var webDepth = (points.Max(p => p.Z) - points.Min(p => p.Z)) * 1000;

                    // Flange thickness (difference between outer and inner Y)
                    var flangeThickness = Math.Abs((points[0].Y - points[1].Y)) * 1000;

                    // Web thickness (difference between inner and outer X at web)
                    var webThickness = Math.Abs((points[1].Y - points[2].Y)) * 1000;

                    return (flangeWidth, flangeThickness, webThickness, webDepth);
                }
            }
        }

        return (0, 0, 0, 0);
    }

    /// <summary>
    /// Parse SMLX file with identification tracking - separates identified and unidentified parts
    /// </summary>
    public async Task<CadParseResultDto> ParseSmlxFileWithIdentificationAsync(
        Stream fileStream,
        string fileName)
    {
        _logger.LogInformation("Parsing SMLX file with identification tracking: {FileName}", fileName);

        var result = new CadParseResultDto
        {
            FileType = "SMLX",
            PartTypeCounters = new Dictionary<string, int>()
        };

        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

            // ZIP bomb protection: limit total uncompressed size to 100MB
            const long MaxUncompressedSize = 100 * 1024 * 1024; // 100MB
            var totalUncompressedSize = archive.Entries.Sum(e => e.Length);
            if (totalUncompressedSize > MaxUncompressedSize)
            {
                throw new InvalidOperationException($"SMLX file too large. Uncompressed size {totalUncompressedSize / 1024 / 1024}MB exceeds maximum of {MaxUncompressedSize / 1024 / 1024}MB");
            }

            // Find the main XML file in Contents folder
            var mainEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.StartsWith("Contents/") && e.FullName.EndsWith(".xml"));

            if (mainEntry == null)
            {
                throw new InvalidOperationException("No XML file found in Contents folder");
            }

            // Additional size check for the main XML entry
            if (mainEntry.Length > MaxUncompressedSize)
            {
                throw new InvalidOperationException($"XML file too large. Size {mainEntry.Length / 1024 / 1024}MB exceeds maximum");
            }

            _logger.LogInformation("Found main XML: {FullName} ({Size} bytes)", mainEntry.FullName, mainEntry.Length);

            using var entryStream = mainEntry.Open();
            var doc = await XDocument.LoadAsync(entryStream, LoadOptions.None, CancellationToken.None);

            // Parse all structural element types
            var structuralClasses = new[] { "CGREXBeam", "CGREXPlate", "CGREXFoldedPlate", "CGREXSpecialPart" };
            var elements = doc.Descendants("Object")
                .Where(obj => structuralClasses.Contains(obj.Attribute("class")?.Value));

            // Dictionary to group parts by assembly
            var assemblyGroups = new Dictionary<string, ParsedAssemblyWithIdDto>();
            var looseParts = new List<ParsedPartWithIdDto>();

            foreach (var element in elements)
            {
                var elementClass = element.Attribute("class")?.Value ?? "";
                var part = ParseStructuralElementWithId(element, elementClass, result.PartTypeCounters);
                if (part == null) continue;

                result.TotalElementCount++;
                result.AllParts.Add(part);

                if (part.IsIdentified)
                    result.IdentifiedPartCount++;
                else
                    result.UnidentifiedPartCount++;

                // Group by assembly
                if (!string.IsNullOrEmpty(part.AssemblyMark))
                {
                    if (!assemblyGroups.ContainsKey(part.AssemblyMark))
                    {
                        // Determine if assembly mark is meaningful or auto-generated/unclear
                        var isAssemblyIdentified = IsAssemblyMarkMeaningful(part.AssemblyMark);

                        assemblyGroups[part.AssemblyMark] = new ParsedAssemblyWithIdDto
                        {
                            TempAssemblyId = Guid.NewGuid().ToString(),
                            AssemblyMark = isAssemblyIdentified ? part.AssemblyMark : null,
                            AssemblyName = isAssemblyIdentified ? part.AssemblyMark : null,
                            IsIdentified = isAssemblyIdentified,
                            SuggestedAssemblyMark = isAssemblyIdentified ? null : part.AssemblyMark, // Keep original for reference
                            IdentifiedParts = new List<ParsedPartWithIdDto>(),
                            UnidentifiedParts = new List<ParsedPartWithIdDto>()
                        };
                    }

                    part.TempAssemblyId = assemblyGroups[part.AssemblyMark].TempAssemblyId;

                    if (part.IsIdentified)
                        assemblyGroups[part.AssemblyMark].IdentifiedParts.Add(part);
                    else
                        assemblyGroups[part.AssemblyMark].UnidentifiedParts.Add(part);
                }
                else
                {
                    looseParts.Add(part);
                }
            }

            // Calculate assembly totals
            foreach (var assembly in assemblyGroups.Values)
            {
                assembly.TotalWeight = assembly.IdentifiedParts.Sum(p => p.Weight ?? 0) +
                                       assembly.UnidentifiedParts.Sum(p => p.Weight ?? 0);
                assembly.Parts = assembly.IdentifiedParts.Cast<ParsedPartDto>().ToList();
            }

            result.Assemblies = assemblyGroups.Values.ToList();
            result.LooseParts = looseParts;
            result.AssemblyCount = assemblyGroups.Count;

            _logger.LogInformation(
                "SMLX parsing complete: {Total} elements, {Identified} identified, {Unidentified} unidentified, {Assemblies} assemblies",
                result.TotalElementCount,
                result.IdentifiedPartCount,
                result.UnidentifiedPartCount,
                result.AssemblyCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing SMLX file with identification: {FileName}", fileName);
            throw;
        }
    }

    private ParsedPartWithIdDto? ParseStructuralElementWithId(XElement element, string elementClass, Dictionary<string, int> typeCounters)
    {
        try
        {
            // Get ALL Members elements within this element
            var allMembers = element.Descendants("Members").ToList();
            if (!allMembers.Any()) return null;

            // Find part reference from ANY Members element
            var partReference = FindStringValueInAnyMembers(allMembers, "m_strSinglePartMark");

            // Determine if this part is identified
            var isIdentified = !string.IsNullOrEmpty(partReference);

            // Get assembly mark
            var assemblyMark = FindStringValueInAnyMembers(allMembers, "m_strMark");

            // Get part description
            var description = FindStringValueInAnyMembers(allMembers, "m_strName") ?? "";

            // Get part type
            var partType = FindStringValueInAnyMembers(allMembers, "m_strRole");
            if (string.IsNullOrEmpty(partType))
            {
                partType = elementClass switch
                {
                    "CGREXBeam" => "Beam",
                    "CGREXPlate" => "Plate",
                    "CGREXFoldedPlate" => "FoldedPlate",
                    "CGREXSpecialPart" => "SpecialPart",
                    _ => "Unknown"
                };
            }

            // Generate suggested reference if not identified
            string? suggestedReference = null;
            if (!isIdentified)
            {
                suggestedReference = GenerateSuggestedReference(description, partType, typeCounters);
            }

            // Use the actual reference if identified, otherwise use suggested
            var finalReference = isIdentified ? partReference! : suggestedReference ?? $"UNID-{element.GetHashCode()}";

            // Get material and dimensions
            var materialGrade = GetMaterialGrade(element);
            var materialStandard = GetMaterialStandard(element);
            var coating = FindStringValueInAnyMembers(allMembers, "m_strCoating");

            // Get dimensions (convert m to mm)
            var length = FindDoubleValueInAnyMembers(allMembers, "m_Length") * 1000;
            var width = FindDoubleValueInAnyMembers(allMembers, "m_Width") * 1000;
            var height = FindDoubleValueInAnyMembers(allMembers, "m_Height") * 1000;
            var thickness = FindDoubleValueInAnyMembers(allMembers, "m_Thickness") * 1000;

            var weight = FindDoubleValueInAnyMembers(allMembers, "m_dfWeight");
            if (weight <= 0)
                weight = FindDoubleValueInAnyMembers(allMembers, "m_ExactWeight");

            var volume = FindDoubleValueInAnyMembers(allMembers, "m_Volume");
            var paintedArea = FindDoubleValueInAnyMembers(allMembers, "m_PaintedArea");

            var (flangeWidth, flangeThickness, webThickness, webDepth) = GetSectionGeometry(element);

            return new ParsedPartWithIdDto
            {
                // Temp ID and identification status
                TempPartId = Guid.NewGuid().ToString(),
                IsIdentified = isIdentified,
                SuggestedReference = suggestedReference,
                IfcElementName = description, // Use description as element name for SMLX
                IfcObjectType = elementClass, // Use class as object type

                // Standard part data
                PartReference = finalReference,
                Description = description,
                PartType = partType,
                MaterialGrade = materialGrade,
                MaterialStandard = materialStandard,
                Coating = coating,

                // Dimensions (mm)
                Length = length > 0 ? (decimal?)length : null,
                Width = width > 0 ? (decimal?)width : null,
                Thickness = thickness > 0 ? (decimal?)thickness : null,
                WebDepth = height > 0 ? (decimal?)height : null,

                // Section geometry (mm)
                FlangeWidth = flangeWidth > 0 ? (decimal?)flangeWidth : null,
                FlangeThickness = flangeThickness > 0 ? (decimal?)flangeThickness : null,
                WebThickness = webThickness > 0 ? (decimal?)webThickness : null,

                // CAD data
                Weight = weight > 0 ? (decimal?)weight : null,
                Volume = volume > 0 ? (decimal?)volume : null,
                PaintedArea = paintedArea > 0 ? (decimal?)paintedArea : null,

                // Assembly tracking
                AssemblyMark = !string.IsNullOrEmpty(assemblyMark) ? assemblyMark : null,

                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing structural element with ID: {ElementClass}", elementClass);
            return null;
        }
    }

    private string GenerateSuggestedReference(string description, string partType, Dictionary<string, int> typeCounters)
    {
        // Create a key based on description or part type
        string key;

        if (!string.IsNullOrEmpty(description))
        {
            // Clean up description for use as key (e.g., "150 PFC" -> "150PFC")
            key = description.Replace(" ", "").ToUpperInvariant();

            // Limit key length
            if (key.Length > 20) key = key.Substring(0, 20);
        }
        else
        {
            // Fall back to part type
            key = partType.ToUpperInvariant();
        }

        // Increment counter for this key
        if (!typeCounters.ContainsKey(key))
            typeCounters[key] = 0;
        typeCounters[key]++;

        // Return suggested reference like "150PFC-1", "BEAM-1", etc.
        return $"{key}-{typeCounters[key]}";
    }

    /// <summary>
    /// Check if an assembly mark is meaningful or auto-generated/unclear
    /// </summary>
    private bool IsAssemblyMarkMeaningful(string assemblyMark)
    {
        if (string.IsNullOrWhiteSpace(assemblyMark))
            return false;

        // Marks that are just numbers are typically auto-generated
        if (int.TryParse(assemblyMark, out _))
            return false;

        // Very short marks (1-2 chars) might be meaningless
        if (assemblyMark.Length <= 2)
            return false;

        // Auto-generated marks often start with common prefixes
        if (assemblyMark.StartsWith("ASM_", StringComparison.OrdinalIgnoreCase))
            return false;
        if (assemblyMark.StartsWith("AUTO_", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public async Task<bool> ValidateSmlxFileAsync(Stream fileStream)
    {
        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);

            // Check for Contents folder with XML file
            var hasContents = archive.Entries.Any(e => e.FullName.StartsWith("Contents/") && e.FullName.EndsWith(".xml"));

            if (!hasContents)
            {
                _logger.LogWarning("SMLX validation failed: No XML file found in Contents folder");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SMLX file");
            return false;
        }
    }
}
