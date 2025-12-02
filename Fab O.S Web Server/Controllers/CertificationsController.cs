using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FabOS.WebServer.Models.DTOs.Assets;
using FabOS.WebServer.Models.Entities.Assets;
using FabOS.WebServer.Services.Interfaces;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for Equipment Certifications in the Asset module
/// </summary>
[ApiController]
[Route("api/assets/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class CertificationsController : ControllerBase
{
    private readonly ICertificationService _certificationService;
    private readonly ILogger<CertificationsController> _logger;

    public CertificationsController(
        ICertificationService certificationService,
        ILogger<CertificationsController> logger)
    {
        _certificationService = certificationService;
        _logger = logger;
    }

    #region CRUD Operations

    /// <summary>
    /// Get all certifications
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CertificationListResponse>> GetCertifications(
        [FromQuery] int companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? equipmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        try
        {
            var certifications = await _certificationService.GetCertificationsPagedAsync(
                companyId, page, pageSize, equipmentId, status, type);
            var totalCount = await _certificationService.GetCertificationsCountAsync(
                companyId, equipmentId, status, type);

            var items = certifications.Select(c => MapToDto(c)).ToList();

            return Ok(new CertificationListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certifications");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    /// <summary>
    /// Get certifications for equipment
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}")]
    public async Task<ActionResult<List<CertificationDto>>> GetByEquipment(int equipmentId)
    {
        try
        {
            var certifications = await _certificationService.GetCertificationsByEquipmentAsync(equipmentId);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certifications for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    /// <summary>
    /// Get certification by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CertificationDto>> GetCertification(int id)
    {
        try
        {
            var certification = await _certificationService.GetCertificationByIdAsync(id);
            if (certification == null)
                return NotFound(new { message = $"Certification with ID {id} not found" });

            return Ok(MapToDto(certification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certification {Id}", id);
            return StatusCode(500, new { message = "Error retrieving certification" });
        }
    }

    /// <summary>
    /// Create certification
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CertificationDto>> CreateCertification([FromBody] CreateCertificationRequest request)
    {
        try
        {
            var certification = new EquipmentCertification
            {
                EquipmentId = request.EquipmentId,
                CertificationType = request.CertificationType,
                CertificateNumber = request.CertificateNumber,
                IssuingAuthority = request.IssuingAuthority,
                IssueDate = request.IssueDate,
                ExpiryDate = request.ExpiryDate,
                DocumentUrl = request.DocumentUrl,
                Notes = request.Notes
            };

            var created = await _certificationService.CreateCertificationAsync(certification);

            return CreatedAtAction(nameof(GetCertification), new { id = created.Id }, MapToDto(created));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating certification");
            return StatusCode(500, new { message = "Error creating certification" });
        }
    }

    /// <summary>
    /// Update certification
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CertificationDto>> UpdateCertification(
        int id,
        [FromBody] UpdateCertificationRequest request)
    {
        try
        {
            var existing = await _certificationService.GetCertificationByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Certification with ID {id} not found" });

            existing.CertificationType = request.CertificationType;
            existing.CertificateNumber = request.CertificateNumber;
            existing.IssuingAuthority = request.IssuingAuthority;
            existing.IssueDate = request.IssueDate;
            existing.ExpiryDate = request.ExpiryDate;
            existing.DocumentUrl = request.DocumentUrl;
            existing.Notes = request.Notes;

            var updated = await _certificationService.UpdateCertificationAsync(existing);

            return Ok(MapToDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating certification {Id}", id);
            return StatusCode(500, new { message = "Error updating certification" });
        }
    }

    /// <summary>
    /// Delete certification
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteCertification(int id)
    {
        try
        {
            var result = await _certificationService.DeleteCertificationAsync(id);
            if (!result)
                return NotFound(new { message = $"Certification with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting certification {Id}", id);
            return StatusCode(500, new { message = "Error deleting certification" });
        }
    }

    #endregion

    #region Status Operations

    /// <summary>
    /// Get valid certifications
    /// </summary>
    [HttpGet("valid")]
    public async Task<ActionResult<List<CertificationDto>>> GetValidCertifications([FromQuery] int companyId)
    {
        try
        {
            var certifications = await _certificationService.GetValidCertificationsAsync(companyId);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid certifications");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    /// <summary>
    /// Get expired certifications
    /// </summary>
    [HttpGet("expired")]
    public async Task<ActionResult<List<CertificationDto>>> GetExpiredCertifications([FromQuery] int companyId)
    {
        try
        {
            var certifications = await _certificationService.GetExpiredCertificationsAsync(companyId);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired certifications");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    /// <summary>
    /// Get expiring certifications
    /// </summary>
    [HttpGet("expiring")]
    public async Task<ActionResult<List<CertificationDto>>> GetExpiringCertifications(
        [FromQuery] int companyId,
        [FromQuery] int daysAhead = 30)
    {
        try
        {
            var certifications = await _certificationService.GetExpiringCertificationsAsync(companyId, daysAhead);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring certifications");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    /// <summary>
    /// Get certifications by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<CertificationDto>>> GetByStatus(
        [FromQuery] int companyId,
        string status)
    {
        try
        {
            var certifications = await _certificationService.GetCertificationsByStatusAsync(companyId, status);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certifications by status");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    #endregion

    #region Renewal and Revocation

    /// <summary>
    /// Renew certification
    /// </summary>
    [HttpPost("{id:int}/renew")]
    public async Task<ActionResult<CertificationDto>> RenewCertification(
        int id,
        [FromBody] RenewCertificationRequest request)
    {
        try
        {
            var renewed = await _certificationService.RenewCertificationAsync(
                id,
                request.NewIssueDate,
                request.NewExpiryDate,
                request.NewCertificateNumber,
                request.DocumentUrl);

            return Ok(MapToDto(renewed));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing certification {Id}", id);
            return StatusCode(500, new { message = "Error renewing certification" });
        }
    }

    /// <summary>
    /// Revoke certification
    /// </summary>
    [HttpPost("{id:int}/revoke")]
    public async Task<ActionResult> RevokeCertification(int id, [FromBody] string reason)
    {
        try
        {
            var result = await _certificationService.RevokeCertificationAsync(id, reason);
            if (!result)
                return NotFound(new { message = $"Certification with ID {id} not found" });

            return Ok(new { message = "Certification revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking certification {Id}", id);
            return StatusCode(500, new { message = "Error revoking certification" });
        }
    }

    #endregion

    #region Type Operations

    /// <summary>
    /// Get certification types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<List<string>>> GetCertificationTypes([FromQuery] int companyId)
    {
        try
        {
            var types = await _certificationService.GetCertificationTypesAsync(companyId);
            return Ok(types.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certification types");
            return StatusCode(500, new { message = "Error retrieving types" });
        }
    }

    /// <summary>
    /// Get certifications by type
    /// </summary>
    [HttpGet("type/{certificationType}")]
    public async Task<ActionResult<List<CertificationDto>>> GetByType(
        [FromQuery] int companyId,
        string certificationType)
    {
        try
        {
            var certifications = await _certificationService.GetCertificationsByTypeAsync(companyId, certificationType);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certifications by type");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    #endregion

    #region Compliance

    /// <summary>
    /// Check if equipment is compliant
    /// </summary>
    [HttpGet("equipment/{equipmentId:int}/compliant")]
    public async Task<ActionResult<bool>> IsEquipmentCompliant(int equipmentId)
    {
        try
        {
            var isCompliant = await _certificationService.IsEquipmentFullyCompliantAsync(equipmentId);
            return Ok(new { equipmentId, isCompliant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking compliance for equipment {EquipmentId}", equipmentId);
            return StatusCode(500, new { message = "Error checking compliance" });
        }
    }

    /// <summary>
    /// Get non-compliant equipment
    /// </summary>
    [HttpGet("non-compliant")]
    public async Task<ActionResult<List<object>>> GetNonCompliantEquipment([FromQuery] int companyId)
    {
        try
        {
            var equipment = await _certificationService.GetNonCompliantEquipmentAsync(companyId);
            return Ok(equipment.Select(e => new
            {
                e.Id,
                e.EquipmentCode,
                e.Name,
                Category = e.EquipmentCategory?.Name,
                Type = e.EquipmentType?.Name,
                e.Location
            }).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting non-compliant equipment");
            return StatusCode(500, new { message = "Error retrieving equipment" });
        }
    }

    #endregion

    #region Analytics

    /// <summary>
    /// Get certification counts by status
    /// </summary>
    [HttpGet("analytics/by-status")]
    public async Task<ActionResult<Dictionary<string, int>>> GetCountByStatus([FromQuery] int companyId)
    {
        try
        {
            var counts = await _certificationService.GetCertificationCountByStatusAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certification counts by status");
            return StatusCode(500, new { message = "Error retrieving counts" });
        }
    }

    /// <summary>
    /// Get certification counts by type
    /// </summary>
    [HttpGet("analytics/by-type")]
    public async Task<ActionResult<Dictionary<string, int>>> GetCountByType([FromQuery] int companyId)
    {
        try
        {
            var counts = await _certificationService.GetCertificationCountByTypeAsync(companyId);
            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certification counts by type");
            return StatusCode(500, new { message = "Error retrieving counts" });
        }
    }

    /// <summary>
    /// Get recent certifications
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<List<CertificationDto>>> GetRecentCertifications(
        [FromQuery] int companyId,
        [FromQuery] int count = 10)
    {
        try
        {
            var certifications = await _certificationService.GetRecentCertificationsAsync(companyId, count);
            return Ok(certifications.Select(c => MapToDto(c)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent certifications");
            return StatusCode(500, new { message = "Error retrieving certifications" });
        }
    }

    #endregion

    #region Helper Methods

    private static CertificationDto MapToDto(EquipmentCertification c)
    {
        return new CertificationDto
        {
            Id = c.Id,
            EquipmentId = c.EquipmentId,
            EquipmentCode = c.Equipment?.EquipmentCode,
            EquipmentName = c.Equipment?.Name,
            CertificationType = c.CertificationType,
            CertificateNumber = c.CertificateNumber,
            IssuingAuthority = c.IssuingAuthority,
            IssueDate = c.IssueDate,
            ExpiryDate = c.ExpiryDate,
            DocumentUrl = c.DocumentUrl,
            Status = c.Status,
            Notes = c.Notes,
            CreatedDate = c.CreatedDate,
            LastModified = c.LastModified
        };
    }

    #endregion
}
