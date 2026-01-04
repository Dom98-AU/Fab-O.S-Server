using System.Globalization;
using System.Text.RegularExpressions;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Models.DTOs.QDocs;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services;

/// <summary>
/// Parser for IFC (Industry Foundation Classes) files
/// IFC is a STEP text format (ISO-10303-21) containing building model data
/// Supports all major structural entity types from Tekla/Advanced Steel exports
/// </summary>
public class IfcParserService : IIfcParserService
{
    private readonly ILogger<IfcParserService> _logger;

    // Regex timeout to prevent ReDoS attacks - 1 second should be sufficient for any valid pattern
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Structural entity types to parse
    private static readonly HashSet<string> StructuralTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "IFCBEAM",
        "IFCPLATE",
        "IFCMEMBER",
        "IFCCOLUMN",
        "IFCFOOTING",
        "IFCSLAB",
        "IFCPILE",
        "IFCBUILDINGELEMENTPROXY"
    };

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

        // Relationship dictionaries
        var profiles = new Dictionary<string, IfcProfile>();
        var materials = new Dictionary<string, string>();
        var materialAssociations = new Dictionary<string, string>(); // elementId -> materialId
        var profileAssociations = new Dictionary<string, string>(); // elementId -> profileId
        var assemblyMembership = new Dictionary<string, string>(); // partId -> assemblyMark
        var quantities = new Dictionary<string, IfcQuantities>(); // elementId -> quantities

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

                var match = Regex.Match(line, @"#(\d+)=(.+);$", RegexOptions.None, RegexTimeout);
                if (match.Success)
                {
                    var id = match.Groups[1].Value;
                    var data = match.Groups[2].Value;
                    ifcData[id] = data;
                }
            }

            _logger.LogInformation("Loaded {Count} IFC entities from {FileName}", ifcData.Count, fileName);

            // Second pass: Build profile dictionary
            foreach (var (id, data) in ifcData)
            {
                var profile = ParseProfile(data);
                if (profile != null)
                {
                    profiles[id] = profile;
                }
            }

            // Third pass: Build material dictionary
            foreach (var (id, data) in ifcData)
            {
                if (data.StartsWith("IFCMATERIAL("))
                {
                    materials[id] = ParseMaterial(data);
                }
            }

            _logger.LogInformation("Extracted {ProfileCount} profiles and {MaterialCount} materials",
                profiles.Count, materials.Count);

            // Fourth pass: Build relationship mappings
            BuildRelationshipMappings(ifcData, materialAssociations, profileAssociations,
                assemblyMembership, quantities, profiles);

            _logger.LogInformation("Built relationships: {MaterialAssoc} material associations, {ProfileAssoc} profile associations, {Assemblies} assembly memberships",
                materialAssociations.Count, profileAssociations.Count, assemblyMembership.Count);

            // Fifth pass: Extract all structural elements
            foreach (var (id, data) in ifcData)
            {
                var entityType = GetEntityType(data);
                if (entityType == null || !StructuralTypes.Contains(entityType)) continue;

                var part = ParseStructuralElement(id, data, entityType, ifcData, profiles, materials,
                    materialAssociations, profileAssociations, assemblyMembership, quantities);

                if (part == null) continue;

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

    private string? GetEntityType(string data)
    {
        var match = Regex.Match(data, @"^(IFC\w+)\(", RegexOptions.None, RegexTimeout);
        return match.Success ? match.Groups[1].Value : null;
    }

    private void BuildRelationshipMappings(
        Dictionary<string, string> ifcData,
        Dictionary<string, string> materialAssociations,
        Dictionary<string, string> profileAssociations,
        Dictionary<string, string> assemblyMembership,
        Dictionary<string, IfcQuantities> quantities,
        Dictionary<string, IfcProfile> profiles)
    {
        foreach (var (id, data) in ifcData)
        {
            // Material associations: IFCRELASSOCIATESMATERIAL
            if (data.StartsWith("IFCRELASSOCIATESMATERIAL("))
            {
                ParseMaterialAssociation(data, ifcData, materialAssociations);
            }
            // Assembly grouping: IFCRELAGGREGATES
            else if (data.StartsWith("IFCRELAGGREGATES("))
            {
                ParseAggregation(data, ifcData, assemblyMembership);
            }
            // Product representation for profile association
            else if (data.StartsWith("IFCPRODUCTDEFINITIONSHAPE("))
            {
                // We'll handle profile association when parsing elements
            }
            // Quantity sets
            else if (data.StartsWith("IFCELEMENTQUANTITY("))
            {
                ParseQuantities(id, data, ifcData, quantities);
            }
            // Quantity property relationships
            else if (data.StartsWith("IFCRELDEFINESBYPROPERTIES("))
            {
                LinkQuantitiesToElements(data, ifcData, quantities);
            }
        }

        // Build profile associations from IFCPRODUCTDEFINITIONSHAPE -> IFCSHAPEREPRESENTATION -> IFCPROFILEDEF
        foreach (var (id, data) in ifcData)
        {
            if (!StructuralTypes.Any(t => data.StartsWith(t + "("))) continue;

            var profileId = TraverseToProfile(id, data, ifcData, profiles);
            if (profileId != null)
            {
                profileAssociations[id] = profileId;
            }
        }
    }

    private void ParseMaterialAssociation(string data, Dictionary<string, string> ifcData,
        Dictionary<string, string> materialAssociations)
    {
        // IFCRELASSOCIATESMATERIAL('...',#101,'...',$,(#301,#302,#303),#501)
        // Last reference is the material, elements are in the tuple
        try
        {
            // Extract elements list (in parentheses)
            var elementsMatch = Regex.Match(data, @"\(([#\d,\s]+)\)[^(]*$");
            if (!elementsMatch.Success) return;

            // Get the last reference which is the material
            var lastRefMatch = Regex.Match(data, @",#(\d+)\)$");
            if (!lastRefMatch.Success) return;

            var materialOrSetId = lastRefMatch.Groups[1].Value;

            // Resolve material (might be IFCMATERIAL or IFCMATERIALLAYERSETUSAGE)
            var materialId = ResolveMaterialId(materialOrSetId, ifcData);
            if (materialId == null) return;

            // Parse the elements list
            var elementsStr = elementsMatch.Groups[1].Value;
            var elementIds = Regex.Matches(elementsStr, @"#(\d+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value);

            foreach (var elementId in elementIds)
            {
                materialAssociations[elementId] = materialId;
            }
        }
        catch (Exception)
        {
            // Silently ignore parsing errors for individual relationships
        }
    }

    private string? ResolveMaterialId(string id, Dictionary<string, string> ifcData)
    {
        if (!ifcData.TryGetValue(id, out var data)) return null;

        if (data.StartsWith("IFCMATERIAL("))
        {
            return id;
        }
        else if (data.StartsWith("IFCMATERIALLAYERSETUSAGE("))
        {
            // IFCMATERIALLAYERSETUSAGE(#layerSetId,...)
            var match = Regex.Match(data, @"\(#(\d+)");
            if (match.Success)
            {
                return ResolveMaterialId(match.Groups[1].Value, ifcData);
            }
        }
        else if (data.StartsWith("IFCMATERIALLAYERSET("))
        {
            // IFCMATERIALLAYERSET((#layer1,#layer2),...)
            var match = Regex.Match(data, @"\(#(\d+)");
            if (match.Success)
            {
                return ResolveMaterialId(match.Groups[1].Value, ifcData);
            }
        }
        else if (data.StartsWith("IFCMATERIALLAYER("))
        {
            // IFCMATERIALLAYER(#materialId,thickness,...)
            var match = Regex.Match(data, @"\(#(\d+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private void ParseAggregation(string data, Dictionary<string, string> ifcData,
        Dictionary<string, string> assemblyMembership)
    {
        // IFCRELAGGREGATES('guid',#owner,'Name',$,#parentElement,(#child1,#child2,...))
        try
        {
            // Find parent element reference
            var parentMatch = Regex.Match(data, @",#(\d+),\([#\d,\s]+\)\)$");
            if (!parentMatch.Success) return;

            var parentId = parentMatch.Groups[1].Value;

            // Get the assembly mark from the parent element
            string? assemblyMark = null;
            if (ifcData.TryGetValue(parentId, out var parentData))
            {
                // Try to extract name/tag from parent
                var nameMatch = Regex.Match(parentData, @"'([^']+)'.*'([^']*)'");
                if (nameMatch.Success)
                {
                    assemblyMark = !string.IsNullOrEmpty(nameMatch.Groups[2].Value)
                        ? nameMatch.Groups[2].Value
                        : nameMatch.Groups[1].Value;
                }
            }

            if (string.IsNullOrEmpty(assemblyMark))
            {
                assemblyMark = $"ASM_{parentId}";
            }

            // Parse child elements
            var childrenMatch = Regex.Match(data, @"\(([#\d,\s]+)\)\)$");
            if (!childrenMatch.Success) return;

            var childIds = Regex.Matches(childrenMatch.Groups[1].Value, @"#(\d+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value);

            foreach (var childId in childIds)
            {
                assemblyMembership[childId] = assemblyMark;
            }
        }
        catch (Exception)
        {
            // Silently ignore parsing errors
        }
    }

    private void ParseQuantities(string qtySetId, string data, Dictionary<string, string> ifcData,
        Dictionary<string, IfcQuantities> quantities)
    {
        // IFCELEMENTQUANTITY('guid',#owner,'BaseQuantities',$,$,(#q1,#q2,#q3))
        try
        {
            var quantitiesMatch = Regex.Match(data, @"\(([#\d,\s]+)\)\)$");
            if (!quantitiesMatch.Success) return;

            var qty = new IfcQuantities();

            var qtyIds = Regex.Matches(quantitiesMatch.Groups[1].Value, @"#(\d+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value);

            foreach (var qtyId in qtyIds)
            {
                if (!ifcData.TryGetValue(qtyId, out var qtyData)) continue;

                if (qtyData.StartsWith("IFCQUANTITYWEIGHT("))
                {
                    // IFCQUANTITYWEIGHT('GrossWeight',$,#unit,123.45,$)
                    var match = Regex.Match(qtyData, @",([0-9.E+-]+),");
                    if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var weight))
                    {
                        qty.Weight = weight;
                    }
                }
                else if (qtyData.StartsWith("IFCQUANTITYVOLUME("))
                {
                    var match = Regex.Match(qtyData, @",([0-9.E+-]+),");
                    if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vol))
                    {
                        qty.Volume = vol;
                    }
                }
                else if (qtyData.StartsWith("IFCQUANTITYAREA("))
                {
                    var match = Regex.Match(qtyData, @",([0-9.E+-]+),");
                    if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var area))
                    {
                        qty.SurfaceArea = area;
                    }
                }
                else if (qtyData.StartsWith("IFCQUANTITYLENGTH("))
                {
                    // IFCQUANTITYLENGTH('Length',$,#unit,1500.0,$)
                    var match = Regex.Match(qtyData, @"'([^']+)'.*,([0-9.E+-]+),");
                    if (match.Success)
                    {
                        var name = match.Groups[1].Value.ToUpperInvariant();
                        if (double.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var len))
                        {
                            if (name.Contains("LENGTH") || name.Contains("HEIGHT"))
                            {
                                qty.Length = len;
                            }
                        }
                    }
                }
            }

            // Store with quantity set ID for later linking
            quantities[qtySetId] = qty;
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    private void LinkQuantitiesToElements(string data, Dictionary<string, string> ifcData,
        Dictionary<string, IfcQuantities> quantities)
    {
        // IFCRELDEFINESBYPROPERTIES('guid',#owner,$,$,(#element1,#element2),#propertySetOrQuantity)
        try
        {
            // Get the property/quantity reference
            var propMatch = Regex.Match(data, @",#(\d+)\)$");
            if (!propMatch.Success) return;

            var propId = propMatch.Groups[1].Value;
            if (!quantities.TryGetValue(propId, out var qty)) return;

            // Get the elements
            var elementsMatch = Regex.Match(data, @"\(([#\d,\s]+)\),#\d+\)$");
            if (!elementsMatch.Success) return;

            var elementIds = Regex.Matches(elementsMatch.Groups[1].Value, @"#(\d+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value);

            foreach (var elementId in elementIds)
            {
                quantities[elementId] = qty;
            }
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    private string? TraverseToProfile(string elementId, string data, Dictionary<string, string> ifcData,
        Dictionary<string, IfcProfile> profiles)
    {
        try
        {
            // Element has reference to IFCPRODUCTDEFINITIONSHAPE
            // Pattern: ...#representationId,...)
            var repMatch = Regex.Match(data, @",#(\d+),[^)]*\)$");
            if (!repMatch.Success) return null;

            var repId = repMatch.Groups[1].Value;
            if (!ifcData.TryGetValue(repId, out var repData)) return null;

            if (!repData.StartsWith("IFCPRODUCTDEFINITIONSHAPE(")) return null;

            // IFCPRODUCTDEFINITIONSHAPE($,$,(#shapeRep1,#shapeRep2))
            var shapesMatch = Regex.Match(repData, @"\(([#\d,\s]+)\)\)$");
            if (!shapesMatch.Success) return null;

            var shapeIds = Regex.Matches(shapesMatch.Groups[1].Value, @"#(\d+)")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value);

            foreach (var shapeId in shapeIds)
            {
                if (!ifcData.TryGetValue(shapeId, out var shapeData)) continue;
                if (!shapeData.StartsWith("IFCSHAPEREPRESENTATION(")) continue;

                // IFCSHAPEREPRESENTATION(#context,'Axis','Curve3D',(#item1,#item2))
                var itemsMatch = Regex.Match(shapeData, @"\(([#\d,\s]+)\)\)$");
                if (!itemsMatch.Success) continue;

                var itemIds = Regex.Matches(itemsMatch.Groups[1].Value, @"#(\d+)")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value);

                foreach (var itemId in itemIds)
                {
                    // Check if this item references a profile
                    if (profiles.ContainsKey(itemId))
                    {
                        return itemId;
                    }

                    // Check for IFCEXTRUDEDAREASOLID which contains profile reference
                    if (ifcData.TryGetValue(itemId, out var itemData) &&
                        itemData.StartsWith("IFCEXTRUDEDAREASOLID("))
                    {
                        // IFCEXTRUDEDAREASOLID(#profileId,#position,#direction,depth)
                        var profileMatch = Regex.Match(itemData, @"\(#(\d+)");
                        if (profileMatch.Success && profiles.ContainsKey(profileMatch.Groups[1].Value))
                        {
                            return profileMatch.Groups[1].Value;
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private IfcProfile? ParseProfile(string data)
    {
        try
        {
            // I-Shape profiles (UB, UC, W-shapes)
            if (data.StartsWith("IFCISHAPEPROFILEDEF("))
            {
                return ParseIShapeProfile(data);
            }
            // U-Shape profiles (PFC, channels)
            else if (data.StartsWith("IFCUSHAPEPROFILEDEF("))
            {
                return ParseUShapeProfile(data);
            }
            // L-Shape profiles (angles)
            else if (data.StartsWith("IFCLSHAPEPROFILEDEF("))
            {
                return ParseLShapeProfile(data);
            }
            // Rectangular hollow sections (RHS, SHS)
            else if (data.StartsWith("IFCRECTANGLEHOLLOWPROFILEDEF("))
            {
                return ParseRectangleHollowProfile(data);
            }
            // Circular hollow sections (CHS)
            else if (data.StartsWith("IFCCIRCLEHOLLOWPROFILEDEF("))
            {
                return ParseCircleHollowProfile(data);
            }
            // Solid rectangles (flats, plates)
            else if (data.StartsWith("IFCRECTANGLEPROFILEDEF("))
            {
                return ParseRectangleProfile(data);
            }
            // Circular sections (rounds)
            else if (data.StartsWith("IFCCIRCLEPROFILEDEF("))
            {
                return ParseCircleProfile(data);
            }
            // Asymmetric I-shapes
            else if (data.StartsWith("IFCASYMMETRICISHAPEPROFILEDEF("))
            {
                return ParseAsymmetricIShapeProfile(data);
            }
            // T-shapes
            else if (data.StartsWith("IFCTSHAPEPROFILEDEF("))
            {
                return ParseTShapeProfile(data);
            }
            // Z-shapes
            else if (data.StartsWith("IFCZSHAPEPROFILEDEF("))
            {
                return ParseZShapeProfile(data);
            }
        }
        catch (Exception)
        {
            // Return null for unparseable profiles
        }

        return null;
    }

    private IfcProfile ParseIShapeProfile(string data)
    {
        // IFCISHAPEPROFILEDEF(.AREA.,'310 UB 46.2',#pos,310.,166.,6.7,11.8,8.,$,$)
        // Parameters: type, name, position, depth, flangeWidth, webThickness, flangeThickness, filletRadius
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                ProfileType = "I",
                Name = match.Groups[1].Value,
                Depth = ParseDouble(match.Groups[2].Value),
                FlangeWidth = ParseDouble(match.Groups[3].Value),
                WebThickness = ParseDouble(match.Groups[4].Value),
                FlangeThickness = ParseDouble(match.Groups[5].Value)
            };
        }
        return new IfcProfile { ProfileType = "I", Name = "Unknown I-Section" };
    }

    private IfcProfile ParseUShapeProfile(string data)
    {
        // IFCUSHAPEPROFILEDEF(.AREA.,'150 PFC',#pos,150.,75.,6.,9.5,10.,0.,0.,$)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                ProfileType = "U",
                Name = match.Groups[1].Value,
                Depth = ParseDouble(match.Groups[2].Value),
                FlangeWidth = ParseDouble(match.Groups[3].Value),
                WebThickness = ParseDouble(match.Groups[4].Value),
                FlangeThickness = ParseDouble(match.Groups[5].Value)
            };
        }
        return new IfcProfile { ProfileType = "U", Name = "Unknown Channel" };
    }

    private IfcProfile ParseLShapeProfile(string data)
    {
        // IFCLSHAPEPROFILEDEF(.AREA.,'100x100x10 EA',#pos,100.,100.,10.,8.,5.,$,$,$)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                ProfileType = "L",
                Name = match.Groups[1].Value,
                LegA = ParseDouble(match.Groups[2].Value),
                LegB = ParseDouble(match.Groups[3].Value),
                Thickness = ParseDouble(match.Groups[4].Value)
            };
        }
        return new IfcProfile { ProfileType = "L", Name = "Unknown Angle" };
    }

    private IfcProfile ParseRectangleHollowProfile(string data)
    {
        // IFCRECTANGLEHOLLOWPROFILEDEF(.AREA.,'100x50x4 RHS',#pos,100.,50.,4.,8.,4.)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                ProfileType = "RHS",
                Name = match.Groups[1].Value,
                Depth = ParseDouble(match.Groups[2].Value),
                Width = ParseDouble(match.Groups[3].Value),
                WallThickness = ParseDouble(match.Groups[4].Value)
            };
        }
        return new IfcProfile { ProfileType = "RHS", Name = "Unknown RHS" };
    }

    private IfcProfile ParseCircleHollowProfile(string data)
    {
        // IFCCIRCLEHOLLOWPROFILEDEF(.AREA.,'114.3x4 CHS',#pos,57.15,4.)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            var radius = ParseDouble(match.Groups[2].Value);
            return new IfcProfile
            {
                ProfileType = "CHS",
                Name = match.Groups[1].Value,
                OutsideDiameter = radius * 2,
                WallThickness = ParseDouble(match.Groups[3].Value)
            };
        }
        return new IfcProfile { ProfileType = "CHS", Name = "Unknown CHS" };
    }

    private IfcProfile ParseRectangleProfile(string data)
    {
        // IFCRECTANGLEPROFILEDEF(.AREA.,'Flat 100x10',#pos,100.,10.)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                ProfileType = "FLAT",
                Name = match.Groups[1].Value,
                Width = ParseDouble(match.Groups[2].Value),
                Thickness = ParseDouble(match.Groups[3].Value)
            };
        }
        return new IfcProfile { ProfileType = "FLAT", Name = "Unknown Flat" };
    }

    private IfcProfile ParseCircleProfile(string data)
    {
        // IFCCIRCLEPROFILEDEF(.AREA.,'Round 20',#pos,10.)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+)");
        if (match.Success)
        {
            var radius = ParseDouble(match.Groups[2].Value);
            return new IfcProfile
            {
                ProfileType = "ROUND",
                Name = match.Groups[1].Value,
                Diameter = radius * 2
            };
        }
        return new IfcProfile { ProfileType = "ROUND", Name = "Unknown Round" };
    }

    private IfcProfile ParseAsymmetricIShapeProfile(string data)
    {
        // More complex parsing for asymmetric I-shapes
        var nameMatch = Regex.Match(data, @"'([^']*)'");
        return new IfcProfile
        {
            ProfileType = "I-ASYM",
            Name = nameMatch.Success ? nameMatch.Groups[1].Value : "Unknown Asymmetric I"
        };
    }

    private IfcProfile ParseTShapeProfile(string data)
    {
        // IFCTSHAPEPROFILEDEF(.AREA.,'T-Section',#pos,depth,flangeWidth,webThickness,flangeThickness,...)
        var match = Regex.Match(data, @"'([^']*)'[^,]*,#\d+,([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+)");
        if (match.Success)
        {
            return new IfcProfile
            {
                ProfileType = "T",
                Name = match.Groups[1].Value,
                Depth = ParseDouble(match.Groups[2].Value),
                FlangeWidth = ParseDouble(match.Groups[3].Value),
                WebThickness = ParseDouble(match.Groups[4].Value),
                FlangeThickness = ParseDouble(match.Groups[5].Value)
            };
        }
        return new IfcProfile { ProfileType = "T", Name = "Unknown T-Section" };
    }

    private IfcProfile ParseZShapeProfile(string data)
    {
        var nameMatch = Regex.Match(data, @"'([^']*)'");
        return new IfcProfile
        {
            ProfileType = "Z",
            Name = nameMatch.Success ? nameMatch.Groups[1].Value : "Unknown Z-Section"
        };
    }

    private string ParseMaterial(string data)
    {
        // IFCMATERIAL('300+') or IFCMATERIAL('Grade 300','Description',$)
        var match = Regex.Match(data, @"IFCMATERIAL\('([^']+)'");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    private ParsedPartDto? ParseStructuralElement(
        string id,
        string data,
        string entityType,
        Dictionary<string, string> ifcData,
        Dictionary<string, IfcProfile> profiles,
        Dictionary<string, string> materials,
        Dictionary<string, string> materialAssociations,
        Dictionary<string, string> profileAssociations,
        Dictionary<string, string> assemblyMembership,
        Dictionary<string, IfcQuantities> quantities)
    {
        try
        {
            // Parse basic element data
            // Pattern: IFCBEAM('guid',#owner,'Name','Description','ObjectType',#placement,#representation,tag)
            var basicMatch = Regex.Match(data, @"^IFC\w+\('[^']+',#\d+,'([^']*)','([^']*)'");
            if (!basicMatch.Success) return null;

            var name = basicMatch.Groups[1].Value;
            var description = basicMatch.Groups[2].Value;

            // Try to get tag (usually at the end)
            var tagMatch = Regex.Match(data, @",'([^']*)'[^']*\)$");
            var tag = tagMatch.Success ? tagMatch.Groups[1].Value : null;

            // Determine part reference
            var partReference = !string.IsNullOrEmpty(tag) ? tag
                : !string.IsNullOrEmpty(name) ? name
                : $"{entityType}_{id}";

            // Get profile
            IfcProfile? profile = null;
            if (profileAssociations.TryGetValue(id, out var profileId) && profiles.TryGetValue(profileId, out var p))
            {
                profile = p;
            }

            // Get material
            string? materialGrade = null;
            if (materialAssociations.TryGetValue(id, out var materialId) && materials.TryGetValue(materialId, out var mat))
            {
                materialGrade = mat;
            }

            // Get assembly mark
            assemblyMembership.TryGetValue(id, out var assemblyMark);

            // Get quantities
            quantities.TryGetValue(id, out var qty);

            // Map entity type to part type
            var partType = MapEntityToPartType(entityType);

            return new ParsedPartDto
            {
                PartReference = partReference,
                Description = profile?.Name ?? description ?? name,
                PartType = partType,
                MaterialGrade = materialGrade,
                MaterialStandard = InferStandardFromProfile(profile?.Name ?? description),

                // Geometry from profile (already in mm for IFC)
                Length = qty?.Length > 0 ? (decimal?)qty.Length : null,
                Width = profile?.Width > 0 ? (decimal?)profile.Width : null,
                Thickness = profile?.Thickness > 0 ? (decimal?)profile.Thickness : null,
                Diameter = profile?.Diameter > 0 ? (decimal?)profile.Diameter : null,
                FlangeThickness = profile?.FlangeThickness > 0 ? (decimal?)profile.FlangeThickness : null,
                FlangeWidth = profile?.FlangeWidth > 0 ? (decimal?)profile.FlangeWidth : null,
                WebThickness = profile?.WebThickness > 0 ? (decimal?)profile.WebThickness : null,
                WebDepth = profile?.Depth > 0 ? (decimal?)profile.Depth : null,
                OutsideDiameter = profile?.OutsideDiameter > 0 ? (decimal?)profile.OutsideDiameter : null,
                WallThickness = profile?.WallThickness > 0 ? (decimal?)profile.WallThickness : null,
                LegA = profile?.LegA > 0 ? (decimal?)profile.LegA : null,
                LegB = profile?.LegB > 0 ? (decimal?)profile.LegB : null,

                Weight = qty?.Weight > 0 ? (decimal?)qty.Weight : null,
                Volume = qty?.Volume > 0 ? (decimal?)qty.Volume : null,
                PaintedArea = qty?.SurfaceArea > 0 ? (decimal?)qty.SurfaceArea : null,

                AssemblyMark = assemblyMark,
                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing {EntityType} #{Id}", entityType, id);
            return null;
        }
    }

    private string MapEntityToPartType(string entityType)
    {
        return entityType.ToUpperInvariant() switch
        {
            "IFCBEAM" => "Beam",
            "IFCPLATE" => "Plate",
            "IFCMEMBER" => "Member",
            "IFCCOLUMN" => "Column",
            "IFCFOOTING" => "Footing",
            "IFCSLAB" => "Slab",
            "IFCPILE" => "Pile",
            "IFCBUILDINGELEMENTPROXY" => "Misc",
            _ => "Unknown"
        };
    }

    private double ParseDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private string? InferStandardFromProfile(string? profileName)
    {
        if (string.IsNullOrEmpty(profileName)) return null;

        var upper = profileName.ToUpperInvariant();
        if (upper.Contains("PFC")) return "ASNZS PFC";
        if (upper.Contains("EA") || upper.Contains("UA")) return "ASNZS EA";
        if (upper.Contains("UB") || upper.Contains("UC")) return "ASNZS UB/UC";
        if (upper.Contains("RHS")) return "ASNZS RHS";
        if (upper.Contains("SHS")) return "ASNZS SHS";
        if (upper.Contains("CHS")) return "ASNZS CHS";
        if (upper.Contains("W") && Regex.IsMatch(upper, @"W\d+X\d+")) return "AISC W";
        if (upper.Contains("HEB") || upper.Contains("HEA") || upper.Contains("IPE")) return "EN 10025";
        return null;
    }

    /// <summary>
    /// Parse IFC file with identification tracking - separates identified and unidentified parts
    /// Also identifies assemblies that need user input for naming
    /// </summary>
    public async Task<CadParseResultDto> ParseIfcFileWithIdentificationAsync(
        Stream fileStream,
        string fileName)
    {
        _logger.LogInformation("Parsing IFC file with identification tracking: {FileName}", fileName);

        var result = new CadParseResultDto
        {
            FileType = "IFC",
            PartTypeCounters = new Dictionary<string, int>()
        };

        var ifcData = new Dictionary<string, string>();
        var profiles = new Dictionary<string, IfcProfile>();
        var materials = new Dictionary<string, string>();
        var materialAssociations = new Dictionary<string, string>();
        var profileAssociations = new Dictionary<string, string>();
        var assemblyMembership = new Dictionary<string, string>();
        var quantities = new Dictionary<string, IfcQuantities>();

        try
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            string? line;

            // First pass: Read all IFC entities into dictionary
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("#")) continue;

                var match = Regex.Match(line, @"#(\d+)=(.+);$", RegexOptions.None, RegexTimeout);
                if (match.Success)
                {
                    var id = match.Groups[1].Value;
                    var data = match.Groups[2].Value;
                    ifcData[id] = data;
                }
            }

            _logger.LogInformation("Loaded {Count} IFC entities", ifcData.Count);

            // Build lookup dictionaries (profiles, materials)
            foreach (var (id, data) in ifcData)
            {
                var profile = ParseProfile(data);
                if (profile != null)
                    profiles[id] = profile;

                if (data.StartsWith("IFCMATERIAL("))
                    materials[id] = ParseMaterial(data);
            }

            // Build relationship mappings
            BuildRelationshipMappings(ifcData, materialAssociations, profileAssociations,
                assemblyMembership, quantities, profiles);

            // Dictionary to group parts by assembly
            var assemblyGroups = new Dictionary<string, ParsedAssemblyWithIdDto>();
            var looseParts = new List<ParsedPartWithIdDto>();

            // Parse all structural elements with identification tracking
            foreach (var (id, data) in ifcData)
            {
                var entityType = GetEntityType(data);
                if (entityType == null || !StructuralTypes.Contains(entityType)) continue;

                var part = ParseStructuralElementWithId(id, data, entityType, ifcData, profiles, materials,
                    materialAssociations, profileAssociations, assemblyMembership, quantities, result.PartTypeCounters);

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
                        // Determine if assembly mark is meaningful or auto-generated
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
                "IFC parsing complete: {Total} elements, {Identified} identified, {Unidentified} unidentified, {Assemblies} assemblies ({UnidentifiedAssemblies} need naming)",
                result.TotalElementCount,
                result.IdentifiedPartCount,
                result.UnidentifiedPartCount,
                result.AssemblyCount,
                result.Assemblies.Count(a => !a.IsIdentified));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing IFC file with identification: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Check if an assembly mark is meaningful or auto-generated
    /// </summary>
    private bool IsAssemblyMarkMeaningful(string assemblyMark)
    {
        if (string.IsNullOrWhiteSpace(assemblyMark))
            return false;

        // Auto-generated marks from IFC parsing start with "ASM_"
        if (assemblyMark.StartsWith("ASM_", StringComparison.OrdinalIgnoreCase))
            return false;

        // Marks that are just numbers are typically auto-generated
        if (int.TryParse(assemblyMark, out _))
            return false;

        // Very short marks (1-2 chars) might be meaningless
        if (assemblyMark.Length <= 2)
            return false;

        return true;
    }

    private ParsedPartWithIdDto? ParseStructuralElementWithId(
        string id,
        string data,
        string entityType,
        Dictionary<string, string> ifcData,
        Dictionary<string, IfcProfile> profiles,
        Dictionary<string, string> materials,
        Dictionary<string, string> materialAssociations,
        Dictionary<string, string> profileAssociations,
        Dictionary<string, string> assemblyMembership,
        Dictionary<string, IfcQuantities> quantities,
        Dictionary<string, int> typeCounters)
    {
        try
        {
            // Parse basic element data
            var basicMatch = Regex.Match(data, @"^IFC\w+\('[^']+',#\d+,'([^']*)','([^']*)'");
            if (!basicMatch.Success) return null;

            var name = basicMatch.Groups[1].Value;
            var description = basicMatch.Groups[2].Value;

            // Get tag
            var tagMatch = Regex.Match(data, @",'([^']*)'[^']*\)$");
            var tag = tagMatch.Success ? tagMatch.Groups[1].Value : null;

            // Determine if part is identified (has meaningful reference)
            var hasReference = !string.IsNullOrEmpty(tag) && tag != "$";
            var isIdentified = hasReference;

            // Get part reference or generate suggested one
            var partReference = hasReference ? tag!
                : !string.IsNullOrEmpty(name) ? name
                : $"{entityType}_{id}";

            // Get profile
            IfcProfile? profile = null;
            if (profileAssociations.TryGetValue(id, out var profileId) && profiles.TryGetValue(profileId, out var p))
            {
                profile = p;
            }

            // Get material
            string? materialGrade = null;
            if (materialAssociations.TryGetValue(id, out var materialId) && materials.TryGetValue(materialId, out var mat))
            {
                materialGrade = mat;
            }

            // Get assembly mark
            assemblyMembership.TryGetValue(id, out var assemblyMark);

            // Get quantities
            quantities.TryGetValue(id, out var qty);

            // Map entity type to part type
            var partType = MapEntityToPartType(entityType);

            // Generate suggested reference if not identified
            string? suggestedReference = null;
            if (!isIdentified)
            {
                suggestedReference = GenerateSuggestedReference(profile?.Name ?? description ?? partType, partType, typeCounters);
            }

            return new ParsedPartWithIdDto
            {
                // Temp ID and identification status
                TempPartId = Guid.NewGuid().ToString(),
                IsIdentified = isIdentified,
                SuggestedReference = suggestedReference,
                IfcElementName = name,
                IfcObjectType = entityType,

                // Standard part data
                PartReference = partReference,
                Description = profile?.Name ?? description ?? name,
                PartType = partType,
                MaterialGrade = materialGrade,
                MaterialStandard = InferStandardFromProfile(profile?.Name ?? description),

                // Geometry
                Length = qty?.Length > 0 ? (decimal?)qty.Length : null,
                Width = profile?.Width > 0 ? (decimal?)profile.Width : null,
                Thickness = profile?.Thickness > 0 ? (decimal?)profile.Thickness : null,
                Diameter = profile?.Diameter > 0 ? (decimal?)profile.Diameter : null,
                FlangeThickness = profile?.FlangeThickness > 0 ? (decimal?)profile.FlangeThickness : null,
                FlangeWidth = profile?.FlangeWidth > 0 ? (decimal?)profile.FlangeWidth : null,
                WebThickness = profile?.WebThickness > 0 ? (decimal?)profile.WebThickness : null,
                WebDepth = profile?.Depth > 0 ? (decimal?)profile.Depth : null,
                OutsideDiameter = profile?.OutsideDiameter > 0 ? (decimal?)profile.OutsideDiameter : null,
                WallThickness = profile?.WallThickness > 0 ? (decimal?)profile.WallThickness : null,
                LegA = profile?.LegA > 0 ? (decimal?)profile.LegA : null,
                LegB = profile?.LegB > 0 ? (decimal?)profile.LegB : null,

                Weight = qty?.Weight > 0 ? (decimal?)qty.Weight : null,
                Volume = qty?.Volume > 0 ? (decimal?)qty.Volume : null,
                PaintedArea = qty?.SurfaceArea > 0 ? (decimal?)qty.SurfaceArea : null,

                AssemblyMark = assemblyMark,
                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing {EntityType} #{Id} with identification", entityType, id);
            return null;
        }
    }

    private string GenerateSuggestedReference(string description, string partType, Dictionary<string, int> typeCounters)
    {
        string key;

        if (!string.IsNullOrEmpty(description))
        {
            // Clean up description for use as key
            key = Regex.Replace(description, @"[^\w]", "").ToUpperInvariant();
            if (key.Length > 20) key = key.Substring(0, 20);
        }
        else
        {
            key = partType.ToUpperInvariant();
        }

        // Increment counter
        if (!typeCounters.ContainsKey(key))
            typeCounters[key] = 0;
        typeCounters[key]++;

        return $"{key}-{typeCounters[key]}";
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
        public string ProfileType { get; set; } = "";
        public string Name { get; set; } = "";
        public double Depth { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public double Diameter { get; set; }
        public double FlangeWidth { get; set; }
        public double FlangeThickness { get; set; }
        public double WebThickness { get; set; }
        public double OutsideDiameter { get; set; }
        public double WallThickness { get; set; }
        public double LegA { get; set; }
        public double LegB { get; set; }
    }

    private class IfcQuantities
    {
        public double Weight { get; set; }
        public double Volume { get; set; }
        public double SurfaceArea { get; set; }
        public double Length { get; set; }
    }
}
