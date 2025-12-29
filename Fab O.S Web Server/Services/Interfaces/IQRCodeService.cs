using FabOS.WebServer.Models.DTOs.Assets;

namespace FabOS.WebServer.Services.Interfaces;

/// <summary>
/// Service interface for QR Code generation
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// Generates a QR code for an equipment item
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <returns>QR code generation result with base64 data URL</returns>
    Task<QRCodeGenerationResult> GenerateEquipmentQRCodeAsync(int equipmentId);

    /// <summary>
    /// Generates a QR code from equipment data
    /// </summary>
    /// <param name="data">The equipment QR data</param>
    /// <returns>QR code generation result with base64 data URL</returns>
    QRCodeGenerationResult GenerateQRCode(EquipmentQRData data);

    /// <summary>
    /// Generates a unique QR code identifier for an equipment item
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <param name="equipmentCode">The equipment code</param>
    /// <returns>A unique identifier string</returns>
    string GenerateQRCodeIdentifier(int equipmentId, string equipmentCode);

    /// <summary>
    /// Validates a QR code identifier format
    /// </summary>
    /// <param name="identifier">The identifier to validate</param>
    /// <returns>True if valid format</returns>
    bool ValidateQRCodeIdentifier(string identifier);

    /// <summary>
    /// Regenerates QR code for equipment (updates stored QR data)
    /// </summary>
    /// <param name="equipmentId">The equipment ID</param>
    /// <returns>Updated QR code data</returns>
    Task<QRCodeGenerationResult> RegenerateEquipmentQRCodeAsync(int equipmentId);

    /// <summary>
    /// Gets QR code size options
    /// </summary>
    /// <returns>Available QR code sizes</returns>
    Dictionary<string, int> GetQRCodeSizeOptions();

    /// <summary>
    /// Generates QR code with custom size
    /// </summary>
    /// <param name="data">The equipment QR data</param>
    /// <param name="pixelsPerModule">Pixels per QR module</param>
    /// <returns>QR code generation result</returns>
    QRCodeGenerationResult GenerateQRCodeWithSize(EquipmentQRData data, int pixelsPerModule);

    /// <summary>
    /// Generates QR code bytes directly from a string content
    /// </summary>
    /// <param name="content">The content to encode in the QR code</param>
    /// <param name="pixelsPerModule">Pixels per QR module (default 10)</param>
    /// <returns>PNG image bytes of the QR code</returns>
    byte[] GenerateQRCodeBytes(string content, int pixelsPerModule = 10);
}
