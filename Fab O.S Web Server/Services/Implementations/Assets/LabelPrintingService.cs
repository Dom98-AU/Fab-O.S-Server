using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for Label generation using QuestPDF
/// </summary>
public class LabelPrintingService : ILabelPrintingService
{
    private readonly ApplicationDbContext _context;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<LabelPrintingService> _logger;

    private static readonly Dictionary<string, LabelTemplateDto> Templates = new()
    {
        ["Standard"] = new LabelTemplateDto
        {
            Name = "Standard",
            Description = "Standard equipment label (50mm x 25mm)",
            WidthMm = 50,
            HeightMm = 25,
            SupportsQRCode = true,
            SupportsBarcode = true,
            MaxCustomFields = 2
        },
        ["Large"] = new LabelTemplateDto
        {
            Name = "Large",
            Description = "Large equipment label (100mm x 50mm)",
            WidthMm = 100,
            HeightMm = 50,
            SupportsQRCode = true,
            SupportsBarcode = true,
            MaxCustomFields = 4
        },
        ["Small"] = new LabelTemplateDto
        {
            Name = "Small",
            Description = "Small equipment label (30mm x 15mm)",
            WidthMm = 30,
            HeightMm = 15,
            SupportsQRCode = true,
            SupportsBarcode = false,
            MaxCustomFields = 0
        },
        ["Inventory"] = new LabelTemplateDto
        {
            Name = "Inventory",
            Description = "Inventory tag label (75mm x 35mm)",
            WidthMm = 75,
            HeightMm = 35,
            SupportsQRCode = true,
            SupportsBarcode = true,
            MaxCustomFields = 2
        }
    };

    static LabelPrintingService()
    {
        // Set QuestPDF license (Community is free for small businesses)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public LabelPrintingService(
        ApplicationDbContext context,
        IQRCodeService qrCodeService,
        ILogger<LabelPrintingService> logger)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public async Task<LabelGenerationResult> GenerateLabelAsync(int equipmentId, string template = "Standard", LabelOptionsDto? options = null)
    {
        return await GenerateBatchLabelsAsync(new[] { equipmentId }, template, options);
    }

    public async Task<LabelGenerationResult> GenerateBatchLabelsAsync(IEnumerable<int> equipmentIds, string template = "Standard", LabelOptionsDto? options = null)
    {
        try
        {
            var idList = equipmentIds.ToList();
            var equipment = await _context.Equipment
                .Include(e => e.EquipmentCategory)
                .Include(e => e.EquipmentType)
                .Include(e => e.Location)
                .Where(e => idList.Contains(e.Id))
                .ToListAsync();

            if (!equipment.Any())
            {
                return new LabelGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No equipment found for the specified IDs"
                };
            }

            var templateInfo = GetTemplate(template) ?? Templates["Standard"];
            options ??= GetDefaultOptions(template);

            var document = Document.Create(container =>
            {
                foreach (var eq in equipment)
                {
                    container.Page(page =>
                    {
                        page.Size((float)templateInfo.WidthMm, (float)templateInfo.HeightMm, Unit.Millimetre);
                        page.Margin(2, Unit.Millimetre);

                        page.Content().Row(row =>
                        {
                            // QR Code section (if enabled)
                            if (options.IncludeQRCode && templateInfo.SupportsQRCode && !string.IsNullOrEmpty(eq.QRCodeData))
                            {
                                row.ConstantItem((float)(templateInfo.HeightMm - 4), Unit.Millimetre)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Image(Convert.FromBase64String(eq.QRCodeData.Replace("data:image/png;base64,", "")));
                            }

                            // Text section
                            row.RelativeItem().PaddingLeft(2, Unit.Millimetre).Column(col =>
                            {
                                // Equipment code and name
                                col.Item().Text(eq.EquipmentCode)
                                    .FontSize(template == "Small" ? 6 : 8)
                                    .Bold();

                                col.Item().Text(eq.Name)
                                    .FontSize(template == "Small" ? 5 : 7);

                                if (template != "Small")
                                {
                                    // Category
                                    if (options.IncludeCategory && eq.EquipmentCategory != null)
                                    {
                                        col.Item().Text(eq.EquipmentCategory.Name)
                                            .FontSize(6)
                                            .FontColor(Colors.Grey.Darken1);
                                    }

                                    // Location
                                    var locationName = eq.Location?.Name ?? eq.LocationLegacy;
                                    if (options.IncludeLocation && !string.IsNullOrEmpty(locationName))
                                    {
                                        col.Item().Text($"Loc: {locationName}")
                                            .FontSize(6);
                                    }

                                    // Serial number
                                    if (options.IncludeSerialNumber && !string.IsNullOrEmpty(eq.SerialNumber))
                                    {
                                        col.Item().Text($"S/N: {eq.SerialNumber}")
                                            .FontSize(5);
                                    }

                                    // Next service date
                                    if (options.IncludeNextServiceDate && eq.NextMaintenanceDate.HasValue)
                                    {
                                        col.Item().Text($"Service: {eq.NextMaintenanceDate.Value:dd/MM/yyyy}")
                                            .FontSize(5)
                                            .FontColor(eq.NextMaintenanceDate.Value < DateTime.Now ?
                                                Colors.Red.Medium : Colors.Green.Medium);
                                    }
                                }
                            });
                        });
                    });
                }
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            var pdfBytes = stream.ToArray();

            return new LabelGenerationResult
            {
                Success = true,
                PdfBase64 = Convert.ToBase64String(pdfBytes),
                FileName = $"equipment_labels_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                LabelCount = equipment.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating labels");
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating labels: {ex.Message}"
            };
        }
    }

    public IEnumerable<LabelTemplateDto> GetAvailableTemplates()
    {
        return Templates.Values;
    }

    public LabelTemplateDto? GetTemplate(string templateName)
    {
        return Templates.TryGetValue(templateName, out var template) ? template : null;
    }

    public bool ValidateOptions(string templateName, LabelOptionsDto options)
    {
        var template = GetTemplate(templateName);
        if (template == null)
            return false;

        // Check if barcode is requested but not supported
        if (options.IncludeBarcode && !template.SupportsBarcode)
            return false;

        return true;
    }

    public LabelOptionsDto GetDefaultOptions(string templateName)
    {
        return templateName switch
        {
            "Small" => new LabelOptionsDto
            {
                IncludeQRCode = true,
                IncludeCategory = false,
                IncludeLocation = false,
                IncludeNextServiceDate = false,
                IncludeSerialNumber = false,
                IncludeBarcode = false
            },
            "Large" => new LabelOptionsDto
            {
                IncludeQRCode = true,
                IncludeCategory = true,
                IncludeLocation = true,
                IncludeNextServiceDate = true,
                IncludeSerialNumber = true,
                IncludeBarcode = false
            },
            _ => new LabelOptionsDto
            {
                IncludeQRCode = true,
                IncludeCategory = true,
                IncludeLocation = true,
                IncludeNextServiceDate = true,
                IncludeSerialNumber = false,
                IncludeBarcode = false
            }
        };
    }

    public async Task<LabelGenerationResult> GenerateLabelsByCategoryAsync(int categoryId, int companyId, string template = "Standard", LabelOptionsDto? options = null)
    {
        var equipmentIds = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.CategoryId == categoryId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync();

        if (!equipmentIds.Any())
        {
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = "No equipment found in the specified category"
            };
        }

        return await GenerateBatchLabelsAsync(equipmentIds, template, options);
    }

    public async Task<LabelGenerationResult> GenerateLabelsByLocationAsync(string location, int companyId, string template = "Standard", LabelOptionsDto? options = null)
    {
        var equipmentIds = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.LocationLegacy == location && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync();

        if (!equipmentIds.Any())
        {
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = "No equipment found in the specified location"
            };
        }

        return await GenerateBatchLabelsAsync(equipmentIds, template, options);
    }

    public async Task<LabelGenerationResult> GenerateMaintenanceDueLabelsAsync(int companyId, int daysAhead = 7, string template = "Standard", LabelOptionsDto? options = null)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        var equipmentIds = await _context.Equipment
            .Where(e => e.CompanyId == companyId &&
                        e.NextMaintenanceDate != null &&
                        e.NextMaintenanceDate <= cutoffDate &&
                        !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync();

        if (!equipmentIds.Any())
        {
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = "No equipment with upcoming maintenance found"
            };
        }

        return await GenerateBatchLabelsAsync(equipmentIds, template, options);
    }

    #region Template-Based Label Generation

    /// <summary>
    /// Generates labels for equipment using a database template
    /// </summary>
    public async Task<LabelGenerationResult> GenerateEquipmentLabelsAsync(
        int companyId,
        IEnumerable<int> equipmentIds,
        LabelTemplate template)
    {
        try
        {
            var idList = equipmentIds.ToList();
            var equipment = await _context.Equipment
                .Include(e => e.EquipmentCategory)
                .Include(e => e.EquipmentType)
                .Include(e => e.Location)
                .Where(e => e.CompanyId == companyId && idList.Contains(e.Id) && !e.IsDeleted)
                .ToListAsync();

            if (!equipment.Any())
            {
                return new LabelGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No equipment found for the specified IDs"
                };
            }

            var document = Document.Create(container =>
            {
                foreach (var eq in equipment)
                {
                    container.Page(page =>
                    {
                        page.Size((float)template.WidthMm, (float)template.HeightMm, Unit.Millimetre);
                        page.Margin((float)template.MarginMm, Unit.Millimetre);

                        page.Content().Row(row =>
                        {
                            // QR Code section (if enabled)
                            if (template.IncludeQRCode && !string.IsNullOrEmpty(eq.QRCodeData))
                            {
                                var qrSize = (float)(template.HeightMm - (template.MarginMm * 2));
                                row.ConstantItem(qrSize, Unit.Millimetre)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Image(Convert.FromBase64String(eq.QRCodeData.Replace("data:image/png;base64,", "")));
                            }

                            // Text section
                            row.RelativeItem().PaddingLeft(2, Unit.Millimetre).Column(col =>
                            {
                                // Equipment code
                                if (template.IncludeCode)
                                {
                                    col.Item().Text(eq.EquipmentCode)
                                        .FontSize(template.PrimaryFontSize)
                                        .Bold();
                                }

                                // Equipment name
                                if (template.IncludeName)
                                {
                                    col.Item().Text(eq.Name)
                                        .FontSize(template.PrimaryFontSize - 1);
                                }

                                // Category
                                if (template.IncludeCategory && eq.EquipmentCategory != null)
                                {
                                    col.Item().Text(eq.EquipmentCategory.Name)
                                        .FontSize(template.SecondaryFontSize)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                // Location
                                var locationName = eq.Location?.Name ?? eq.LocationLegacy;
                                if (template.IncludeLocation && !string.IsNullOrEmpty(locationName))
                                {
                                    col.Item().Text($"Loc: {locationName}")
                                        .FontSize(template.SecondaryFontSize);
                                }

                                // Serial number
                                if (template.IncludeSerialNumber && !string.IsNullOrEmpty(eq.SerialNumber))
                                {
                                    col.Item().Text($"S/N: {eq.SerialNumber}")
                                        .FontSize(template.SecondaryFontSize - 1);
                                }

                                // Next service date
                                if (template.IncludeServiceDate && eq.NextMaintenanceDate.HasValue)
                                {
                                    col.Item().Text($"Service: {eq.NextMaintenanceDate.Value:dd/MM/yyyy}")
                                        .FontSize(template.SecondaryFontSize - 1)
                                        .FontColor(eq.NextMaintenanceDate.Value < DateTime.Now ?
                                            Colors.Red.Medium : Colors.Green.Medium);
                                }
                            });
                        });
                    });
                }
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            var pdfBytes = stream.ToArray();

            return new LabelGenerationResult
            {
                Success = true,
                PdfBase64 = Convert.ToBase64String(pdfBytes),
                FileName = $"equipment_labels_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                LabelCount = equipment.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating equipment labels with template {TemplateName}", template.Name);
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating labels: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Generates labels for equipment kits using a database template
    /// </summary>
    public async Task<LabelGenerationResult> GenerateKitLabelsAsync(
        int companyId,
        IEnumerable<int> kitIds,
        LabelTemplate template)
    {
        try
        {
            var idList = kitIds.ToList();
            var kits = await _context.EquipmentKits
                .Include(k => k.KitTemplate)
                .Include(k => k.Location)
                .Include(k => k.KitItems)
                    .ThenInclude(ki => ki.Equipment)
                .Where(k => k.CompanyId == companyId && idList.Contains(k.Id) && !k.IsDeleted)
                .ToListAsync();

            if (!kits.Any())
            {
                return new LabelGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No kits found for the specified IDs"
                };
            }

            var document = Document.Create(container =>
            {
                foreach (var kit in kits)
                {
                    container.Page(page =>
                    {
                        page.Size((float)template.WidthMm, (float)template.HeightMm, Unit.Millimetre);
                        page.Margin((float)template.MarginMm, Unit.Millimetre);

                        page.Content().Row(row =>
                        {
                            // QR Code section (if enabled)
                            if (template.IncludeQRCode && !string.IsNullOrEmpty(kit.QRCodeData))
                            {
                                var qrSize = (float)(template.HeightMm - (template.MarginMm * 2));
                                row.ConstantItem(qrSize, Unit.Millimetre)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Image(Convert.FromBase64String(kit.QRCodeData.Replace("data:image/png;base64,", "")));
                            }

                            // Text section
                            row.RelativeItem().PaddingLeft(2, Unit.Millimetre).Column(col =>
                            {
                                // Kit code
                                if (template.IncludeCode)
                                {
                                    col.Item().Text(kit.KitCode)
                                        .FontSize(template.PrimaryFontSize)
                                        .Bold();
                                }

                                // Kit name
                                if (template.IncludeName)
                                {
                                    col.Item().Text(kit.Name)
                                        .FontSize(template.PrimaryFontSize - 1);
                                }

                                // Category (template name for kits)
                                if (template.IncludeCategory && kit.KitTemplate != null)
                                {
                                    col.Item().Text(kit.KitTemplate.Name)
                                        .FontSize(template.SecondaryFontSize)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                // Location
                                var locationName = kit.Location?.Name ?? kit.LocationLegacy;
                                if (template.IncludeLocation && !string.IsNullOrEmpty(locationName))
                                {
                                    col.Item().Text($"Loc: {locationName}")
                                        .FontSize(template.SecondaryFontSize);
                                }

                                // Item count (useful for kits)
                                var itemCount = kit.KitItems?.Count ?? 0;
                                if (itemCount > 0)
                                {
                                    col.Item().Text($"Items: {itemCount}")
                                        .FontSize(template.SecondaryFontSize - 1);
                                }

                                // Status indicator for kits with maintenance flag
                                if (kit.HasMaintenanceFlag)
                                {
                                    col.Item().Text("⚠ Maintenance Required")
                                        .FontSize(template.SecondaryFontSize - 1)
                                        .FontColor(Colors.Orange.Medium);
                                }
                            });
                        });
                    });
                }
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            var pdfBytes = stream.ToArray();

            return new LabelGenerationResult
            {
                Success = true,
                PdfBase64 = Convert.ToBase64String(pdfBytes),
                FileName = $"kit_labels_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                LabelCount = kits.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating kit labels with template {TemplateName}", template.Name);
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating labels: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Generates labels for locations using a database template
    /// </summary>
    public async Task<LabelGenerationResult> GenerateLocationLabelsAsync(
        int companyId,
        IEnumerable<int> locationIds,
        LabelTemplate template)
    {
        try
        {
            var idList = locationIds.ToList();
            var locations = await _context.Locations
                .Where(l => l.CompanyId == companyId && idList.Contains(l.Id) && !l.IsDeleted)
                .ToListAsync();

            if (!locations.Any())
            {
                return new LabelGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No locations found for the specified IDs"
                };
            }

            var document = Document.Create(container =>
            {
                foreach (var location in locations)
                {
                    container.Page(page =>
                    {
                        page.Size((float)template.WidthMm, (float)template.HeightMm, Unit.Millimetre);
                        page.Margin((float)template.MarginMm, Unit.Millimetre);

                        page.Content().Row(row =>
                        {
                            // QR Code section (if enabled - generate from location code)
                            if (template.IncludeQRCode)
                            {
                                var qrSize = (float)(template.HeightMm - (template.MarginMm * 2));
                                var qrContent = $"LOC:{location.LocationCode}";
                                var qrImageBytes = _qrCodeService.GenerateQRCodeBytes(qrContent, template.QRCodePixelsPerModule);

                                row.ConstantItem(qrSize, Unit.Millimetre)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Image(qrImageBytes);
                            }

                            // Text section
                            row.RelativeItem().PaddingLeft(2, Unit.Millimetre).Column(col =>
                            {
                                // Location code
                                if (template.IncludeCode)
                                {
                                    col.Item().Text(location.LocationCode)
                                        .FontSize(template.PrimaryFontSize)
                                        .Bold();
                                }

                                // Location name
                                if (template.IncludeName)
                                {
                                    col.Item().Text(location.Name)
                                        .FontSize(template.PrimaryFontSize - 1);
                                }

                                // Location type (maps to IncludeCategory)
                                if (template.IncludeCategory)
                                {
                                    col.Item().Text(location.Type.ToString())
                                        .FontSize(template.SecondaryFontSize)
                                        .FontColor(Colors.Grey.Darken1);
                                }

                                // Address (maps to IncludeLocation for locations)
                                if (template.IncludeLocation && !string.IsNullOrEmpty(location.Address))
                                {
                                    col.Item().Text(location.Address)
                                        .FontSize(template.SecondaryFontSize);
                                }

                                // Contact info
                                if (template.IncludeContactInfo && !string.IsNullOrEmpty(location.ContactName))
                                {
                                    var contactInfo = location.ContactName;
                                    if (!string.IsNullOrEmpty(location.ContactPhone))
                                    {
                                        contactInfo += $" - {location.ContactPhone}";
                                    }
                                    col.Item().Text(contactInfo)
                                        .FontSize(template.SecondaryFontSize - 1);
                                }
                            });
                        });
                    });
                }
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            var pdfBytes = stream.ToArray();

            return new LabelGenerationResult
            {
                Success = true,
                PdfBase64 = Convert.ToBase64String(pdfBytes),
                FileName = $"location_labels_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
                LabelCount = locations.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating location labels with template {TemplateName}", template.Name);
            return new LabelGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating labels: {ex.Message}"
            };
        }
    }

    #endregion
}
