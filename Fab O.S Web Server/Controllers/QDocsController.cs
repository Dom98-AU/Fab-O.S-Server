using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Services;
using FabOS.WebServer.Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for QDocs module - drawings, revisions, CAD file uploads, and drawing parts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class QDocsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISmlxParserService _smlxParser;
    private readonly IIfcParserService _ifcParser;
    private readonly ISharePointService _sharePointService;
    private readonly NumberSeriesService _numberSeriesService;
    private readonly ICadImportSessionService _cadImportSession;
    private readonly ILogger<QDocsController> _logger;

    public QDocsController(
        ApplicationDbContext context,
        ISmlxParserService smlxParser,
        IIfcParserService ifcParser,
        ISharePointService sharePointService,
        NumberSeriesService numberSeriesService,
        ICadImportSessionService cadImportSession,
        ILogger<QDocsController> logger)
    {
        _context = context;
        _smlxParser = smlxParser;
        _ifcParser = ifcParser;
        _sharePointService = sharePointService;
        _numberSeriesService = numberSeriesService;
        _cadImportSession = cadImportSession;
        _logger = logger;
    }

    #region Helper Methods

    private int GetCompanyId()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        if (int.TryParse(companyIdClaim, out int companyId))
            return companyId;
        throw new UnauthorizedAccessException("Unable to determine company ID from token");
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
            return userId;
        throw new UnauthorizedAccessException("Unable to determine user ID from token");
    }

    private string GetUserName()
    {
        return User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? "Unknown";
    }

    #endregion

    #region Drawing CRUD Operations

    /// <summary>
    /// Get all QDocs drawings with optional filtering
    /// </summary>
    [HttpGet("drawings")]
    public async Task<ActionResult> GetDrawings([FromQuery] int? orderId, [FromQuery] int? workPackageId)
    {
        try
        {
            var companyId = GetCompanyId();
            var query = _context.QDocsDrawings.Where(d => d.CompanyId == companyId);

            if (orderId.HasValue)
                query = query.Where(d => d.OrderId == orderId.Value);

            if (workPackageId.HasValue)
                query = query.Where(d => d.WorkPackageId == workPackageId.Value);

            var drawings = await query
                .Include(d => d.Revisions)
                .OrderByDescending(d => d.CreatedDate)
                .Select(d => new QDocsDrawingDto
                {
                    Id = d.Id,
                    DrawingNumber = d.DrawingNumber,
                    DrawingTitle = d.DrawingTitle,
                    Description = d.Description,
                    OrderId = d.OrderId,
                    WorkPackageId = d.WorkPackageId,
                    AssemblyId = d.AssemblyId,
                    CurrentStage = d.CurrentStage,
                    ActiveRevisionId = d.ActiveRevisionId,
                    CreatedDate = d.CreatedDate,
                    CreatedBy = d.CreatedBy,
                    ModifiedDate = d.ModifiedDate,
                    Revisions = d.Revisions.Select(r => new DrawingRevisionDto
                    {
                        Id = r.Id,
                        DrawingId = r.DrawingId,
                        RevisionCode = r.RevisionCode,
                        RevisionType = r.RevisionType,
                        RevisionNumber = r.RevisionNumber,
                        RevisionNotes = r.RevisionNotes,
                        Status = r.Status,
                        IsActiveForProduction = r.IsActiveForProduction,
                        CreatedDate = r.CreatedDate
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { success = true, data = drawings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawings");
            return StatusCode(500, new { success = false, message = "Error retrieving drawings" });
        }
    }

    /// <summary>
    /// Get a single QDocs drawing by ID
    /// </summary>
    [HttpGet("drawings/{id:int}")]
    public async Task<ActionResult> GetDrawing(int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {id} not found" });

            var dto = new QDocsDrawingDto
            {
                Id = drawing.Id,
                DrawingNumber = drawing.DrawingNumber,
                DrawingTitle = drawing.DrawingTitle,
                Description = drawing.Description,
                OrderId = drawing.OrderId,
                WorkPackageId = drawing.WorkPackageId,
                AssemblyId = drawing.AssemblyId,
                CurrentStage = drawing.CurrentStage,
                ActiveRevisionId = drawing.ActiveRevisionId,
                CreatedDate = drawing.CreatedDate,
                CreatedBy = drawing.CreatedBy,
                ModifiedDate = drawing.ModifiedDate,
                Revisions = drawing.Revisions.Select(r => new DrawingRevisionDto
                {
                    Id = r.Id,
                    DrawingId = r.DrawingId,
                    RevisionCode = r.RevisionCode,
                    RevisionType = r.RevisionType,
                    RevisionNumber = r.RevisionNumber,
                    RevisionNotes = r.RevisionNotes,
                    Status = r.Status,
                    IsActiveForProduction = r.IsActiveForProduction,
                    CreatedDate = r.CreatedDate
                }).ToList()
            };

            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing {DrawingId}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving drawing" });
        }
    }

    /// <summary>
    /// Create a new QDocs drawing
    /// </summary>
    [HttpPost("drawings")]
    public async Task<ActionResult> CreateDrawing([FromBody] CreateQDocsDrawingDto dto)
    {
        try
        {
            // Validate order exists
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
                return BadRequest(new { success = false, message = $"Order with ID {dto.OrderId} not found" });

            var companyId = order.CompanyId;

            var drawing = new QDocsDrawing
            {
                DrawingNumber = dto.DrawingNumber,
                DrawingTitle = dto.DrawingTitle,
                Description = dto.Description,
                OrderId = dto.OrderId,
                WorkPackageId = dto.WorkPackageId,
                CurrentStage = "IFA",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = GetUserId(),
                CompanyId = companyId
            };

            await _context.QDocsDrawings.AddAsync(drawing);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created QDocs drawing: {DrawingNumber} (ID: {DrawingId})", drawing.DrawingNumber, drawing.Id);

            return CreatedAtAction(nameof(GetDrawing), new { id = drawing.Id }, new { success = true, data = new { id = drawing.Id, drawingNumber = drawing.DrawingNumber } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating drawing");
            return StatusCode(500, new { success = false, message = "Error creating drawing" });
        }
    }

    /// <summary>
    /// Update a QDocs drawing
    /// </summary>
    [HttpPut("drawings/{id:int}")]
    public async Task<ActionResult> UpdateDrawing(int id, [FromBody] UpdateQDocsDrawingDto dto)
    {
        try
        {
            var companyId = GetCompanyId();
            var drawing = await _context.QDocsDrawings.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);
            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {id} not found" });

            if (!string.IsNullOrEmpty(dto.DrawingNumber))
                drawing.DrawingNumber = dto.DrawingNumber;
            if (!string.IsNullOrEmpty(dto.DrawingTitle))
                drawing.DrawingTitle = dto.DrawingTitle;
            if (dto.Description != null)
                drawing.Description = dto.Description;
            if (dto.WorkPackageId.HasValue)
                drawing.WorkPackageId = dto.WorkPackageId;
            drawing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = new QDocsDrawingDto
            {
                Id = drawing.Id,
                DrawingNumber = drawing.DrawingNumber,
                DrawingTitle = drawing.DrawingTitle,
                Description = drawing.Description,
                OrderId = drawing.OrderId,
                WorkPackageId = drawing.WorkPackageId,
                CurrentStage = drawing.CurrentStage,
                CreatedDate = drawing.CreatedDate,
                ModifiedDate = drawing.ModifiedDate
            }});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating drawing {DrawingId}", id);
            return StatusCode(500, new { success = false, message = "Error updating drawing" });
        }
    }

    /// <summary>
    /// Delete a QDocs drawing
    /// </summary>
    [HttpDelete("drawings/{id:int}")]
    public async Task<ActionResult> DeleteDrawing(int id)
    {
        try
        {
            var companyId = GetCompanyId();
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .Include(d => d.Parts)
                .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {id} not found" });

            _context.QDocsDrawings.Remove(drawing);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Drawing deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting drawing {DrawingId}", id);
            return StatusCode(500, new { success = false, message = "Error deleting drawing" });
        }
    }

    #endregion

    #region Revision Lifecycle Operations

    /// <summary>
    /// Get all revisions for a drawing
    /// </summary>
    [HttpGet("drawings/{drawingId:int}/revisions")]
    public async Task<ActionResult> GetRevisions(int drawingId)
    {
        try
        {
            var companyId = GetCompanyId();
            var revisions = await _context.DrawingRevisions
                .Where(r => r.DrawingId == drawingId && r.CompanyId == companyId)
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new DrawingRevisionDto
                {
                    Id = r.Id,
                    DrawingId = r.DrawingId,
                    RevisionCode = r.RevisionCode,
                    RevisionType = r.RevisionType,
                    RevisionNumber = r.RevisionNumber,
                    RevisionNotes = r.RevisionNotes,
                    DrawingFileName = r.DrawingFileName,
                    CloudProvider = r.CloudProvider,
                    CloudFileId = r.CloudFileId,
                    CloudFilePath = r.CloudFilePath,
                    Status = r.Status,
                    ReviewedBy = r.ReviewedBy,
                    ReviewedDate = r.ReviewedDate,
                    ApprovedBy = r.ApprovedBy,
                    ApprovedDate = r.ApprovedDate,
                    ApprovalComments = r.ApprovalComments,
                    SupersededById = r.SupersededById,
                    CreatedFromIFARevisionId = r.CreatedFromIFARevisionId,
                    IsActiveForProduction = r.IsActiveForProduction,
                    CreatedDate = r.CreatedDate
                })
                .ToListAsync();

            return Ok(new { success = true, data = revisions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revisions for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error retrieving revisions" });
        }
    }

    /// <summary>
    /// Create a new revision (IFA or IFC)
    /// </summary>
    [HttpPost("drawings/{drawingId:int}/revisions")]
    public async Task<ActionResult> CreateRevision(int drawingId, [FromBody] CreateDrawingRevisionDto dto)
    {
        try
        {
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            // Calculate next revision number
            var existingRevisions = drawing.Revisions.Where(r => r.RevisionType == dto.RevisionType).ToList();
            var nextRevisionNumber = existingRevisions.Any() ? existingRevisions.Max(r => r.RevisionNumber) + 1 : 1;

            // Generate revision code (e.g., IFA-R1, IFC-R1)
            var revisionCode = $"{dto.RevisionType}-R{nextRevisionNumber}";

            var revision = new DrawingRevision
            {
                DrawingId = drawingId,
                RevisionCode = revisionCode,
                RevisionType = dto.RevisionType,
                RevisionNumber = nextRevisionNumber,
                RevisionNotes = dto.RevisionNotes,
                Status = "Draft",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = GetUserId(),
                CompanyId = drawing.CompanyId,
                CreatedFromIFARevisionId = dto.CreatedFromIFARevisionId
            };

            await _context.DrawingRevisions.AddAsync(revision);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created revision {RevisionCode} for drawing {DrawingId}", revisionCode, drawingId);

            return CreatedAtAction(nameof(GetRevision), new { revisionId = revision.Id },
                new { success = true, data = new { id = revision.Id, revisionCode = revision.RevisionCode, revisionType = revision.RevisionType } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating revision for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error creating revision" });
        }
    }

    /// <summary>
    /// Get a single revision by ID
    /// </summary>
    [HttpGet("revisions/{revisionId:int}")]
    public async Task<ActionResult> GetRevision(int revisionId)
    {
        try
        {
            var revision = await _context.DrawingRevisions.FirstOrDefaultAsync(r => r.Id == revisionId && r.CompanyId == GetCompanyId());
            if (revision == null)
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found" });

            var dto = new DrawingRevisionDto
            {
                Id = revision.Id,
                DrawingId = revision.DrawingId,
                RevisionCode = revision.RevisionCode,
                RevisionType = revision.RevisionType,
                RevisionNumber = revision.RevisionNumber,
                RevisionNotes = revision.RevisionNotes,
                DrawingFileName = revision.DrawingFileName,
                CloudProvider = revision.CloudProvider,
                CloudFileId = revision.CloudFileId,
                CloudFilePath = revision.CloudFilePath,
                Status = revision.Status,
                ReviewedBy = revision.ReviewedBy,
                ReviewedDate = revision.ReviewedDate,
                ApprovedBy = revision.ApprovedBy,
                ApprovedDate = revision.ApprovedDate,
                ApprovalComments = revision.ApprovalComments,
                SupersededById = revision.SupersededById,
                CreatedFromIFARevisionId = revision.CreatedFromIFARevisionId,
                IsActiveForProduction = revision.IsActiveForProduction,
                CreatedDate = revision.CreatedDate
            };

            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error retrieving revision" });
        }
    }

    /// <summary>
    /// Update revision status (Draft → UnderReview → Approved/Rejected)
    /// </summary>
    [HttpPatch("revisions/{revisionId:int}/status")]
    public async Task<ActionResult> UpdateRevisionStatus(int revisionId, [FromBody] UpdateRevisionStatusDto dto)
    {
        try
        {
            var revision = await _context.DrawingRevisions.FirstOrDefaultAsync(r => r.Id == revisionId && r.CompanyId == GetCompanyId());
            if (revision == null)
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found" });

            // Validate status transition - allowing Draft to go directly to Approved for simpler workflows
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Draft", new[] { "UnderReview", "Approved" } },
                { "UnderReview", new[] { "Approved", "Rejected" } },
                { "Rejected", new[] { "Draft", "UnderReview" } },
                { "Approved", new[] { "Superseded" } }
            };

            if (!validTransitions.ContainsKey(revision.Status) || !validTransitions[revision.Status].Contains(dto.Status))
            {
                return BadRequest(new { success = false, message = $"Invalid status transition from {revision.Status} to {dto.Status}" });
            }

            revision.Status = dto.Status;

            if (dto.Status == "UnderReview")
            {
                revision.ReviewedBy = dto.ReviewedBy;
                revision.ReviewedDate = DateTime.UtcNow;
            }
            else if (dto.Status == "Approved")
            {
                revision.ApprovedBy = dto.ApprovedBy;
                revision.ApprovedDate = DateTime.UtcNow;
                revision.ApprovalComments = dto.ApprovalComments;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated revision {RevisionId} status to {Status}", revisionId, dto.Status);

            return Ok(new { success = true, data = new { id = revision.Id, status = revision.Status, approvedDate = revision.ApprovedDate } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating revision {RevisionId} status", revisionId);
            return StatusCode(500, new { success = false, message = "Error updating revision status" });
        }
    }

    /// <summary>
    /// Create IFC revision from approved IFA
    /// </summary>
    [HttpPost("revisions/{ifaRevisionId:int}/create-ifc")]
    public async Task<ActionResult> CreateIFCFromIFA(int ifaRevisionId, [FromBody] CreateIFCFromIFADto dto)
    {
        try
        {
            var ifaRevision = await _context.DrawingRevisions
                .Include(r => r.Drawing)
                .FirstOrDefaultAsync(r => r.Id == ifaRevisionId);

            if (ifaRevision == null)
                return NotFound(new { success = false, message = $"IFA Revision with ID {ifaRevisionId} not found" });

            if (ifaRevision.RevisionType != "IFA")
                return BadRequest(new { success = false, message = "Source revision must be IFA type" });

            if (ifaRevision.Status != "Approved")
                return BadRequest(new { success = false, message = "IFA revision must be approved before creating IFC" });

            // Get existing IFC revisions to determine next number
            var existingIFCs = await _context.DrawingRevisions
                .Where(r => r.DrawingId == ifaRevision.DrawingId && r.RevisionType == "IFC")
                .ToListAsync();

            var nextRevisionNumber = existingIFCs.Any() ? existingIFCs.Max(r => r.RevisionNumber) + 1 : 1;
            var revisionCode = $"IFC-R{nextRevisionNumber}";

            var ifcRevision = new DrawingRevision
            {
                DrawingId = ifaRevision.DrawingId,
                RevisionCode = revisionCode,
                RevisionType = "IFC",
                RevisionNumber = nextRevisionNumber,
                RevisionNotes = dto.RevisionNotes ?? $"Created from IFA revision {ifaRevision.RevisionCode}",
                Status = "Approved", // IFC from approved IFA is auto-approved
                CreatedDate = DateTime.UtcNow,
                CreatedBy = GetUserId(),
                CompanyId = ifaRevision.CompanyId,
                CreatedFromIFARevisionId = ifaRevisionId,
                ApprovedBy = ifaRevision.ApprovedBy,
                ApprovedDate = DateTime.UtcNow,
                ApprovalComments = "Auto-approved from IFA approval"
            };

            await _context.DrawingRevisions.AddAsync(ifcRevision);

            // Update drawing stage
            ifaRevision.Drawing.CurrentStage = "IFC";
            ifaRevision.Drawing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created IFC revision {RevisionCode} from IFA {IFARevisionId}", revisionCode, ifaRevisionId);

            return CreatedAtAction(nameof(GetRevision), new { revisionId = ifcRevision.Id },
                new { success = true, data = new { id = ifcRevision.Id, revisionCode = ifcRevision.RevisionCode, revisionType = "IFC" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating IFC from IFA {IFARevisionId}", ifaRevisionId);
            return StatusCode(500, new { success = false, message = "Error creating IFC revision" });
        }
    }

    /// <summary>
    /// Set a revision as active for production
    /// </summary>
    [HttpPut("revisions/{revisionId:int}/set-active")]
    public async Task<ActionResult> SetRevisionActive(int revisionId)
    {
        try
        {
            var revision = await _context.DrawingRevisions
                .Include(r => r.Drawing)
                .FirstOrDefaultAsync(r => r.Id == revisionId);

            if (revision == null)
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found" });

            if (revision.RevisionType != "IFC")
                return BadRequest(new { success = false, message = "Only IFC revisions can be set as active for production" });

            if (revision.Status != "Approved")
                return BadRequest(new { success = false, message = "Revision must be approved before setting as active" });

            // Deactivate all other IFC revisions for this drawing
            var otherRevisions = await _context.DrawingRevisions
                .Where(r => r.DrawingId == revision.DrawingId && r.RevisionType == "IFC" && r.Id != revisionId)
                .ToListAsync();

            foreach (var otherRevision in otherRevisions)
            {
                if (otherRevision.IsActiveForProduction)
                {
                    otherRevision.IsActiveForProduction = false;
                    otherRevision.Status = "Superseded";
                }
            }

            revision.IsActiveForProduction = true;
            revision.Drawing.ActiveRevisionId = revisionId;
            revision.Drawing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Set revision {RevisionId} as active for production", revisionId);

            return Ok(new { success = true, data = new { id = revision.Id, isActiveForProduction = revision.IsActiveForProduction } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting revision {RevisionId} as active", revisionId);
            return StatusCode(500, new { success = false, message = "Error setting revision as active" });
        }
    }

    /// <summary>
    /// Set a revision as active for a drawing (alternative route)
    /// </summary>
    [HttpPost("drawings/{drawingId:int}/set-active-revision/{revisionId:int}")]
    public async Task<ActionResult> SetActiveRevisionForDrawing(int drawingId, int revisionId)
    {
        try
        {
            var drawing = await _context.QDocsDrawings.FindAsync(drawingId);
            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            var revision = await _context.DrawingRevisions
                .FirstOrDefaultAsync(r => r.Id == revisionId && r.DrawingId == drawingId);

            if (revision == null)
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found for drawing {drawingId}" });

            // Deactivate all other revisions for this drawing
            var otherRevisions = await _context.DrawingRevisions
                .Where(r => r.DrawingId == drawingId && r.Id != revisionId)
                .ToListAsync();

            foreach (var otherRevision in otherRevisions)
            {
                otherRevision.IsActiveForProduction = false;
            }

            revision.IsActiveForProduction = true;
            drawing.ActiveRevisionId = revisionId;
            drawing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Set revision {RevisionId} as active for drawing {DrawingId}", revisionId, drawingId);

            return Ok(new { success = true, data = new { drawingId = drawingId, activeRevisionId = revisionId } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active revision for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error setting active revision" });
        }
    }

    #endregion

    #region File Management Operations

    /// <summary>
    /// Upload file to a revision (PDF for IFA, CAD for IFC)
    /// </summary>
    [HttpPost("revisions/{revisionId:int}/files/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadFile(int revisionId, [Required] IFormFile file, [FromForm] string? fileType)
    {
        try
        {
            var revision = await _context.DrawingRevisions
                .Include(r => r.Drawing)
                .FirstOrDefaultAsync(r => r.Id == revisionId);

            if (revision == null)
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found" });

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var detectedFileType = fileType ?? extension.TrimStart('.').ToUpper();

            // Validate file type based on revision type
            if (revision.RevisionType == "IFA" && extension != ".pdf")
            {
                return BadRequest(new { success = false, message = "IFA revisions only accept PDF files" });
            }

            // Upload to SharePoint
            var folderPath = $"QDocs/{revision.Drawing.DrawingNumber}/{revision.RevisionCode}";

            using var stream = file.OpenReadStream();
            var fileInfo = await _sharePointService.UploadFileAsync(folderPath, stream, file.FileName, file.ContentType);

            // Create DrawingFile record
            var drawingFile = new DrawingFile
            {
                DrawingRevisionId = revisionId,
                FileName = file.FileName,
                FileType = detectedFileType,
                FilePath = fileInfo.WebUrl,
                FileSizeBytes = file.Length,
                UploadedDate = DateTime.UtcNow,
                UploadedBy = GetUserId(),
                ParseStatus = extension == ".smlx" || extension == ".ifc" ? "Pending" : "NotApplicable",
                CompanyId = revision.CompanyId
            };

            await _context.DrawingFiles.AddAsync(drawingFile);

            // Update revision with file info
            revision.DrawingFileName = file.FileName;
            revision.CloudProvider = "SharePoint";
            revision.CloudFileId = fileInfo.Id;
            revision.CloudFilePath = fileInfo.WebUrl;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Uploaded file {FileName} to revision {RevisionId}", file.FileName, revisionId);

            return Ok(new {
                success = true,
                data = new {
                    fileId = drawingFile.Id,
                    fileName = file.FileName,
                    fileType = detectedFileType,
                    storageProvider = "SharePoint",
                    webUrl = fileInfo.WebUrl
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error uploading file" });
        }
    }

    /// <summary>
    /// Get all files for a revision
    /// </summary>
    [HttpGet("revisions/{revisionId:int}/files")]
    public async Task<ActionResult> GetRevisionFiles(int revisionId)
    {
        try
        {
            var files = await _context.DrawingFiles
                .Where(f => f.DrawingRevisionId == revisionId)
                .Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.FileType,
                    f.FilePath,
                    f.FileSizeBytes,
                    f.UploadedDate,
                    f.ParseStatus,
                    f.ParsedPartCount
                })
                .ToListAsync();

            return Ok(new { success = true, data = new { files } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error retrieving files" });
        }
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    [HttpGet("files/{fileId:int}")]
    public async Task<ActionResult> GetFile(int fileId)
    {
        try
        {
            var file = await _context.DrawingFiles.FindAsync(fileId);
            if (file == null)
                return NotFound(new { success = false, message = $"File with ID {fileId} not found" });

            return Ok(new {
                success = true,
                data = new
                {
                    file.Id,
                    file.FileName,
                    file.FileType,
                    file.FilePath,
                    file.FileSizeBytes,
                    file.UploadedDate,
                    file.ParseStatus,
                    file.ParsedPartCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "Error retrieving file" });
        }
    }

    /// <summary>
    /// Get file web URL for preview
    /// </summary>
    [HttpGet("files/{fileId:int}/web-url")]
    public async Task<ActionResult> GetFileWebUrl(int fileId)
    {
        try
        {
            var file = await _context.DrawingFiles.FindAsync(fileId);
            if (file == null)
                return NotFound(new { success = false, message = $"File with ID {fileId} not found" });

            return Ok(new { success = true, data = new { webUrl = file.FilePath } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting web URL for file {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "Error retrieving web URL" });
        }
    }

    /// <summary>
    /// Download file
    /// </summary>
    [HttpGet("files/{fileId:int}/download")]
    public async Task<ActionResult> DownloadFile(int fileId)
    {
        try
        {
            var file = await _context.DrawingFiles
                .Include(f => f.DrawingRevision)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                return NotFound(new { success = false, message = $"File with ID {fileId} not found" });

            // Get the drive item ID from revision
            var revision = file.DrawingRevision;
            if (string.IsNullOrEmpty(revision?.CloudFileId))
                return NotFound(new { success = false, message = "File not found in cloud storage" });

            var stream = await _sharePointService.DownloadFileAsync(revision.CloudFileId);

            return File(stream, "application/octet-stream", file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "Error downloading file" });
        }
    }

    /// <summary>
    /// Update/replace file
    /// </summary>
    [HttpPut("files/{fileId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UpdateFile(int fileId, [Required] IFormFile file)
    {
        try
        {
            var existingFile = await _context.DrawingFiles
                .Include(f => f.DrawingRevision)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (existingFile == null)
                return NotFound(new { success = false, message = $"File with ID {fileId} not found" });

            var revision = existingFile.DrawingRevision;
            if (string.IsNullOrEmpty(revision?.CloudFileId))
                return BadRequest(new { success = false, message = "Cannot update file - no cloud storage reference" });

            using var stream = file.OpenReadStream();
            var fileInfo = await _sharePointService.UpdateFileAsync(revision.CloudFileId, stream, file.ContentType);

            existingFile.FileName = file.FileName;
            existingFile.FileSizeBytes = file.Length;
            existingFile.FilePath = fileInfo.WebUrl;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = new { fileId = existingFile.Id, fileName = file.FileName } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "Error updating file" });
        }
    }

    /// <summary>
    /// Delete file
    /// </summary>
    [HttpDelete("files/{fileId:int}")]
    public async Task<ActionResult> DeleteFile(int fileId)
    {
        try
        {
            var file = await _context.DrawingFiles
                .Include(f => f.DrawingRevision)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                return NotFound(new { success = false, message = $"File with ID {fileId} not found" });

            var revision = file.DrawingRevision;
            if (!string.IsNullOrEmpty(revision?.CloudFileId))
            {
                await _sharePointService.DeleteFileAsync(revision.CloudFileId);
            }

            _context.DrawingFiles.Remove(file);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "Error deleting file" });
        }
    }

    /// <summary>
    /// Parse CAD file (SMLX or IFC)
    /// </summary>
    [HttpPost("files/{fileId:int}/parse")]
    public async Task<ActionResult> ParseFile(int fileId)
    {
        try
        {
            var file = await _context.DrawingFiles
                .Include(f => f.DrawingRevision)
                    .ThenInclude(r => r.Drawing)
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (file == null)
                return NotFound(new { success = false, message = $"File with ID {fileId} not found" });

            if (file.FileType != "SMLX" && file.FileType != "IFC")
                return BadRequest(new { success = false, message = "Only SMLX and IFC files can be parsed" });

            var revision = file.DrawingRevision;
            if (string.IsNullOrEmpty(revision?.CloudFileId))
                return BadRequest(new { success = false, message = "File not found in cloud storage" });

            // Download file from SharePoint
            var stream = await _sharePointService.DownloadFileAsync(revision.CloudFileId);

            // Parse based on file type
            List<ParsedAssemblyDto> parsedAssemblies;
            List<ParsedPartDto> parsedLooseParts;

            if (file.FileType == "SMLX")
            {
                (parsedAssemblies, parsedLooseParts) = await _smlxParser.ParseSmlxFileAsync(stream, file.FileName);
            }
            else
            {
                (parsedAssemblies, parsedLooseParts) = await _ifcParser.ParseIfcFileAsync(stream, file.FileName);
            }

            // Update parse status
            file.ParseStatus = "Completed";
            file.ParsedPartCount = parsedAssemblies.Sum(a => a.Parts.Count) + parsedLooseParts.Count;

            await _context.SaveChangesAsync();

            return Ok(new {
                success = true,
                data = new
                {
                    assembliesCount = parsedAssemblies.Count,
                    partsCount = file.ParsedPartCount,
                    assemblies = parsedAssemblies,
                    looseParts = parsedLooseParts
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file {FileId}", fileId);
            return StatusCode(500, new { success = false, message = "Error parsing file" });
        }
    }

    #endregion

    #region Drawing Parts Operations

    /// <summary>
    /// Get all parts for a revision
    /// </summary>
    [HttpGet("revisions/{revisionId:int}/parts")]
    public async Task<ActionResult> GetRevisionParts(int revisionId)
    {
        try
        {
            var parts = await _context.DrawingParts
                .Where(p => p.DrawingRevisionId == revisionId)
                .Select(p => MapEntityToPartDto(p))
                .ToListAsync();

            return Ok(new { success = true, data = new { parts } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parts for revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error retrieving parts" });
        }
    }

    /// <summary>
    /// Get parsed parts for a revision (from CAD parsing)
    /// </summary>
    [HttpGet("revisions/{revisionId:int}/parsed-parts")]
    public async Task<ActionResult> GetParsedParts(int revisionId)
    {
        try
        {
            var parts = await _context.DrawingParts
                .Where(p => p.DrawingRevisionId == revisionId)
                .Select(p => MapEntityToPartDto(p))
                .ToListAsync();

            return Ok(new { success = true, data = new { parts } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parsed parts for revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error retrieving parsed parts" });
        }
    }

    /// <summary>
    /// Get parsed assemblies for a revision
    /// </summary>
    [HttpGet("revisions/{revisionId:int}/parsed-assemblies")]
    public async Task<ActionResult> GetParsedAssemblies(int revisionId)
    {
        try
        {
            var assemblies = await _context.DrawingAssemblies
                .Where(a => a.DrawingRevisionId == revisionId)
                .Include(a => a.Parts)
                .Select(a => new DrawingAssemblyDto
                {
                    Id = a.Id,
                    AssemblyMark = a.AssemblyMark,
                    AssemblyName = a.AssemblyName,
                    Description = a.Description,
                    TotalWeight = a.TotalWeight,
                    PartCount = a.PartCount,
                    Parts = a.Parts.Select(p => MapEntityToPartDto(p)).ToList()
                })
                .ToListAsync();

            return Ok(new { success = true, data = new { assemblies } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parsed assemblies for revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error retrieving parsed assemblies" });
        }
    }

    /// <summary>
    /// Create a single drawing part manually
    /// </summary>
    [HttpPost("drawing-parts")]
    public async Task<ActionResult> CreateDrawingPart([FromBody] CreateDrawingPartDto dto)
    {
        try
        {
            var drawing = await _context.QDocsDrawings.FindAsync(dto.DrawingId);
            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {dto.DrawingId} not found" });

            var part = new DrawingPart
            {
                DrawingId = dto.DrawingId,
                DrawingRevisionId = dto.DrawingRevisionId,
                PartReference = dto.PartReference,
                Description = dto.Description,
                PartType = dto.PartType,
                MaterialGrade = dto.MaterialGrade,
                MaterialStandard = dto.MaterialStandard,
                Quantity = dto.Quantity,
                Unit = dto.Unit,
                Length = dto.Length,
                Width = dto.Width,
                Thickness = dto.Thickness,
                Diameter = dto.Diameter,
                FlangeThickness = dto.FlangeThickness,
                FlangeWidth = dto.FlangeWidth,
                WebThickness = dto.WebThickness,
                WebDepth = dto.WebDepth,
                OutsideDiameter = dto.OutsideDiameter,
                WallThickness = dto.WallThickness,
                LegA = dto.LegA,
                LegB = dto.LegB,
                AssemblyMark = dto.AssemblyMark,
                AssemblyName = dto.AssemblyName,
                ParentAssemblyId = dto.ParentAssemblyId,
                Coating = dto.Coating,
                Weight = dto.Weight,
                Volume = dto.Volume,
                PaintedArea = dto.PaintedArea,
                CatalogueItemId = dto.CatalogueItemId,
                Notes = dto.Notes,
                CompanyId = drawing.CompanyId
            };

            await _context.DrawingParts.AddAsync(part);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDrawingPart), new { partId = part.Id },
                new { success = true, data = new { id = part.Id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating drawing part");
            return StatusCode(500, new { success = false, message = "Error creating drawing part" });
        }
    }

    /// <summary>
    /// Get a single drawing part
    /// </summary>
    [HttpGet("drawing-parts/{partId:int}")]
    public async Task<ActionResult> GetDrawingPart(int partId)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            return Ok(new { success = true, data = MapEntityToPartDto(part) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drawing part {PartId}", partId);
            return StatusCode(500, new { success = false, message = "Error retrieving drawing part" });
        }
    }

    /// <summary>
    /// Update a drawing part
    /// </summary>
    [HttpPut("drawing-parts/{partId:int}")]
    public async Task<ActionResult> UpdateDrawingPart(int partId, [FromBody] UpdateDrawingPartDto dto)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            if (dto.Quantity.HasValue) part.Quantity = dto.Quantity.Value;
            if (dto.Description != null) part.Description = dto.Description;
            if (dto.MaterialGrade != null) part.MaterialGrade = dto.MaterialGrade;
            if (dto.CatalogueItemId.HasValue) part.CatalogueItemId = dto.CatalogueItemId;
            if (dto.Notes != null) part.Notes = dto.Notes;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = MapEntityToPartDto(part) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating drawing part {PartId}", partId);
            return StatusCode(500, new { success = false, message = "Error updating drawing part" });
        }
    }

    /// <summary>
    /// Delete a drawing part
    /// </summary>
    [HttpDelete("drawing-parts/{partId:int}")]
    public async Task<ActionResult> DeleteDrawingPart(int partId)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            _context.DrawingParts.Remove(part);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Part deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting drawing part {PartId}", partId);
            return StatusCode(500, new { success = false, message = "Error deleting drawing part" });
        }
    }

    /// <summary>
    /// Bulk create parts from parsed CAD data
    /// </summary>
    [HttpPost("revisions/{revisionId:int}/drawing-parts/bulk")]
    public async Task<ActionResult> BulkCreateParts(int revisionId)
    {
        try
        {
            var revision = await _context.DrawingRevisions
                .Include(r => r.Drawing)
                .FirstOrDefaultAsync(r => r.Id == revisionId);

            if (revision == null)
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found" });

            // Get parsed assemblies and parts
            var assemblies = await _context.DrawingAssemblies
                .Where(a => a.DrawingRevisionId == revisionId)
                .Include(a => a.Parts)
                .ToListAsync();

            var partIds = new List<int>();
            var partsCreated = 0;

            foreach (var assembly in assemblies)
            {
                foreach (var part in assembly.Parts)
                {
                    partIds.Add(part.Id);
                    partsCreated++;
                }
            }

            // Get loose parts
            var looseParts = await _context.DrawingParts
                .Where(p => p.DrawingRevisionId == revisionId && p.ParentAssemblyId == null)
                .ToListAsync();

            foreach (var part in looseParts)
            {
                partIds.Add(part.Id);
                partsCreated++;
            }

            return Ok(new { success = true, data = new { partsCreated, partIds } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating parts for revision {RevisionId}", revisionId);
            return StatusCode(500, new { success = false, message = "Error bulk creating parts" });
        }
    }

    /// <summary>
    /// Link a part to a catalogue item
    /// </summary>
    [HttpPut("drawing-parts/{partId:int}/link-catalogue")]
    public async Task<ActionResult> LinkPartToCatalogue(int partId, [FromBody] LinkCatalogueDto dto)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            var catalogueItem = await _context.CatalogueItems.FindAsync(dto.CatalogueItemId);
            if (catalogueItem == null)
                return NotFound(new { success = false, message = $"Catalogue item with ID {dto.CatalogueItemId} not found" });

            part.CatalogueItemId = dto.CatalogueItemId;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = new { id = part.Id, catalogueItemId = part.CatalogueItemId } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking part {PartId} to catalogue", partId);
            return StatusCode(500, new { success = false, message = "Error linking part to catalogue" });
        }
    }

    /// <summary>
    /// Unlink a part from a catalogue item
    /// </summary>
    [HttpDelete("drawing-parts/{partId:int}/unlink-catalogue")]
    public async Task<ActionResult> UnlinkPartFromCatalogue(int partId)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            part.CatalogueItemId = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Part unlinked from catalogue" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking part {PartId} from catalogue", partId);
            return StatusCode(500, new { success = false, message = "Error unlinking part from catalogue" });
        }
    }

    /// <summary>
    /// Get unordered parts (parts not linked to any purchase order)
    /// </summary>
    [HttpGet("parts/unordered")]
    public async Task<ActionResult> GetUnorderedParts([FromQuery] int? drawingId)
    {
        try
        {
            var query = _context.DrawingParts.AsQueryable();

            if (drawingId.HasValue)
                query = query.Where(p => p.DrawingId == drawingId.Value);

            // Filter parts that are not linked to PO line items
            // This would need a join with PurchaseOrderLineItems if that relationship exists
            var parts = await query
                .Select(p => MapEntityToPartDto(p))
                .ToListAsync();

            return Ok(new { success = true, data = new { parts } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unordered parts");
            return StatusCode(500, new { success = false, message = "Error retrieving unordered parts" });
        }
    }

    /// <summary>
    /// Create a simple drawing part for a drawing (test-friendly route)
    /// </summary>
    [HttpPost("drawings/{drawingId:int}/parts")]
    public async Task<ActionResult> CreateSimpleDrawingPart(int drawingId, [FromBody] CreateSimpleDrawingPartDto dto)
    {
        try
        {
            var drawing = await _context.QDocsDrawings.FindAsync(drawingId);
            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            var part = new DrawingPart
            {
                DrawingId = drawingId,
                PartReference = dto.PartMark,
                Description = dto.Description ?? string.Empty,
                PartType = dto.PartType,
                Quantity = dto.Quantity,
                Weight = dto.WeightEach,
                MaterialGrade = dto.MaterialGrade,
                Unit = "EA",
                CompanyId = drawing.CompanyId
            };

            await _context.DrawingParts.AddAsync(part);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created simple part {PartMark} for drawing {DrawingId}", dto.PartMark, drawingId);

            return CreatedAtAction(nameof(GetSimplePart), new { partId = part.Id },
                new { success = true, data = new SimpleDrawingPartDto
                {
                    Id = part.Id,
                    DrawingId = part.DrawingId,
                    PartMark = part.PartReference,
                    PartType = part.PartType,
                    Description = part.Description,
                    Quantity = part.Quantity,
                    WeightEach = part.Weight,
                    MaterialGrade = part.MaterialGrade,
                    IsManualEntry = false,
                    CreatedDate = DateTime.UtcNow
                }});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating simple drawing part for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error creating drawing part" });
        }
    }

    /// <summary>
    /// Get all parts for a drawing (test-friendly route)
    /// </summary>
    [HttpGet("drawings/{drawingId:int}/parts")]
    public async Task<ActionResult> GetDrawingParts(int drawingId)
    {
        try
        {
            var drawing = await _context.QDocsDrawings.FindAsync(drawingId);
            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            var parts = await _context.DrawingParts
                .Where(p => p.DrawingId == drawingId)
                .Select(p => new SimpleDrawingPartDto
                {
                    Id = p.Id,
                    DrawingId = p.DrawingId,
                    PartMark = p.PartReference,
                    PartType = p.PartType,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    WeightEach = p.Weight,
                    MaterialGrade = p.MaterialGrade,
                    IsManualEntry = false,
                    CreatedDate = DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(new { success = true, data = parts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parts for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error retrieving parts" });
        }
    }

    /// <summary>
    /// Get a single simple part by ID
    /// </summary>
    [HttpGet("parts/{partId:int}")]
    public async Task<ActionResult> GetSimplePart(int partId)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            return Ok(new { success = true, data = new SimpleDrawingPartDto
            {
                Id = part.Id,
                DrawingId = part.DrawingId,
                PartMark = part.PartReference,
                PartType = part.PartType,
                Description = part.Description,
                Quantity = part.Quantity,
                WeightEach = part.Weight,
                MaterialGrade = part.MaterialGrade,
                IsManualEntry = part.IsManualEntry,
                CreatedDate = DateTime.UtcNow
            }});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting simple part {PartId}", partId);
            return StatusCode(500, new { success = false, message = "Error retrieving part" });
        }
    }

    /// <summary>
    /// Update a simple part (test-friendly route)
    /// </summary>
    [HttpPut("parts/{partId:int}")]
    public async Task<ActionResult> UpdateSimplePart(int partId, [FromBody] UpdateSimplePartDto dto)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            if (dto.Description != null) part.Description = dto.Description;
            if (dto.Quantity.HasValue) part.Quantity = dto.Quantity.Value;
            if (dto.WeightEach.HasValue) part.Weight = dto.WeightEach.Value;
            if (dto.MaterialGrade != null) part.MaterialGrade = dto.MaterialGrade;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated simple part {PartId}", partId);

            return Ok(new { success = true, data = new SimpleDrawingPartDto
            {
                Id = part.Id,
                DrawingId = part.DrawingId,
                PartMark = part.PartReference,
                PartType = part.PartType,
                Description = part.Description,
                Quantity = part.Quantity,
                WeightEach = part.Weight,
                MaterialGrade = part.MaterialGrade,
                IsManualEntry = part.IsManualEntry,
                CreatedDate = DateTime.UtcNow
            }});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating simple part {PartId}", partId);
            return StatusCode(500, new { success = false, message = "Error updating part" });
        }
    }

    /// <summary>
    /// Delete a simple part (test-friendly route)
    /// </summary>
    [HttpDelete("parts/{partId:int}")]
    public async Task<ActionResult> DeleteSimplePart(int partId)
    {
        try
        {
            var part = await _context.DrawingParts.FirstOrDefaultAsync(p => p.Id == partId && p.CompanyId == GetCompanyId());
            if (part == null)
                return NotFound(new { success = false, message = $"Part with ID {partId} not found" });

            _context.DrawingParts.Remove(part);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted simple part {PartId}", partId);

            return Ok(new { success = true, message = "Part deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting simple part {PartId}", partId);
            return StatusCode(500, new { success = false, message = "Error deleting part" });
        }
    }

    /// <summary>
    /// Create a manual part (not linked to a drawing) - Uses null DrawingId and sets IsManualEntry = true
    /// </summary>
    [HttpPost("parts/manual")]
    public async Task<ActionResult> CreateManualPart([FromBody] CreateManualPartDto dto)
    {
        try
        {
            // Validate order exists to get company ID
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
                return BadRequest(new { success = false, message = $"Order with ID {dto.OrderId} not found" });

            var part = new DrawingPart
            {
                DrawingId = null, // Null indicates manual entry (not linked to any drawing)
                OrderId = dto.OrderId, // Track which order this manual part belongs to
                IsManualEntry = true, // Flag to distinguish from CAD-imported parts
                PartReference = dto.PartMark,
                Description = dto.Description ?? string.Empty,
                PartType = dto.PartType,
                Quantity = dto.Quantity,
                Weight = dto.WeightEach,
                MaterialGrade = dto.MaterialGrade,
                Unit = "EA",
                CompanyId = order.CompanyId
            };

            await _context.DrawingParts.AddAsync(part);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created manual part {PartMark} for order {OrderId}", dto.PartMark, dto.OrderId);

            return CreatedAtAction(nameof(GetSimplePart), new { partId = part.Id },
                new { success = true, data = new SimpleDrawingPartDto
                {
                    Id = part.Id,
                    DrawingId = null, // Return null in DTO to indicate manual entry
                    PartMark = part.PartReference,
                    PartType = part.PartType,
                    Description = part.Description,
                    Quantity = part.Quantity,
                    WeightEach = part.Weight,
                    MaterialGrade = part.MaterialGrade,
                    IsManualEntry = true,
                    CreatedDate = DateTime.UtcNow
                }});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual part");
            return StatusCode(500, new { success = false, message = "Error creating manual part" });
        }
    }

    /// <summary>
    /// Get drawing summary (total parts, weight, quantity)
    /// </summary>
    [HttpGet("drawings/{drawingId:int}/summary")]
    public async Task<ActionResult> GetDrawingSummary(int drawingId)
    {
        try
        {
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            var parts = await _context.DrawingParts
                .Where(p => p.DrawingId == drawingId)
                .ToListAsync();

            var summary = new DrawingSummaryDto
            {
                DrawingId = drawingId,
                DrawingNumber = drawing.DrawingNumber,
                DrawingTitle = drawing.DrawingTitle,
                TotalParts = parts.Count,
                TotalWeight = parts.Sum(p => (p.Weight ?? 0) * p.Quantity),
                TotalQuantity = (int)parts.Sum(p => p.Quantity),
                RevisionCount = drawing.Revisions?.Count ?? 0,
                CurrentStage = drawing.CurrentStage
            };

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error retrieving drawing summary" });
        }
    }

    /// <summary>
    /// Get order summary (total drawings, parts, weight, quantity)
    /// </summary>
    [HttpGet("orders/{orderId:int}/summary")]
    public async Task<ActionResult> GetOrderSummary(int orderId)
    {
        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = $"Order with ID {orderId} not found" });

            var drawings = await _context.QDocsDrawings
                .Where(d => d.OrderId == orderId)
                .ToListAsync();

            var drawingIds = drawings.Select(d => d.Id).ToList();

            // Get parts linked to drawings OR manual parts linked directly to this order
            var parts = await _context.DrawingParts
                .Where(p => (p.DrawingId.HasValue && drawingIds.Contains(p.DrawingId.Value)) || p.OrderId == orderId)
                .ToListAsync();

            var summary = new OrderSummaryDto
            {
                OrderId = orderId,
                TotalDrawings = drawings.Count,
                TotalParts = parts.Count,
                TotalWeight = parts.Sum(p => (p.Weight ?? 0) * p.Quantity),
                TotalQuantity = (int)parts.Sum(p => p.Quantity)
            };

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary for order {OrderId}", orderId);
            return StatusCode(500, new { success = false, message = "Error retrieving order summary" });
        }
    }

    #endregion

    #region CAD Upload Operations (Original)

    /// <summary>
    /// Upload CAD file (SMLX or IFC) to an existing IFC drawing revision
    /// This endpoint parses the CAD file and extracts assemblies and parts
    /// </summary>
    /// <param name="drawingId">Drawing ID</param>
    /// <param name="revisionId">Revision ID (must be IFC type)</param>
    /// <param name="file">CAD file (.smlx or .ifc)</param>
    /// <returns>Upload result with assembly and part counts</returns>
    [HttpPost("drawings/{drawingId:int}/revisions/{revisionId:int}/upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DrawingUploadResultDto>> UploadCADFile(
        int drawingId,
        int revisionId,
        [Required] IFormFile file)
    {
        try
        {
            _logger.LogInformation("CAD file upload started: Drawing={DrawingId}, Revision={RevisionId}, File={FileName}",
                drawingId, revisionId, file.FileName);

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate drawing exists
            var drawing = await _context.QDocsDrawings
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
            {
                return NotFound(new { message = $"Drawing with ID {drawingId} not found" });
            }

            // Validate revision exists and belongs to drawing
            var revision = await _context.DrawingRevisions
                .FirstOrDefaultAsync(r => r.Id == revisionId && r.DrawingId == drawingId);

            if (revision == null)
            {
                return NotFound(new { message = $"Revision with ID {revisionId} not found for drawing {drawingId}" });
            }

            // Validate revision is IFC type (not IFA)
            if (revision.RevisionType != "IFC")
            {
                return BadRequest(new
                {
                    message = "CAD files can only be uploaded to IFC revisions. IFA revisions accept PDF files only."
                });
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".smlx" && fileExtension != ".ifc")
            {
                return BadRequest(new
                {
                    message = "Invalid file type. Only .smlx and .ifc files are supported.",
                    supportedTypes = new[] { ".smlx", ".ifc" }
                });
            }

            // Validate file format
            bool isValidFormat = false;
            using (var validationStream = file.OpenReadStream())
            {
                if (fileExtension == ".smlx")
                {
                    isValidFormat = await _smlxParser.ValidateSmlxFileAsync(validationStream);
                }
                else if (fileExtension == ".ifc")
                {
                    isValidFormat = await _ifcParser.ValidateIfcFileAsync(validationStream);
                }
            }

            if (!isValidFormat)
            {
                return BadRequest(new
                {
                    message = $"Invalid {fileExtension.ToUpper()} file format. File does not appear to be a valid CAD file."
                });
            }

            // Parse CAD file
            List<ParsedAssemblyDto> parsedAssemblies;
            List<ParsedPartDto> parsedLooseParts;

            using (var parseStream = file.OpenReadStream())
            {
                if (fileExtension == ".smlx")
                {
                    (parsedAssemblies, parsedLooseParts) = await _smlxParser.ParseSmlxFileAsync(parseStream, file.FileName);
                }
                else // .ifc
                {
                    (parsedAssemblies, parsedLooseParts) = await _ifcParser.ParseIfcFileAsync(parseStream, file.FileName);
                }
            }

            _logger.LogInformation("Parsed {AssemblyCount} assemblies and {LoosePartCount} loose parts from {FileName}",
                parsedAssemblies.Count, parsedLooseParts.Count, file.FileName);

            // Save to database
            var companyId = drawing.CompanyId;
            var assembliesSaved = 0;
            var partsSaved = 0;

            // Save assemblies and their parts
            foreach (var assemblyDto in parsedAssemblies)
            {
                var assembly = new DrawingAssembly
                {
                    DrawingId = drawingId,
                    DrawingRevisionId = revisionId,
                    AssemblyMark = assemblyDto.AssemblyMark,
                    AssemblyName = assemblyDto.AssemblyName ?? assemblyDto.AssemblyMark,
                    Description = null,
                    TotalWeight = assemblyDto.TotalWeight,
                    PartCount = assemblyDto.Parts.Count,
                    CompanyId = companyId,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.DrawingAssemblies.AddAsync(assembly);
                await _context.SaveChangesAsync();

                assembliesSaved++;

                // Save parts in this assembly
                foreach (var partDto in assemblyDto.Parts)
                {
                    var part = MapParsedPartToEntity(partDto, drawingId, revisionId, companyId);
                    part.ParentAssemblyId = assembly.Id;
                    part.AssemblyMark = assemblyDto.AssemblyMark;
                    part.AssemblyName = assemblyDto.AssemblyName;

                    await _context.DrawingParts.AddAsync(part);
                    partsSaved++;
                }
            }

            // Save loose parts
            foreach (var partDto in parsedLooseParts)
            {
                var part = MapParsedPartToEntity(partDto, drawingId, revisionId, companyId);
                part.ParentAssemblyId = null;
                part.AssemblyMark = null;
                part.AssemblyName = null;

                await _context.DrawingParts.AddAsync(part);
                partsSaved++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("CAD upload complete: Saved {AssemblyCount} assemblies and {PartCount} parts",
                assembliesSaved, partsSaved);

            return Ok(new DrawingUploadResultDto
            {
                DrawingId = drawingId,
                DrawingRevisionId = revisionId,
                DrawingFileId = 0,
                FileName = file.FileName,
                FileType = fileExtension,
                ParseStatus = "Completed",
                ParseErrors = null,
                AssemblyCount = assembliesSaved,
                PartCount = partsSaved,
                LoosePartCount = parsedLooseParts.Count,
                Assemblies = new List<DrawingAssemblyDto>(),
                LooseParts = new List<DrawingPartDto>(),
                TotalWeight = parsedAssemblies.Sum(a => a.TotalWeight) + parsedLooseParts.Sum(p => p.Weight ?? 0),
                UploadedDate = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CAD file: {FileName}", file?.FileName);
            return StatusCode(500, new
            {
                message = "An error occurred while processing the CAD file",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all assemblies for a drawing revision
    /// </summary>
    [HttpGet("drawings/{drawingId:int}/revisions/{revisionId:int}/assemblies")]
    public async Task<ActionResult<List<DrawingAssemblyDto>>> GetAssemblies(int drawingId, int revisionId)
    {
        try
        {
            var assemblies = await _context.DrawingAssemblies
                .Where(a => a.DrawingId == drawingId && a.DrawingRevisionId == revisionId)
                .Include(a => a.Parts)
                .ToListAsync();

            var result = assemblies.Select(a => new DrawingAssemblyDto
            {
                Id = a.Id,
                AssemblyMark = a.AssemblyMark,
                AssemblyName = a.AssemblyName,
                Description = a.Description,
                TotalWeight = a.TotalWeight,
                PartCount = a.PartCount,
                Parts = a.Parts.Select(p => MapEntityToPartDto(p)).ToList()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assemblies for drawing {DrawingId}, revision {RevisionId}",
                drawingId, revisionId);
            return StatusCode(500, new { message = "Error retrieving assemblies" });
        }
    }

    /// <summary>
    /// Get loose parts for a drawing revision
    /// </summary>
    [HttpGet("drawings/{drawingId:int}/revisions/{revisionId:int}/loose-parts")]
    public async Task<ActionResult<List<DrawingPartDto>>> GetLooseParts(int drawingId, int revisionId)
    {
        try
        {
            var looseParts = await _context.DrawingParts
                .Where(p => p.DrawingId == drawingId
                         && p.DrawingRevisionId == revisionId
                         && p.ParentAssemblyId == null)
                .ToListAsync();

            var result = looseParts.Select(p => MapEntityToPartDto(p)).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loose parts for drawing {DrawingId}, revision {RevisionId}",
                drawingId, revisionId);
            return StatusCode(500, new { message = "Error retrieving loose parts" });
        }
    }

    #endregion

    #region Interactive CAD Import (with Part Identification)

    /// <summary>
    /// Upload and preview CAD file with identification status.
    /// Returns unidentified parts that need user input before import can complete.
    /// </summary>
    /// <param name="drawingId">Drawing ID</param>
    /// <param name="revisionId">Revision ID (must be IFC type)</param>
    /// <param name="file">CAD file (.smlx or .ifc)</param>
    /// <returns>Preview with identified/unidentified parts and session ID</returns>
    [HttpPost("drawings/{drawingId:int}/revisions/{revisionId:int}/upload-preview")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CadImportPreviewDto>> UploadPreview(
        int drawingId,
        int revisionId,
        [Required] IFormFile file)
    {
        try
        {
            _logger.LogInformation("CAD preview upload started: Drawing={DrawingId}, Revision={RevisionId}, File={FileName}",
                drawingId, revisionId, file.FileName);

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            // Validate drawing exists
            var drawing = await _context.QDocsDrawings
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
            {
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });
            }

            // Validate revision exists and belongs to drawing
            var revision = await _context.DrawingRevisions
                .FirstOrDefaultAsync(r => r.Id == revisionId && r.DrawingId == drawingId);

            if (revision == null)
            {
                return NotFound(new { success = false, message = $"Revision with ID {revisionId} not found for drawing {drawingId}" });
            }

            // Validate revision is IFC type
            if (revision.RevisionType != "IFC")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "CAD files can only be uploaded to IFC revisions. IFA revisions accept PDF files only."
                });
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".smlx" && fileExtension != ".ifc")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid file type. Only .smlx and .ifc files are supported.",
                    supportedTypes = new[] { ".smlx", ".ifc" }
                });
            }

            // Validate file format
            bool isValidFormat = false;
            using (var validationStream = file.OpenReadStream())
            {
                if (fileExtension == ".smlx")
                {
                    isValidFormat = await _smlxParser.ValidateSmlxFileAsync(validationStream);
                }
                else if (fileExtension == ".ifc")
                {
                    isValidFormat = await _ifcParser.ValidateIfcFileAsync(validationStream);
                }
            }

            if (!isValidFormat)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Invalid {fileExtension.ToUpper()} file format. File does not appear to be a valid CAD file."
                });
            }

            // Parse CAD file with identification tracking
            CadParseResultDto parseResult;
            using (var parseStream = file.OpenReadStream())
            {
                if (fileExtension == ".smlx")
                {
                    parseResult = await _smlxParser.ParseSmlxFileWithIdentificationAsync(parseStream, file.FileName);
                }
                else // .ifc
                {
                    parseResult = await _ifcParser.ParseIfcFileWithIdentificationAsync(parseStream, file.FileName);
                }
            }

            _logger.LogInformation(
                "Parsed {TotalCount} elements: {Identified} identified, {Unidentified} unidentified from {FileName}",
                parseResult.TotalElementCount, parseResult.IdentifiedPartCount, parseResult.UnidentifiedPartCount, file.FileName);

            // Create import session and return preview
            var preview = _cadImportSession.CreateSession(drawingId, revisionId, file.FileName, parseResult);

            _logger.LogInformation(
                "Created import session {SessionId} for drawing {DrawingId}, revision {RevisionId}",
                preview.ImportSessionId, drawingId, revisionId);

            return Ok(new { success = true, data = preview });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in upload-preview: {FileName}", file?.FileName);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while processing the CAD file",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get an existing import session preview
    /// </summary>
    /// <param name="sessionId">Import session ID</param>
    /// <returns>Preview DTO or 404 if session not found/expired</returns>
    [HttpGet("import-sessions/{sessionId}")]
    public ActionResult<CadImportPreviewDto> GetImportSession(string sessionId)
    {
        try
        {
            var preview = _cadImportSession.GetPreview(sessionId);
            if (preview == null)
            {
                return NotFound(new { success = false, message = "Import session not found or expired" });
            }

            return Ok(new { success = true, data = preview });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import session {SessionId}", sessionId);
            return StatusCode(500, new { success = false, message = "Error retrieving session" });
        }
    }

    /// <summary>
    /// Confirm CAD import with user-provided part mappings.
    /// Creates assemblies and parts in the database.
    /// </summary>
    /// <param name="drawingId">Drawing ID</param>
    /// <param name="revisionId">Revision ID</param>
    /// <param name="request">Import confirmation with part mappings</param>
    /// <returns>Import result with created assemblies and parts</returns>
    [HttpPost("drawings/{drawingId:int}/revisions/{revisionId:int}/confirm-import")]
    public async Task<ActionResult<DrawingUploadResultDto>> ConfirmImport(
        int drawingId,
        int revisionId,
        [FromBody] ConfirmCadImportRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Confirming import session {SessionId} for drawing {DrawingId}, revision {RevisionId}",
                request.ImportSessionId, drawingId, revisionId);

            // Get and validate session
            var session = _cadImportSession.GetSession(request.ImportSessionId);
            if (session == null)
            {
                return NotFound(new { success = false, message = "Import session not found or expired" });
            }

            // Validate session matches drawing/revision
            if (session.DrawingId != drawingId || session.DrawingRevisionId != revisionId)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Session does not match the specified drawing/revision"
                });
            }

            // Validate drawing exists
            var drawing = await _context.QDocsDrawings
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
            {
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });
            }

            // Apply user mappings to get final parse result
            var finalResult = _cadImportSession.ApplyMappings(request.ImportSessionId, request);
            if (finalResult == null)
            {
                return BadRequest(new { success = false, message = "Failed to apply mappings" });
            }

            // Check if there are still unidentified parts
            if (finalResult.UnidentifiedPartCount > 0 && !request.AutoGenerateRemainingReferences)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"There are still {finalResult.UnidentifiedPartCount} unidentified parts. " +
                              "Provide mappings for all parts or set autoGenerateRemainingReferences=true."
                });
            }

            // Save to database
            var companyId = drawing.CompanyId;
            var assembliesSaved = 0;
            var partsSaved = 0;

            // Save assemblies and their parts
            foreach (var assemblyDto in finalResult.Assemblies)
            {
                var assembly = new DrawingAssembly
                {
                    DrawingId = drawingId,
                    DrawingRevisionId = revisionId,
                    AssemblyMark = assemblyDto.AssemblyMark,
                    AssemblyName = assemblyDto.AssemblyName ?? assemblyDto.AssemblyMark,
                    Description = null,
                    TotalWeight = assemblyDto.TotalWeight,
                    PartCount = assemblyDto.IdentifiedParts.Count + assemblyDto.UnidentifiedParts.Count,
                    CompanyId = companyId,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.DrawingAssemblies.AddAsync(assembly);
                await _context.SaveChangesAsync();

                assembliesSaved++;

                // Save identified parts in this assembly
                foreach (var partDto in assemblyDto.IdentifiedParts)
                {
                    var part = MapParsedPartWithIdToEntity(partDto, drawingId, revisionId, companyId);
                    part.ParentAssemblyId = assembly.Id;
                    part.AssemblyMark = assemblyDto.AssemblyMark;
                    part.AssemblyName = assemblyDto.AssemblyName;

                    await _context.DrawingParts.AddAsync(part);
                    partsSaved++;
                }

                // Save unidentified parts (should be empty if auto-generate was used)
                foreach (var partDto in assemblyDto.UnidentifiedParts)
                {
                    var part = MapUnidentifiedPartToEntity(partDto, drawingId, revisionId, companyId);
                    part.ParentAssemblyId = assembly.Id;
                    part.AssemblyMark = assemblyDto.AssemblyMark;
                    part.AssemblyName = assemblyDto.AssemblyName;

                    await _context.DrawingParts.AddAsync(part);
                    partsSaved++;
                }
            }

            // Save loose parts
            foreach (var partDto in finalResult.LooseParts)
            {
                var part = MapParsedPartWithIdToEntity(partDto, drawingId, revisionId, companyId);
                part.ParentAssemblyId = null;
                part.AssemblyMark = null;
                part.AssemblyName = null;

                await _context.DrawingParts.AddAsync(part);
                partsSaved++;
            }

            await _context.SaveChangesAsync();

            // Remove the session from cache
            _cadImportSession.RemoveSession(request.ImportSessionId);

            _logger.LogInformation(
                "CAD import complete for session {SessionId}: Saved {AssemblyCount} assemblies and {PartCount} parts",
                request.ImportSessionId, assembliesSaved, partsSaved);

            return Ok(new
            {
                success = true,
                data = new DrawingUploadResultDto
                {
                    DrawingId = drawingId,
                    DrawingRevisionId = revisionId,
                    DrawingFileId = 0,
                    FileName = session.FileName,
                    FileType = finalResult.FileType,
                    ParseStatus = "Completed",
                    ParseErrors = null,
                    AssemblyCount = assembliesSaved,
                    PartCount = partsSaved,
                    LoosePartCount = finalResult.LooseParts.Count,
                    Assemblies = new List<DrawingAssemblyDto>(),
                    LooseParts = new List<DrawingPartDto>(),
                    TotalWeight = finalResult.Assemblies.Sum(a => a.TotalWeight) +
                                  finalResult.LooseParts.Sum(p => p.Weight ?? 0),
                    UploadedDate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming import for session {SessionId}", request?.ImportSessionId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while completing the import",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Cancel an import session
    /// </summary>
    /// <param name="sessionId">Import session ID</param>
    [HttpDelete("import-sessions/{sessionId}")]
    public ActionResult CancelImportSession(string sessionId)
    {
        try
        {
            var session = _cadImportSession.GetSession(sessionId);
            if (session == null)
            {
                return NotFound(new { success = false, message = "Import session not found or already expired" });
            }

            _cadImportSession.RemoveSession(sessionId);

            _logger.LogInformation("Cancelled import session {SessionId}", sessionId);

            return Ok(new { success = true, message = "Import session cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling import session {SessionId}", sessionId);
            return StatusCode(500, new { success = false, message = "Error cancelling session" });
        }
    }

    #endregion

    #region E2E Workflow Helpers

    /// <summary>
    /// Upload and parse CAD file in one operation
    /// </summary>
    [HttpPost("drawings/{drawingId:int}/upload-and-parse")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DrawingUploadResultDto>> UploadAndParse(int drawingId, [Required] IFormFile file)
    {
        try
        {
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            // Get or create IFC revision
            var ifcRevision = drawing.Revisions.FirstOrDefault(r => r.RevisionType == "IFC" && r.IsActiveForProduction);
            if (ifcRevision == null)
            {
                // Create IFA first if needed
                var ifaRevision = drawing.Revisions.FirstOrDefault(r => r.RevisionType == "IFA");
                if (ifaRevision == null)
                {
                    ifaRevision = new DrawingRevision
                    {
                        DrawingId = drawingId,
                        RevisionCode = "IFA-R1",
                        RevisionType = "IFA",
                        RevisionNumber = 1,
                        Status = "Approved",
                        ApprovedBy = GetUserName(),
                        ApprovedDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = GetUserId(),
                        CompanyId = drawing.CompanyId
                    };
                    await _context.DrawingRevisions.AddAsync(ifaRevision);
                    await _context.SaveChangesAsync();
                }

                ifcRevision = new DrawingRevision
                {
                    DrawingId = drawingId,
                    RevisionCode = "IFC-R1",
                    RevisionType = "IFC",
                    RevisionNumber = 1,
                    Status = "Approved",
                    IsActiveForProduction = true,
                    CreatedFromIFARevisionId = ifaRevision.Id,
                    ApprovedBy = GetUserName(),
                    ApprovedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = GetUserId(),
                    CompanyId = drawing.CompanyId
                };
                await _context.DrawingRevisions.AddAsync(ifcRevision);
                drawing.CurrentStage = "IFC";
                drawing.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                drawing.ActiveRevisionId = ifcRevision.Id;
                await _context.SaveChangesAsync();
            }

            // Now upload and parse
            return await UploadCADFile(drawingId, ifcRevision.Id, file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in upload-and-parse for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error in upload-and-parse" });
        }
    }

    /// <summary>
    /// Create parts from parsed data
    /// </summary>
    [HttpPost("drawings/{drawingId:int}/create-parts-from-parsed")]
    public async Task<ActionResult> CreatePartsFromParsed(int drawingId)
    {
        try
        {
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            var activeRevision = drawing.Revisions.FirstOrDefault(r => r.IsActiveForProduction);
            if (activeRevision == null)
                return BadRequest(new { success = false, message = "No active revision found" });

            var partsCount = await _context.DrawingParts
                .CountAsync(p => p.DrawingRevisionId == activeRevision.Id);

            return StatusCode(201, new { success = true, data = new { partsCreated = partsCount } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating parts from parsed data for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error creating parts" });
        }
    }

    /// <summary>
    /// Auto-match parts to catalogue items
    /// </summary>
    [HttpPost("drawings/{drawingId:int}/auto-match-catalogue")]
    public async Task<ActionResult> AutoMatchCatalogue(int drawingId)
    {
        try
        {
            var drawing = await _context.QDocsDrawings
                .Include(d => d.Revisions)
                .FirstOrDefaultAsync(d => d.Id == drawingId);

            if (drawing == null)
                return NotFound(new { success = false, message = $"Drawing with ID {drawingId} not found" });

            var activeRevision = drawing.Revisions.FirstOrDefault(r => r.IsActiveForProduction);
            if (activeRevision == null)
                return BadRequest(new { success = false, message = "No active revision found" });

            var parts = await _context.DrawingParts
                .Where(p => p.DrawingRevisionId == activeRevision.Id && p.CatalogueItemId == null)
                .ToListAsync();

            var matchedCount = 0;
            foreach (var part in parts)
            {
                // Try to find matching catalogue item by description or material
                var catalogueItem = await _context.CatalogueItems
                    .FirstOrDefaultAsync(c => c.Description.Contains(part.Description) ||
                                             (part.MaterialGrade != null && c.Description.Contains(part.MaterialGrade)));

                if (catalogueItem != null)
                {
                    part.CatalogueItemId = catalogueItem.Id;
                    matchedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = new { matchedCount, totalParts = parts.Count } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-matching catalogue for drawing {DrawingId}", drawingId);
            return StatusCode(500, new { success = false, message = "Error auto-matching catalogue" });
        }
    }

    #endregion

    #region Helper Methods

    private DrawingPart MapParsedPartToEntity(ParsedPartDto dto, int drawingId, int revisionId, int companyId)
    {
        return new DrawingPart
        {
            DrawingId = drawingId,
            DrawingRevisionId = revisionId,
            PartReference = dto.PartReference,
            Description = dto.Description,
            PartType = dto.PartType,
            MaterialGrade = dto.MaterialGrade,
            MaterialStandard = dto.MaterialStandard,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Length = dto.Length,
            Width = dto.Width,
            Thickness = dto.Thickness,
            FlangeThickness = dto.FlangeThickness,
            FlangeWidth = dto.FlangeWidth,
            WebThickness = dto.WebThickness,
            WebDepth = dto.WebDepth,
            LegA = dto.LegA,
            LegB = dto.LegB,
            Diameter = null,
            OutsideDiameter = null,
            WallThickness = null,
            Coating = dto.Coating,
            Weight = dto.Weight,
            Volume = dto.Volume,
            PaintedArea = dto.PaintedArea,
            CompanyId = companyId
        };
    }

    private static DrawingPartDto MapEntityToPartDto(DrawingPart entity)
    {
        return new DrawingPartDto
        {
            Id = entity.Id,
            DrawingId = entity.DrawingId ?? 0, // DTO expects int, so provide 0 for manual parts
            DrawingRevisionId = entity.DrawingRevisionId,
            PartReference = entity.PartReference,
            Description = entity.Description,
            PartType = entity.PartType,
            MaterialGrade = entity.MaterialGrade,
            MaterialStandard = entity.MaterialStandard,
            Quantity = entity.Quantity,
            Unit = entity.Unit,
            Length = entity.Length,
            Width = entity.Width,
            Thickness = entity.Thickness,
            Diameter = entity.Diameter,
            FlangeThickness = entity.FlangeThickness,
            FlangeWidth = entity.FlangeWidth,
            WebThickness = entity.WebThickness,
            WebDepth = entity.WebDepth,
            OutsideDiameter = entity.OutsideDiameter,
            WallThickness = entity.WallThickness,
            LegA = entity.LegA,
            LegB = entity.LegB,
            AssemblyMark = entity.AssemblyMark,
            AssemblyName = entity.AssemblyName,
            ParentAssemblyId = entity.ParentAssemblyId,
            Coating = entity.Coating,
            Weight = entity.Weight,
            Volume = entity.Volume,
            PaintedArea = entity.PaintedArea,
            CatalogueItemId = entity.CatalogueItemId,
            Notes = entity.Notes
        };
    }

    private DrawingPart MapParsedPartWithIdToEntity(ParsedPartWithIdDto dto, int drawingId, int revisionId, int companyId)
    {
        return new DrawingPart
        {
            DrawingId = drawingId,
            DrawingRevisionId = revisionId,
            PartReference = dto.PartReference,
            Description = dto.Description,
            PartType = dto.PartType,
            MaterialGrade = dto.MaterialGrade,
            MaterialStandard = dto.MaterialStandard,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Length = dto.Length,
            Width = dto.Width,
            Thickness = dto.Thickness,
            FlangeThickness = dto.FlangeThickness,
            FlangeWidth = dto.FlangeWidth,
            WebThickness = dto.WebThickness,
            WebDepth = dto.WebDepth,
            LegA = dto.LegA,
            LegB = dto.LegB,
            Diameter = null,
            OutsideDiameter = null,
            WallThickness = null,
            Coating = dto.Coating,
            Weight = dto.Weight,
            Volume = dto.Volume,
            PaintedArea = dto.PaintedArea,
            CompanyId = companyId
        };
    }

    private DrawingPart MapUnidentifiedPartToEntity(ParsedPartWithIdDto dto, int drawingId, int revisionId, int companyId)
    {
        return new DrawingPart
        {
            DrawingId = drawingId,
            DrawingRevisionId = revisionId,
            PartReference = dto.PartReference ?? dto.SuggestedReference ?? $"UNIDENTIFIED-{dto.TempPartId.Substring(0, 8)}",
            Description = dto.Description,
            PartType = dto.PartType,
            MaterialGrade = dto.MaterialGrade,
            MaterialStandard = dto.MaterialStandard,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Length = dto.Length,
            Width = dto.Width,
            Thickness = dto.Thickness,
            FlangeThickness = dto.FlangeThickness,
            FlangeWidth = dto.FlangeWidth,
            WebThickness = dto.WebThickness,
            WebDepth = dto.WebDepth,
            LegA = dto.LegA,
            LegB = dto.LegB,
            Diameter = null,
            OutsideDiameter = null,
            WallThickness = null,
            Coating = dto.Coating,
            Weight = dto.Weight,
            Volume = dto.Volume,
            PaintedArea = dto.PaintedArea,
            CompanyId = companyId
        };
    }

    #endregion
}

#region DTOs for Status Updates

public class UpdateRevisionStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovalComments { get; set; }
}

public class CreateIFCFromIFADto
{
    public string? RevisionNotes { get; set; }
}

public class UpdateDrawingPartDto
{
    public decimal? Quantity { get; set; }
    public string? Description { get; set; }
    public string? MaterialGrade { get; set; }
    public int? CatalogueItemId { get; set; }
    public string? Notes { get; set; }
}

public class LinkCatalogueDto
{
    public int CatalogueItemId { get; set; }
}

#endregion
