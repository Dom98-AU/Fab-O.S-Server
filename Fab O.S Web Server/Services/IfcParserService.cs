using System.Text.RegularExpressions;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services;

/// <summary>
/// Parser for IFC (Industry Foundation Classes) files
/// IFC is a STEP text format (ISO-10303-21) containing building model data
/// </summary>
public class IfcParserService : IIfcParserService
{
    private readonly ILogger<IfcParserService> _logger;

    public IfcParserService(ILogger<IfcParserService> logger)
    {
        _logger = logger;
    }

    public async Task<(List<ParsedAssemblyDto> Assemblies, List<ParsedPartDto> LooseParts)> ParseIfcFileAsync(
        Stream fileStream,
        string fileName)
    {
        _logger.LogInformation("Parsing IFC file: {FileName}", fileName);

        var assemblies = new Dictionary<string, ParsedAssemblyDto>();
        var looseParts = new List<ParsedPartDto>();
        var ifcData = new Dictionary<string, string>();
        var profiles = new Dictionary<string, IfcProfile>();
        var materials = new Dictionary<string, string>();

        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            string? line;
            int lineNumber = 0;

            // First pass: Read all IFC entities into dictionary
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("#")) continue;

                var match = Regex.Match(line, @"#(\d+)=(.+);");
                if (match.Success)
                {
                    var id = match.Groups[1].Value;
                    var data = match.Groups[2].Value;
                    ifcData[id] = data;
                }
            }

            _logger.LogInformation("Loaded {Count} IFC entities from {FileName}", ifcData.Count, fileName);

            // Second pass: Extract profiles
            foreach (var (id, data) in ifcData)
            {
                if (data.StartsWith("IFCUSHAPEPROFILEDEF"))
                {
                    profiles[id] = ParseUShapeProfile(data);
                }
                else if (data.StartsWith("IFCLSHAPEPROFILEDEF"))
                {
                    profiles[id] = ParseLShapeProfile(data);
                }
                else if (data.StartsWith("IFCMATERIAL"))
                {
                    materials[id] = ParseMaterial(data);
                }
            }

            _logger.LogInformation("Extracted {ProfileCount} profiles and {MaterialCount} materials",
                profiles.Count, materials.Count);

            // Third pass: Extract beams and plates
            foreach (var (id, data) in ifcData)
            {
                if (data.StartsWith("IFCBEAM"))
                {
                    var part = ParseBeam(id, data, ifcData, profiles, materials);
                    if (part != null)
                    {
                        if (!string.IsNullOrEmpty(part.AssemblyMark))
                        {
                            if (!assemblies.ContainsKey(part.AssemblyMark))
                            {
                                assemblies[part.AssemblyMark] = new ParsedAssemblyDto
                                {
                                    AssemblyMark = part.AssemblyMark,
                                    AssemblyName = part.AssemblyMark,
                                    Parts = new List<ParsedPartDto>()
                                };
                            }
                            assemblies[part.AssemblyMark].Parts.Add(part);
                        }
                        else
                        {
                            looseParts.Add(part);
                        }
                    }
                }
                else if (data.StartsWith("IFCPLATE"))
                {
                    var part = ParsePlate(id, data, ifcData, materials);
                    if (part != null)
                    {
                        if (!string.IsNullOrEmpty(part.AssemblyMark))
                        {
                            if (!assemblies.ContainsKey(part.AssemblyMark))
                            {
                                assemblies[part.AssemblyMark] = new ParsedAssemblyDto
                                {
                                    AssemblyMark = part.AssemblyMark,
                                    AssemblyName = part.AssemblyMark,
                                    Parts = new List<ParsedPartDto>()
                                };
                            }
                            assemblies[part.AssemblyMark].Parts.Add(part);
                        }
                        else
                        {
                            looseParts.Add(part);
                        }
                    }
                }
            }

            // Calculate assembly totals
            foreach (var assembly in assemblies.Values)
            {
                assembly.TotalWeight = assembly.Parts.Sum(p => p.Weight ?? 0);
            }

            _logger.LogInformation(
                "IFC parsing complete: {AssemblyCount} assemblies, {PartCount} total parts, {LoosePartCount} loose parts",
                assemblies.Count,
                assemblies.Values.Sum(a => a.Parts.Count) + looseParts.Count,
                looseParts.Count);

            return (assemblies.Values.ToList(), looseParts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing IFC file: {FileName}", fileName);
            throw;
        }
    }

    private IfcProfile ParseUShapeProfile(string data)
    {
        // IFCUSHAPEPROFILEDEF(.AREA.,'150 PFC',#380,150.,75.,6.,9.5,10.,0.,0.,$)
        var match = Regex.Match(data, @"'([^']+)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                Name = match.Groups[1].Value,
                Depth = double.Parse(match.Groups[2].Value),
                FlangeWidth = double.Parse(match.Groups[3].Value),
                WebThickness = double.Parse(match.Groups[4].Value),
                FlangeThickness = double.Parse(match.Groups[5].Value)
            };
        }
        return new IfcProfile { Name = "Unknown" };
    }

    private IfcProfile ParseLShapeProfile(string data)
    {
        // IFCLSHAPEPROFILEDEF(.AREA.,'100x100x10 EA',#2840,100.,100.,10.,8.,5.,$,$,$)
        var match = Regex.Match(data, @"'([^']+)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                Name = match.Groups[1].Value,
                Depth = double.Parse(match.Groups[2].Value), // LegA
                FlangeWidth = double.Parse(match.Groups[3].Value), // LegB
                WebThickness = double.Parse(match.Groups[4].Value) // Thickness
            };
        }
        return new IfcProfile { Name = "Unknown" };
    }

    private string ParseMaterial(string data)
    {
        // IFCMATERIAL('300+')
        var match = Regex.Match(data, @"IFCMATERIAL\('([^']+)'\)");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    private ParsedPartDto? ParseBeam(string id, string data, Dictionary<string, string> ifcData,
        Dictionary<string, IfcProfile> profiles, Dictionary<string, string> materials)
    {
        try
        {
            // IFCBEAM('1gzDwd6vf0nfc_Xf_u0hQD',#150,'Horizontal_Beam','PFC 150','Horizontal_Beam',#930,#980,$)
            var match = Regex.Match(data, @"IFCBEAM\('[^']+',#\d+,'([^']*)','([^']*)'");
            if (!match.Success) return null;

            var partReference = match.Groups[1].Value;
            var description = match.Groups[2].Value;

            // Find associated profile
            var profileId = ExtractProfileId(id, ifcData);
            var profile = profileId != null && profiles.ContainsKey(profileId)
                ? profiles[profileId]
                : new IfcProfile { Name = description };

            // Find associated material
            var materialId = ExtractMaterialId(id, ifcData);
            var materialGrade = materialId != null && materials.ContainsKey(materialId)
                ? materials[materialId]
                : null;

            // Try to extract assembly mark (simplified - would need IFCRELAGGREGATES parsing for full support)
            string? assemblyMark = null;

            return new ParsedPartDto
            {
                PartReference = !string.IsNullOrEmpty(partReference) ? partReference : $"BEAM_{id}",
                Description = profile.Name,
                PartType = "Beam",
                MaterialGrade = materialGrade,
                MaterialStandard = InferStandardFromProfile(profile.Name),

                // Geometry from profile (already in mm)
                WebDepth = profile.Depth > 0 ? (decimal?)profile.Depth : null,
                FlangeWidth = profile.FlangeWidth > 0 ? (decimal?)profile.FlangeWidth : null,
                WebThickness = profile.WebThickness > 0 ? (decimal?)profile.WebThickness : null,
                FlangeThickness = profile.FlangeThickness > 0 ? (decimal?)profile.FlangeThickness : null,

                AssemblyMark = assemblyMark,
                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing IFCBEAM #{Id}", id);
            return null;
        }
    }

    private ParsedPartDto? ParsePlate(string id, string data, Dictionary<string, string> ifcData,
        Dictionary<string, string> materials)
    {
        try
        {
            // IFCPLATE('3x_2ryvob6ZQwrYwIHA1kA',#150,'Plate','-','Plate',#13430,#13480,$)
            var match = Regex.Match(data, @"IFCPLATE\('[^']+',#\d+,'([^']*)'");
            if (!match.Success) return null;

            var partReference = match.Groups[1].Value;

            // Find thickness from IFCMATERIALLAYER
            var thickness = ExtractPlateThickness(id, ifcData);

            // Find material
            var materialId = ExtractMaterialId(id, ifcData);
            var materialGrade = materialId != null && materials.ContainsKey(materialId)
                ? materials[materialId]
                : null;

            return new ParsedPartDto
            {
                PartReference = !string.IsNullOrEmpty(partReference) ? partReference : $"PLATE_{id}",
                Description = "Plate",
                PartType = "Plate",
                MaterialGrade = materialGrade,

                Thickness = thickness > 0 ? (decimal?)thickness : null,

                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing IFCPLATE #{Id}", id);
            return null;
        }
    }

    private string? ExtractProfileId(string beamId, Dictionary<string, string> ifcData)
    {
        // Simplified - would need to traverse IFCPROFILEDEF relationships
        return null;
    }

    private string? ExtractMaterialId(string elementId, Dictionary<string, string> ifcData)
    {
        // Simplified - would need to traverse IFCMATERIAL relationships
        return null;
    }

    private double ExtractPlateThickness(string plateId, Dictionary<string, string> ifcData)
    {
        // Look for IFCMATERIALLAYER associated with this plate
        foreach (var (id, data) in ifcData)
        {
            if (data.Contains("IFCMATERIALLAYER"))
            {
                var match = Regex.Match(data, @"IFCMATERIALLAYER\(#\d+,([0-9.]+)");
                if (match.Success)
                {
                    return double.Parse(match.Groups[1].Value);
                }
            }
        }
        return 0;
    }

    private string? InferStandardFromProfile(string profileName)
    {
        if (profileName.Contains("PFC")) return "ASNZS PFC";
        if (profileName.Contains("EA")) return "ASNZS EA";
        if (profileName.Contains("UB") || profileName.Contains("UC")) return "ASNZS UB/UC";
        return null;
    }

    public async Task<bool> ValidateIfcFileAsync(Stream fileStream)
    {
        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            var firstLine = await reader.ReadLineAsync();

            if (firstLine == null || !firstLine.StartsWith("ISO-10303-21"))
            {
                _logger.LogWarning("IFC validation failed: Not a valid STEP file");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating IFC file");
            return false;
        }
    }

    private class IfcProfile
    {
        public string Name { get; set; } = "";
        public double Depth { get; set; }
        public double FlangeWidth { get; set; }
        public double WebThickness { get; set; }
        public double FlangeThickness { get; set; }
    }
}
