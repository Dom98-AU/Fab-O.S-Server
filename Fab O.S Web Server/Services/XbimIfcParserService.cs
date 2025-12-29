using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Services.Interfaces;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces; // Cross-schema interfaces for IFC2x3 and IFC4
using Xbim.IO.Memory;

namespace FabOS.WebServer.Services;

/// <summary>
/// xBim-based parser for IFC (Industry Foundation Classes) files
/// Uses the xBim library for proper IFC model traversal and data extraction
/// Supports IFC2x3 and IFC4 schemas
/// </summary>
public class XbimIfcParserService : IIfcParserService
{
    private readonly ILogger<XbimIfcParserService> _logger;

    public XbimIfcParserService(ILogger<XbimIfcParserService> logger)
    {
        _logger = logger;
    }

    public async Task<(List<ParsedAssemblyDto> Assemblies, List<ParsedPartDto> LooseParts)> ParseIfcFileAsync(
        Stream fileStream,
        string fileName)
    {
        _logger.LogInformation("Parsing IFC file with xBim: {FileName}", fileName);

        var assemblies = new Dictionary<string, ParsedAssemblyDto>();
        var looseParts = new List<ParsedPartDto>();

        try
        {
            // Copy stream to memory for xBim (it needs seekable stream)
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Open IFC model with xBim
            using var model = MemoryModel.OpenReadStep21(memoryStream);

            _logger.LogInformation("Loaded IFC model: {Schema}, {EntityCount} entities",
                model.SchemaVersion, model.Instances.Count);

            // Get all structural elements
            var structuralElements = model.Instances
                .OfType<IIfcElement>()
                .Where(e => IsStructuralElement(e))
                .ToList();

            _logger.LogInformation("Found {Count} structural elements", structuralElements.Count);

            foreach (var element in structuralElements)
            {
                var part = ExtractPartData(element, model);
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
                "xBim IFC parsing complete: {AssemblyCount} assemblies, {PartCount} total parts, {LoosePartCount} loose parts",
                assemblies.Count,
                assemblies.Values.Sum(a => a.Parts.Count) + looseParts.Count,
                looseParts.Count);

            return (assemblies.Values.ToList(), looseParts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing IFC file with xBim: {FileName}", fileName);
            throw;
        }
    }

    private bool IsStructuralElement(IIfcElement element)
    {
        return element is IIfcBeam or
               IIfcPlate or
               IIfcMember or
               IIfcColumn or
               IIfcFooting or
               IIfcSlab or
               IIfcPile or
               IIfcMechanicalFastener or  // Bolts
               IIfcBuildingElementProxy;
    }

    /// <summary>
    /// Parse IFC file with identification tracking - separates identified and unidentified parts
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

        try
        {
            // Copy stream to memory for xBim (it needs seekable stream)
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Open IFC model with xBim
            using var model = MemoryModel.OpenReadStep21(memoryStream);

            _logger.LogInformation("Loaded IFC model: {Schema}, {EntityCount} entities",
                model.SchemaVersion, model.Instances.Count);

            // Get all structural elements
            var structuralElements = model.Instances
                .OfType<IIfcElement>()
                .Where(e => IsStructuralElement(e))
                .ToList();

            result.TotalElementCount = structuralElements.Count;
            _logger.LogInformation("Found {Count} structural elements", structuralElements.Count);

            // Dictionary to group parts by assembly
            var assemblyGroups = new Dictionary<string, ParsedAssemblyWithIdDto>();
            var looseParts = new List<ParsedPartWithIdDto>();

            foreach (var element in structuralElements)
            {
                var part = ExtractPartDataWithId(element, model, result.PartTypeCounters);
                if (part == null) continue;

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
                        assemblyGroups[part.AssemblyMark] = new ParsedAssemblyWithIdDto
                        {
                            TempAssemblyId = Guid.NewGuid().ToString(),
                            AssemblyMark = part.AssemblyMark,
                            AssemblyName = part.AssemblyMark,
                            IsIdentified = true,
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
                "IFC parsing complete: {Total} elements, {Identified} identified, {Unidentified} unidentified, {Assemblies} assemblies",
                result.TotalElementCount,
                result.IdentifiedPartCount,
                result.UnidentifiedPartCount,
                result.AssemblyCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing IFC file with identification: {FileName}", fileName);
            throw;
        }
    }

    private ParsedPartWithIdDto? ExtractPartDataWithId(IIfcElement element, IModel model, Dictionary<string, int> typeCounters)
    {
        try
        {
            // Get basic info from IFC element
            var name = element.Name?.ToString() ?? "";
            var description = element.Description?.ToString() ?? "";
            var tag = element.Tag?.ToString() ?? "";
            var objectType = element.ObjectType?.ToString() ?? "";

            // Get part type from IFC entity type
            var partType = MapElementToPartType(element);

            // Extract material
            var materialGrade = GetMaterialGrade(element, model);

            // Extract profile geometry
            var profile = GetProfileData(element, model);

            // Extract quantities
            var quantities = GetQuantities(element, model);

            // Get assembly mark
            var assemblyMark = GetAssemblyMark(element, model);

            // Get Reference property from property sets (the official part mark)
            var referenceFromProps = GetPropertyValue(element, model, "Reference");

            // Determine if this part is identified (has a valid part reference)
            var isIdentified = !string.IsNullOrEmpty(referenceFromProps);

            // Generate a suggested reference if not identified
            string? suggestedReference = null;
            if (!isIdentified)
            {
                suggestedReference = GenerateSuggestedReference(element, profile, objectType, typeCounters);
            }

            var partReference = isIdentified ? referenceFromProps! : suggestedReference ?? $"UNID-{element.EntityLabel}";

            return new ParsedPartWithIdDto
            {
                // Temp ID for tracking
                TempPartId = Guid.NewGuid().ToString(),
                IsIdentified = isIdentified,
                IfcElementName = name,
                IfcObjectType = objectType,
                SuggestedReference = suggestedReference,

                // Standard part data
                PartReference = partReference,
                Description = profile?.Name ?? description ?? objectType ?? name,
                PartType = partType,
                MaterialGrade = materialGrade,
                MaterialStandard = InferStandardFromProfile(profile?.Name ?? description),

                // Geometry (in mm)
                Length = quantities.Length > 0 ? (decimal?)quantities.Length : null,
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

                Weight = quantities.Weight > 0 ? (decimal?)quantities.Weight : null,
                Volume = quantities.Volume > 0 ? (decimal?)quantities.Volume : null,
                PaintedArea = quantities.SurfaceArea > 0 ? (decimal?)quantities.SurfaceArea : null,

                AssemblyMark = assemblyMark,
                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting data from element #{EntityLabel}", element.EntityLabel);
            return null;
        }
    }

    private string GenerateSuggestedReference(IIfcElement element, ProfileData? profile, string objectType, Dictionary<string, int> typeCounters)
    {
        // Create a key based on element type and profile
        string key;

        if (element is IIfcMechanicalFastener fastener)
        {
            // For bolts/fasteners, use the object type
            var fastenerType = objectType.ToUpperInvariant();
            if (fastenerType.Contains("BOLT"))
                key = "BOLT";
            else if (fastenerType.Contains("NUT"))
                key = "NUT";
            else
                key = "FASTENER";
        }
        else if (element is IIfcBuildingElementProxy)
        {
            // For proxy elements, use object type
            var proxyType = objectType.ToUpperInvariant();
            if (proxyType.Contains("NUT"))
                key = "NUT";
            else if (proxyType.Contains("WASHER"))
                key = "WASHER";
            else
                key = "MISC";
        }
        else if (profile != null && !string.IsNullOrEmpty(profile.Name))
        {
            // Use profile name (e.g., "PFC 150" -> "PFC150")
            key = profile.Name.Replace(" ", "").ToUpperInvariant();
        }
        else
        {
            // Fall back to element type
            key = MapElementToPartType(element).ToUpperInvariant();
        }

        // Increment counter for this key
        if (!typeCounters.ContainsKey(key))
            typeCounters[key] = 0;
        typeCounters[key]++;

        // Return suggested reference like "PFC150-1", "BOLT-1", etc.
        return $"{key}-{typeCounters[key]}";
    }

    private ParsedPartDto? ExtractPartData(IIfcElement element, IModel model)
    {
        try
        {
            // Get basic info
            var name = element.Name?.ToString() ?? "";
            var description = element.Description?.ToString() ?? "";
            var tag = element.Tag?.ToString() ?? "";
            var objectType = element.ObjectType?.ToString() ?? "";

            // Determine part reference - prefer Tag, then Name
            var partReference = !string.IsNullOrEmpty(tag) ? tag
                : !string.IsNullOrEmpty(name) ? name
                : $"{element.GetType().Name}_{element.EntityLabel}";

            // Get part type from IFC entity type
            var partType = MapElementToPartType(element);

            // Extract material from IFCRELASSOCIATESMATERIAL
            var materialGrade = GetMaterialGrade(element, model);

            // Extract profile geometry
            var profile = GetProfileData(element, model);

            // Extract quantities from property sets
            var quantities = GetQuantities(element, model);

            // Get assembly mark from IFCRELAGGREGATES or property sets
            var assemblyMark = GetAssemblyMark(element, model);

            // Get Reference property from property sets (often used as part mark)
            var referenceFromProps = GetPropertyValue(element, model, "Reference");
            if (!string.IsNullOrEmpty(referenceFromProps))
            {
                partReference = referenceFromProps;
            }

            return new ParsedPartDto
            {
                PartReference = partReference,
                Description = profile?.Name ?? description ?? objectType ?? name,
                PartType = partType,
                MaterialGrade = materialGrade,
                MaterialStandard = InferStandardFromProfile(profile?.Name ?? description),

                // Geometry from profile (in mm)
                Length = quantities.Length > 0 ? (decimal?)quantities.Length : null,
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

                Weight = quantities.Weight > 0 ? (decimal?)quantities.Weight : null,
                Volume = quantities.Volume > 0 ? (decimal?)quantities.Volume : null,
                PaintedArea = quantities.SurfaceArea > 0 ? (decimal?)quantities.SurfaceArea : null,

                AssemblyMark = assemblyMark,
                Quantity = 1,
                Unit = "EA"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting data from element #{EntityLabel}", element.EntityLabel);
            return null;
        }
    }

    private string MapElementToPartType(IIfcElement element)
    {
        return element switch
        {
            IIfcBeam => "Beam",
            IIfcPlate => "Plate",
            IIfcMember => "Member",
            IIfcColumn => "Column",
            IIfcFooting => "Footing",
            IIfcSlab => "Slab",
            IIfcPile => "Pile",
            IIfcMechanicalFastener => "Fastener",
            IIfcBuildingElementProxy proxy => MapProxyToPartType(proxy),
            _ => "Unknown"
        };
    }

    private string MapProxyToPartType(IIfcBuildingElementProxy proxy)
    {
        var objectType = proxy.ObjectType?.ToString()?.ToUpperInvariant() ?? "";

        if (objectType.Contains("NUT")) return "Nut";
        if (objectType.Contains("WASHER")) return "Washer";
        if (objectType.Contains("BOLT")) return "Bolt";
        if (objectType.Contains("ANCHOR")) return "Anchor";

        return "Misc";
    }

    private string? GetMaterialGrade(IIfcElement element, IModel model)
    {
        try
        {
            // Find IFCRELASSOCIATESMATERIAL relationships for this element
            var materialRels = model.Instances
                .OfType<IIfcRelAssociatesMaterial>()
                .Where(r => r.RelatedObjects.Contains(element));

            foreach (var rel in materialRels)
            {
                var material = rel.RelatingMaterial;

                // Direct material reference
                if (material is IIfcMaterial mat)
                {
                    return mat.Name.ToString();
                }

                // Material layer set
                if (material is IIfcMaterialLayerSetUsage layerSetUsage)
                {
                    var layer = layerSetUsage.ForLayerSet?.MaterialLayers?.FirstOrDefault();
                    if (layer?.Material != null)
                    {
                        return layer.Material.Name.ToString();
                    }
                }

                // Material layer set (direct)
                if (material is IIfcMaterialLayerSet layerSet)
                {
                    var layer = layerSet.MaterialLayers?.FirstOrDefault();
                    if (layer?.Material != null)
                    {
                        return layer.Material.Name.ToString();
                    }
                }

                // Material profile set
                if (material is IIfcMaterialProfileSetUsage profileSetUsage)
                {
                    var profile = profileSetUsage.ForProfileSet?.MaterialProfiles?.FirstOrDefault();
                    if (profile?.Material != null)
                    {
                        return profile.Material.Name.ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting material for element #{EntityLabel}", element.EntityLabel);
        }

        return null;
    }

    private ProfileData? GetProfileData(IIfcElement element, IModel model)
    {
        try
        {
            // Get the product representation
            var representation = element.Representation;
            if (representation == null) return null;

            // Find profile definitions in shape representations
            foreach (var rep in representation.Representations)
            {
                foreach (var item in rep.Items)
                {
                    // Check for extruded area solid which contains the profile
                    if (item is IIfcExtrudedAreaSolid extruded)
                    {
                        return ExtractProfileFromSweptArea(extruded.SweptArea);
                    }

                    // Check for boolean results
                    if (item is IIfcBooleanResult boolResult)
                    {
                        var profile = ExtractProfileFromBooleanOperand(boolResult.FirstOperand);
                        if (profile != null) return profile;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting profile for element #{EntityLabel}", element.EntityLabel);
        }

        return null;
    }

    private ProfileData? ExtractProfileFromBooleanOperand(IIfcBooleanOperand operand)
    {
        if (operand is IIfcExtrudedAreaSolid extruded)
        {
            return ExtractProfileFromSweptArea(extruded.SweptArea);
        }
        if (operand is IIfcBooleanResult nested)
        {
            return ExtractProfileFromBooleanOperand(nested.FirstOperand);
        }
        return null;
    }

    private ProfileData? ExtractProfileFromSweptArea(IIfcProfileDef? profileDef)
    {
        if (profileDef == null) return null;

        var profile = new ProfileData
        {
            Name = profileDef.ProfileName?.ToString() ?? ""
        };

        switch (profileDef)
        {
            case IIfcIShapeProfileDef iShape:
                profile.ProfileType = "I";
                profile.Depth = iShape.OverallDepth;
                profile.FlangeWidth = iShape.OverallWidth;
                profile.WebThickness = iShape.WebThickness;
                profile.FlangeThickness = iShape.FlangeThickness;
                break;

            case IIfcUShapeProfileDef uShape:
                profile.ProfileType = "U";
                profile.Depth = uShape.Depth;
                profile.FlangeWidth = uShape.FlangeWidth;
                profile.WebThickness = uShape.WebThickness;
                profile.FlangeThickness = uShape.FlangeThickness;
                break;

            case IIfcLShapeProfileDef lShape:
                profile.ProfileType = "L";
                profile.LegA = lShape.Depth;
                profile.LegB = lShape.Width ?? lShape.Depth; // Equal angle if width not specified
                profile.Thickness = lShape.Thickness;
                break;

            case IIfcRectangleHollowProfileDef rectHollow:
                profile.ProfileType = "RHS";
                profile.Depth = rectHollow.XDim;
                profile.Width = rectHollow.YDim;
                profile.WallThickness = rectHollow.WallThickness;
                break;

            case IIfcCircleHollowProfileDef circleHollow:
                profile.ProfileType = "CHS";
                profile.OutsideDiameter = circleHollow.Radius * 2;
                profile.WallThickness = circleHollow.WallThickness;
                break;

            case IIfcRectangleProfileDef rect:
                profile.ProfileType = "FLAT";
                profile.Width = rect.XDim;
                profile.Thickness = rect.YDim;
                break;

            case IIfcCircleProfileDef circle:
                profile.ProfileType = "ROUND";
                profile.Diameter = circle.Radius * 2;
                break;

            case IIfcTShapeProfileDef tShape:
                profile.ProfileType = "T";
                profile.Depth = tShape.Depth;
                profile.FlangeWidth = tShape.FlangeWidth;
                profile.WebThickness = tShape.WebThickness;
                profile.FlangeThickness = tShape.FlangeThickness;
                break;
        }

        return profile;
    }

    private QuantityData GetQuantities(IIfcElement element, IModel model)
    {
        var quantities = new QuantityData();

        try
        {
            // Find quantity sets linked to this element via IFCRELDEFINESBYPROPERTIES
            var propRels = model.Instances
                .OfType<IIfcRelDefinesByProperties>()
                .Where(r => r.RelatedObjects.Contains(element));

            foreach (var rel in propRels)
            {
                if (rel.RelatingPropertyDefinition is IIfcElementQuantity elemQty)
                {
                    foreach (var qty in elemQty.Quantities)
                    {
                        switch (qty)
                        {
                            case IIfcQuantityWeight weightQty:
                                quantities.Weight = weightQty.WeightValue;
                                break;
                            case IIfcQuantityVolume volQty:
                                quantities.Volume = volQty.VolumeValue;
                                break;
                            case IIfcQuantityArea areaQty:
                                quantities.SurfaceArea = areaQty.AreaValue;
                                break;
                            case IIfcQuantityLength lengthQty:
                                var name = lengthQty.Name.ToString().ToUpperInvariant();
                                if (name.Contains("LENGTH") || name.Contains("HEIGHT") || name == "LENGTH")
                                {
                                    quantities.Length = lengthQty.LengthValue;
                                }
                                break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting quantities for element #{EntityLabel}", element.EntityLabel);
        }

        return quantities;
    }

    private string? GetAssemblyMark(IIfcElement element, IModel model)
    {
        try
        {
            // Check if element is part of an aggregation
            var aggregateRels = model.Instances
                .OfType<IIfcRelAggregates>()
                .Where(r => r.RelatedObjects.Contains(element));

            foreach (var rel in aggregateRels)
            {
                var parent = rel.RelatingObject;
                if (parent != null)
                {
                    // Use parent's name or tag as assembly mark
                    var mark = parent.Name?.ToString();
                    if (!string.IsNullOrEmpty(mark)) return mark;

                    if (parent is IIfcProduct product)
                    {
                        mark = GetPropertyValue(element, model, "AssemblyMark")
                            ?? GetPropertyValue(element, model, "Assembly");
                        if (!string.IsNullOrEmpty(mark)) return mark;
                    }
                }
            }

            // Also check property sets for assembly mark
            var propMark = GetPropertyValue(element, model, "AssemblyMark")
                ?? GetPropertyValue(element, model, "Assembly")
                ?? GetPropertyValue(element, model, "Mark");

            return propMark;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting assembly mark for element #{EntityLabel}", element.EntityLabel);
        }

        return null;
    }

    private string? GetPropertyValue(IIfcElement element, IModel model, string propertyName)
    {
        try
        {
            var propRels = model.Instances
                .OfType<IIfcRelDefinesByProperties>()
                .Where(r => r.RelatedObjects.Contains(element));

            foreach (var rel in propRels)
            {
                if (rel.RelatingPropertyDefinition is IIfcPropertySet propSet)
                {
                    foreach (var prop in propSet.HasProperties)
                    {
                        if (prop is IIfcPropertySingleValue singleValue)
                        {
                            if (string.Equals(prop.Name.ToString(), propertyName, StringComparison.OrdinalIgnoreCase))
                            {
                                return singleValue.NominalValue?.ToString();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting property {PropertyName} for element #{EntityLabel}",
                propertyName, element.EntityLabel);
        }

        return null;
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
        if (upper.Contains("W") && System.Text.RegularExpressions.Regex.IsMatch(upper, @"W\d+X\d+")) return "AISC W";
        if (upper.Contains("HEB") || upper.Contains("HEA") || upper.Contains("IPE")) return "EN 10025";
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

    private class ProfileData
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

    private class QuantityData
    {
        public double Weight { get; set; }
        public double Volume { get; set; }
        public double SurfaceArea { get; set; }
        public double Length { get; set; }
    }
}
