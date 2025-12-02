using System.Text.Json;
using QRCoder;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FabOS.WebServer.Services.Implementations.Assets;

/// <summary>
/// Service implementation for QR Code generation using QRCoder library
/// </summary>
public class QRCodeService : IQRCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QRCodeService> _logger;

    private static readonly Dictionary<string, int> QRCodeSizes = new()
    {
        { "Small", 5 },
        { "Medium", 10 },
        { "Large", 15 },
        { "ExtraLarge", 20 }
    };

    public QRCodeService(ApplicationDbContext context, ILogger<QRCodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QRCodeGenerationResult> GenerateEquipmentQRCodeAsync(int equipmentId)
    {
        try
        {
            var equipment = await _context.Equipment
                .Include(e => e.EquipmentCategory)
                .Include(e => e.EquipmentType)
                .FirstOrDefaultAsync(e => e.Id == equipmentId);

            if (equipment == null)
            {
                return new QRCodeGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Equipment with ID {equipmentId} not found"
                };
            }

            var qrData = new EquipmentQRData
            {
                Id = equipment.Id,
                Code = equipment.EquipmentCode,
                Name = equipment.Name,
                Category = equipment.EquipmentCategory?.Name,
                Type = equipment.EquipmentType?.Name,
                Location = equipment.Location?.Name ?? equipment.LocationLegacy,
                NextServiceDate = equipment.NextMaintenanceDate,
                SerialNumber = equipment.SerialNumber,
                GeneratedAt = DateTime.UtcNow.ToString("O")
            };

            var result = GenerateQRCode(qrData);

            if (result.Success)
            {
                // Update equipment with QR code data
                equipment.QRCodeData = result.QRCodeDataUrl;
                equipment.QRCodeIdentifier = GenerateQRCodeIdentifier(equipment.Id, equipment.EquipmentCode);
                result.QRCodeIdentifier = equipment.QRCodeIdentifier;
                await _context.SaveChangesAsync();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for equipment {EquipmentId}", equipmentId);
            return new QRCodeGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating QR code: {ex.Message}"
            };
        }
    }

    public QRCodeGenerationResult GenerateQRCode(EquipmentQRData data)
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(jsonData, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);
            var base64 = Convert.ToBase64String(qrCodeBytes);

            return new QRCodeGenerationResult
            {
                Success = true,
                QRCodeDataUrl = $"data:image/png;base64,{base64}",
                QRCodeIdentifier = GenerateQRCodeIdentifier(data.Id, data.Code)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return new QRCodeGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating QR code: {ex.Message}"
            };
        }
    }

    public QRCodeGenerationResult GenerateQRCodeWithSize(EquipmentQRData data, int pixelsPerModule)
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(jsonData, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
            var base64 = Convert.ToBase64String(qrCodeBytes);

            return new QRCodeGenerationResult
            {
                Success = true,
                QRCodeDataUrl = $"data:image/png;base64,{base64}",
                QRCodeIdentifier = GenerateQRCodeIdentifier(data.Id, data.Code)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code with size");
            return new QRCodeGenerationResult
            {
                Success = false,
                ErrorMessage = $"Error generating QR code: {ex.Message}"
            };
        }
    }

    public string GenerateQRCodeIdentifier(int equipmentId, string equipmentCode)
    {
        // Format: EQP-{ID}-{CODE}-{TIMESTAMP}
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"EQP-{equipmentId}-{equipmentCode}-{timestamp}";
    }

    public bool ValidateQRCodeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        // Should start with EQP- and have 4 parts
        var parts = identifier.Split('-');
        if (parts.Length < 4 || parts[0] != "EQP")
            return false;

        // Second part should be numeric (equipment ID)
        if (!int.TryParse(parts[1], out _))
            return false;

        return true;
    }

    public async Task<QRCodeGenerationResult> RegenerateEquipmentQRCodeAsync(int equipmentId)
    {
        _logger.LogInformation("Regenerating QR code for equipment {EquipmentId}", equipmentId);
        return await GenerateEquipmentQRCodeAsync(equipmentId);
    }

    public Dictionary<string, int> GetQRCodeSizeOptions()
    {
        return QRCodeSizes;
    }
}
