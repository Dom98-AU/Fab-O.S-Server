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

            // Find the main XML file in Contents folder
            var mainEntry = archive.Entries
                .FirstOrDefault(e => e.FullName.StartsWith("Contents/") && e.FullName.EndsWith(".xml"));

            if (mainEntry == null)
            {
                throw new InvalidOperationException("No XML file found in Contents folder");
            }

            _logger.LogInformation("Found main XML: {FullName}", mainEntry.FullName);

            using var entryStream = mainEntry.Open();
            var doc = await XDocument.LoadAsync(entryStream, LoadOptions.None, CancellationToken.None);

            // Parse all CGREXBeam objects (structural members)
            var beams = doc.Descendants("Object")
                .Where(obj => obj.Attribute("class")?.Value == "CGREXBeam");

            foreach (var beam in beams)
            {
                var part = ParseBeamElement(beam);
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

    private ParsedPartDto? ParseBeamElement(XElement beamElement)
    {
        try
        {
            // Navigate to CGREXBeam > CGREXLine > CGREXMember > CGREXObject > Members
            var members = beamElement.Descendants("Members").FirstOrDefault();
            if (members == null) return null;

            // Get part identification from m_strSinglePartMark
            var partReference = GetStringValue(members, "m_strSinglePartMark");
            if (string.IsNullOrEmpty(partReference)) return null;

            // Get assembly mark from m_strMark
            var assemblyMark = GetStringValue(members, "m_strMark");

            // Get part description from m_strName
            var description = GetStringValue(members, "m_strName") ?? "";

            // Get part type from m_strRole (Beam, Plate, etc.)
            var partType = GetStringValue(members, "m_strRole") ?? "Beam";

            // Get material data from CGREXMaterial object
            var materialGrade = GetMaterialGrade(beamElement);
            var materialStandard = GetMaterialStandard(beamElement);

            // Get coating from m_strCoating
            var coating = GetStringValue(members, "m_strCoating");

            // Get dimensions (in meters, need to convert to mm)
            var length = GetDoubleValue(members, "m_Length") * 1000; // Convert m to mm
            var width = GetDoubleValue(members, "m_Width") * 1000;
            var height = GetDoubleValue(members, "m_Height") * 1000;

            // Get weight and volume
            var weight = GetDoubleValue(members, "m_ExactWeight"); // Already in kg
            var volume = GetDoubleValue(members, "m_Volume"); // Already in m³
            var paintedArea = GetDoubleValue(members, "m_PaintedArea"); // Already in m²

            // Get section geometry for flange/web dimensions
            var (flangeWidth, flangeThickness, webThickness, webDepth) = GetSectionGeometry(beamElement);

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
                WebDepth = height > 0 ? (decimal?)height : null, // Height maps to WebDepth

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
            _logger.LogWarning(ex, "Error parsing beam element");
            return null;
        }
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
