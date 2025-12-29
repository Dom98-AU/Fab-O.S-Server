using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Services.Interfaces.Estimate;
using System.Security.Claims;

namespace FabOS.WebServer.Controllers.Api.Estimate;

/// <summary>
/// API Controller for managing Estimation Revisions.
/// Base endpoint: /api/{tenantSlug}/estimate/estimations/{estimationId}/revisions
/// </summary>
[Route("api/{tenantSlug}/estimate/estimations/{estimationId:int}/revisions")]
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
public class EstimationRevisionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEstimationCalculationService _calculationService;
    private readonly ILogger<EstimationRevisionsController> _logger;

    public EstimationRevisionsController(
        ApplicationDbContext context,
        IEstimationCalculationService calculationService,
        ILogger<EstimationRevisionsController> logger)
    {
        _context = context;
        _calculationService = calculationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all revisions for an estimation.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RevisionDetailDto>>> GetRevisions(
        string tenantSlug,
        int estimationId)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var revisions = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                .Where(r => r.EstimationId == estimationId && r.CompanyId == companyId && !r.IsDeleted)
                .OrderBy(r => r.RevisionLetter)
                .Select(r => new RevisionDetailDto
                {
                    Id = r.Id,
                    EstimationId = r.EstimationId,
                    RevisionLetter = r.RevisionLetter,
                    Status = r.Status,
                    Notes = r.Notes,
                    TotalMaterialCost = r.TotalMaterialCost,
                    TotalLaborHours = r.TotalLaborHours,
                    TotalLaborCost = r.TotalLaborCost,
                    Subtotal = r.Subtotal,
                    OverheadPercentage = r.OverheadPercentage,
                    OverheadAmount = r.OverheadAmount,
                    MarginPercentage = r.MarginPercentage,
                    MarginAmount = r.MarginAmount,
                    TotalAmount = r.TotalAmount,
                    ValidUntilDate = r.ValidUntilDate,
                    PackageCount = r.Packages.Count(p => !p.IsDeleted),
                    CreatedDate = r.CreatedDate,
                    SubmittedDate = r.SubmittedDate,
                    ApprovedDate = r.ApprovedDate
                })
                .ToListAsync();

            return Ok(revisions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revisions for estimation {EstimationId}", estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific revision by letter.
    /// </summary>
    [HttpGet("{letter}")]
    public async Task<ActionResult<RevisionDetailDto>> GetRevision(
        string tenantSlug,
        int estimationId,
        string letter)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var revision = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Worksheets.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r =>
                    r.EstimationId == estimationId &&
                    r.RevisionLetter == letter.ToUpper() &&
                    r.CompanyId == companyId &&
                    !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {letter} not found");

            var dto = new RevisionDetailDto
            {
                Id = revision.Id,
                EstimationId = revision.EstimationId,
                RevisionLetter = revision.RevisionLetter,
                Status = revision.Status,
                Notes = revision.Notes,
                TotalMaterialCost = revision.TotalMaterialCost,
                TotalLaborHours = revision.TotalLaborHours,
                TotalLaborCost = revision.TotalLaborCost,
                Subtotal = revision.Subtotal,
                OverheadPercentage = revision.OverheadPercentage,
                OverheadAmount = revision.OverheadAmount,
                MarginPercentage = revision.MarginPercentage,
                MarginAmount = revision.MarginAmount,
                TotalAmount = revision.TotalAmount,
                ValidUntilDate = revision.ValidUntilDate,
                PackageCount = revision.Packages.Count(p => !p.IsDeleted),
                CreatedDate = revision.CreatedDate,
                SubmittedDate = revision.SubmittedDate,
                ApprovedDate = revision.ApprovedDate,
                Packages = revision.Packages.Where(p => !p.IsDeleted).Select(p => new PackageSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PackageTotal = p.PackageTotal,
                    WorksheetCount = p.Worksheets.Count(w => !w.IsDeleted)
                }).ToList()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revision {Letter} for estimation {EstimationId}",
                letter, estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new revision (automatically uses next letter).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RevisionDetailDto>> CreateRevision(
        string tenantSlug,
        int estimationId,
        [FromBody] CreateRevisionRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            // Verify estimation exists
            var estimation = await _context.Estimations
                .Include(e => e.Revisions.Where(r => !r.IsDeleted))
                .FirstOrDefaultAsync(e => e.Id == estimationId && e.CompanyId == companyId && !e.IsDeleted);

            if (estimation == null)
                return NotFound($"Estimation {estimationId} not found");

            // Get next revision letter
            var lastLetter = estimation.Revisions
                .OrderByDescending(r => r.RevisionLetter)
                .Select(r => r.RevisionLetter)
                .FirstOrDefault() ?? "@"; // @ is before A in ASCII

            var nextLetter = ((char)(lastLetter[0] + 1)).ToString();

            // Supersede previous revision
            var previousRevision = estimation.Revisions
                .FirstOrDefault(r => r.RevisionLetter == lastLetter);

            if (previousRevision != null)
            {
                previousRevision.SupersededBy = nextLetter;
                previousRevision.ModifiedDate = DateTime.UtcNow;
            }

            var revision = new EstimationRevision
            {
                CompanyId = companyId.Value,
                EstimationId = estimationId,
                RevisionLetter = nextLetter,
                SupersedesLetter = lastLetter != "@" ? lastLetter : null,
                Status = "Draft",
                Notes = request.Notes,
                ValidUntilDate = request.ValidUntilDate ?? DateTime.UtcNow.AddDays(30),
                OverheadPercentage = request.OverheadPercentage ?? previousRevision?.OverheadPercentage ?? 15m,
                MarginPercentage = request.MarginPercentage ?? previousRevision?.MarginPercentage ?? 20m,
                CreatedBy = userId.Value,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationRevisions.Add(revision);
            await _context.SaveChangesAsync();

            // Copy packages from previous revision if requested
            if (request.CopyFromPrevious && previousRevision != null)
            {
                await CopyPackagesFromRevision(previousRevision.Id, revision.Id, userId.Value);
            }

            // Update estimation current revision
            estimation.CurrentRevisionLetter = nextLetter;
            estimation.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created revision {Letter} for estimation {EstimationId}",
                nextLetter, estimationId);

            // Reload revision with packages for response
            var createdRevision = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Worksheets.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == revision.Id);

            var responseDto = new RevisionDetailDto
            {
                Id = createdRevision!.Id,
                EstimationId = createdRevision.EstimationId,
                RevisionLetter = createdRevision.RevisionLetter,
                Status = createdRevision.Status,
                Notes = createdRevision.Notes,
                TotalMaterialCost = createdRevision.TotalMaterialCost,
                TotalLaborHours = createdRevision.TotalLaborHours,
                TotalLaborCost = createdRevision.TotalLaborCost,
                Subtotal = createdRevision.Subtotal,
                OverheadPercentage = createdRevision.OverheadPercentage,
                OverheadAmount = createdRevision.OverheadAmount,
                MarginPercentage = createdRevision.MarginPercentage,
                MarginAmount = createdRevision.MarginAmount,
                TotalAmount = createdRevision.TotalAmount,
                ValidUntilDate = createdRevision.ValidUntilDate,
                PackageCount = createdRevision.Packages.Count(p => !p.IsDeleted),
                CreatedDate = createdRevision.CreatedDate,
                SubmittedDate = createdRevision.SubmittedDate,
                ApprovedDate = createdRevision.ApprovedDate,
                Packages = createdRevision.Packages.Where(p => !p.IsDeleted).Select(p => new PackageSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PackageTotal = p.PackageTotal,
                    WorksheetCount = p.Worksheets.Count(w => !w.IsDeleted)
                }).ToList()
            };

            return CreatedAtAction(nameof(GetRevision),
                new { tenantSlug, estimationId, letter = nextLetter }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating revision for estimation {EstimationId}", estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update revision settings (overhead, margin, notes).
    /// </summary>
    [HttpPut("{letter}")]
    public async Task<ActionResult<RevisionDetailDto>> UpdateRevision(
        string tenantSlug,
        int estimationId,
        string letter,
        [FromBody] UpdateRevisionRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var revision = await _context.EstimationRevisions
                .FirstOrDefaultAsync(r =>
                    r.EstimationId == estimationId &&
                    r.RevisionLetter == letter.ToUpper() &&
                    r.CompanyId == companyId &&
                    !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {letter} not found");

            if (revision.Status != "Draft")
                return BadRequest("Can only modify draft revisions");

            // Update fields
            revision.Notes = request.Notes ?? revision.Notes;
            revision.ValidUntilDate = request.ValidUntilDate ?? revision.ValidUntilDate;
            revision.OverheadPercentage = request.OverheadPercentage ?? revision.OverheadPercentage;
            revision.MarginPercentage = request.MarginPercentage ?? revision.MarginPercentage;
            revision.ModifiedBy = userId.Value;
            revision.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate totals if percentages changed
            if (request.OverheadPercentage.HasValue || request.MarginPercentage.HasValue)
            {
                await _calculationService.RecalculateRevisionAsync(revision.Id);
            }

            // Reload revision with packages for response
            var updatedRevision = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Worksheets.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == revision.Id);

            var responseDto = new RevisionDetailDto
            {
                Id = updatedRevision!.Id,
                EstimationId = updatedRevision.EstimationId,
                RevisionLetter = updatedRevision.RevisionLetter,
                Status = updatedRevision.Status,
                Notes = updatedRevision.Notes,
                TotalMaterialCost = updatedRevision.TotalMaterialCost,
                TotalLaborHours = updatedRevision.TotalLaborHours,
                TotalLaborCost = updatedRevision.TotalLaborCost,
                Subtotal = updatedRevision.Subtotal,
                OverheadPercentage = updatedRevision.OverheadPercentage,
                OverheadAmount = updatedRevision.OverheadAmount,
                MarginPercentage = updatedRevision.MarginPercentage,
                MarginAmount = updatedRevision.MarginAmount,
                TotalAmount = updatedRevision.TotalAmount,
                ValidUntilDate = updatedRevision.ValidUntilDate,
                PackageCount = updatedRevision.Packages.Count(p => !p.IsDeleted),
                CreatedDate = updatedRevision.CreatedDate,
                SubmittedDate = updatedRevision.SubmittedDate,
                ApprovedDate = updatedRevision.ApprovedDate,
                Packages = updatedRevision.Packages.Where(p => !p.IsDeleted).Select(p => new PackageSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PackageTotal = p.PackageTotal,
                    WorksheetCount = p.Worksheets.Count(w => !w.IsDeleted)
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating revision {Letter} for estimation {EstimationId}",
                letter, estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Submit revision for review.
    /// </summary>
    [HttpPost("{letter}/submit")]
    public async Task<ActionResult<RevisionDetailDto>> SubmitForReview(
        string tenantSlug,
        int estimationId,
        string letter)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var revision = await _context.EstimationRevisions
                .FirstOrDefaultAsync(r =>
                    r.EstimationId == estimationId &&
                    r.RevisionLetter == letter.ToUpper() &&
                    r.CompanyId == companyId &&
                    !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {letter} not found");

            if (revision.Status != "Draft")
                return BadRequest($"Revision must be in Draft status to submit. Current status: {revision.Status}");

            revision.Status = "SubmittedForReview";
            revision.SubmittedBy = userId.Value;
            revision.SubmittedDate = DateTime.UtcNow;
            revision.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Submitted revision {Letter} for review on estimation {EstimationId}",
                letter, estimationId);

            // Reload revision with packages for response
            var submittedRevision = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Worksheets.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == revision.Id);

            var responseDto = new RevisionDetailDto
            {
                Id = submittedRevision!.Id,
                EstimationId = submittedRevision.EstimationId,
                RevisionLetter = submittedRevision.RevisionLetter,
                Status = submittedRevision.Status,
                Notes = submittedRevision.Notes,
                TotalMaterialCost = submittedRevision.TotalMaterialCost,
                TotalLaborHours = submittedRevision.TotalLaborHours,
                TotalLaborCost = submittedRevision.TotalLaborCost,
                Subtotal = submittedRevision.Subtotal,
                OverheadPercentage = submittedRevision.OverheadPercentage,
                OverheadAmount = submittedRevision.OverheadAmount,
                MarginPercentage = submittedRevision.MarginPercentage,
                MarginAmount = submittedRevision.MarginAmount,
                TotalAmount = submittedRevision.TotalAmount,
                ValidUntilDate = submittedRevision.ValidUntilDate,
                PackageCount = submittedRevision.Packages.Count(p => !p.IsDeleted),
                CreatedDate = submittedRevision.CreatedDate,
                SubmittedDate = submittedRevision.SubmittedDate,
                ApprovedDate = submittedRevision.ApprovedDate,
                Packages = submittedRevision.Packages.Where(p => !p.IsDeleted).Select(p => new PackageSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PackageTotal = p.PackageTotal,
                    WorksheetCount = p.Worksheets.Count(w => !w.IsDeleted)
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting revision {Letter} for estimation {EstimationId}",
                letter, estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Approve a revision.
    /// </summary>
    [HttpPost("{letter}/approve")]
    public async Task<ActionResult<RevisionDetailDto>> ApproveRevision(
        string tenantSlug,
        int estimationId,
        string letter,
        [FromBody] ApprovalRequest? request = null)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var revision = await _context.EstimationRevisions
                .FirstOrDefaultAsync(r =>
                    r.EstimationId == estimationId &&
                    r.RevisionLetter == letter.ToUpper() &&
                    r.CompanyId == companyId &&
                    !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {letter} not found");

            if (revision.Status != "SubmittedForReview" && revision.Status != "InReview")
                return BadRequest($"Revision must be submitted for review to approve. Current status: {revision.Status}");

            revision.Status = "Approved";
            revision.ApprovedBy = userId.Value;
            revision.ApprovedDate = DateTime.UtcNow;
            revision.ApprovalComments = request?.Comments;
            revision.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Approved revision {Letter} for estimation {EstimationId}",
                letter, estimationId);

            // Reload revision with packages for response
            var approvedRevision = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Worksheets.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == revision.Id);

            var responseDto = new RevisionDetailDto
            {
                Id = approvedRevision!.Id,
                EstimationId = approvedRevision.EstimationId,
                RevisionLetter = approvedRevision.RevisionLetter,
                Status = approvedRevision.Status,
                Notes = approvedRevision.Notes,
                TotalMaterialCost = approvedRevision.TotalMaterialCost,
                TotalLaborHours = approvedRevision.TotalLaborHours,
                TotalLaborCost = approvedRevision.TotalLaborCost,
                Subtotal = approvedRevision.Subtotal,
                OverheadPercentage = approvedRevision.OverheadPercentage,
                OverheadAmount = approvedRevision.OverheadAmount,
                MarginPercentage = approvedRevision.MarginPercentage,
                MarginAmount = approvedRevision.MarginAmount,
                TotalAmount = approvedRevision.TotalAmount,
                ValidUntilDate = approvedRevision.ValidUntilDate,
                PackageCount = approvedRevision.Packages.Count(p => !p.IsDeleted),
                CreatedDate = approvedRevision.CreatedDate,
                SubmittedDate = approvedRevision.SubmittedDate,
                ApprovedDate = approvedRevision.ApprovedDate,
                Packages = approvedRevision.Packages.Where(p => !p.IsDeleted).Select(p => new PackageSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PackageTotal = p.PackageTotal,
                    WorksheetCount = p.Worksheets.Count(w => !w.IsDeleted)
                }).ToList()
            };

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving revision {Letter} for estimation {EstimationId}",
                letter, estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reject a revision.
    /// </summary>
    [HttpPost("{letter}/reject")]
    public async Task<ActionResult<RevisionDetailDto>> RejectRevision(
        string tenantSlug,
        int estimationId,
        string letter,
        [FromBody] RejectionRequest request)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            if (companyId == null || userId == null)
                return Unauthorized("Company or user context not found");

            var revision = await _context.EstimationRevisions
                .FirstOrDefaultAsync(r =>
                    r.EstimationId == estimationId &&
                    r.RevisionLetter == letter.ToUpper() &&
                    r.CompanyId == companyId &&
                    !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {letter} not found");

            if (revision.Status != "SubmittedForReview" && revision.Status != "InReview")
                return BadRequest($"Revision must be submitted for review to reject. Current status: {revision.Status}");

            revision.Status = "Rejected";
            revision.ReviewedBy = userId.Value;
            revision.ReviewedDate = DateTime.UtcNow;
            revision.RejectionReason = request.Reason;
            revision.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Rejected revision {Letter} for estimation {EstimationId}: {Reason}",
                letter, estimationId, request.Reason);

            // Reload revision with packages for response
            var rejectedRevision = await _context.EstimationRevisions
                .Include(r => r.Packages.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Worksheets.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == revision.Id);

            var rejectDto = new RevisionDetailDto
            {
                Id = rejectedRevision!.Id,
                EstimationId = rejectedRevision.EstimationId,
                RevisionLetter = rejectedRevision.RevisionLetter,
                Status = rejectedRevision.Status,
                Notes = rejectedRevision.Notes,
                TotalMaterialCost = rejectedRevision.TotalMaterialCost,
                TotalLaborHours = rejectedRevision.TotalLaborHours,
                TotalLaborCost = rejectedRevision.TotalLaborCost,
                Subtotal = rejectedRevision.Subtotal,
                OverheadPercentage = rejectedRevision.OverheadPercentage,
                OverheadAmount = rejectedRevision.OverheadAmount,
                MarginPercentage = rejectedRevision.MarginPercentage,
                MarginAmount = rejectedRevision.MarginAmount,
                TotalAmount = rejectedRevision.TotalAmount,
                ValidUntilDate = rejectedRevision.ValidUntilDate,
                PackageCount = rejectedRevision.Packages.Count(p => !p.IsDeleted),
                CreatedDate = rejectedRevision.CreatedDate,
                SubmittedDate = rejectedRevision.SubmittedDate,
                ApprovedDate = rejectedRevision.ApprovedDate,
                Packages = rejectedRevision.Packages.Where(p => !p.IsDeleted).Select(p => new PackageSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PackageTotal = p.PackageTotal,
                    WorksheetCount = p.Worksheets.Count(w => !w.IsDeleted)
                }).ToList()
            };

            return Ok(rejectDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting revision {Letter} for estimation {EstimationId}",
                letter, estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Recalculate revision totals.
    /// </summary>
    [HttpPost("{letter}/recalculate")]
    public async Task<ActionResult<RevisionSummary>> RecalculateRevision(
        string tenantSlug,
        int estimationId,
        string letter)
    {
        try
        {
            var companyId = GetCompanyId();
            if (companyId == null)
                return Unauthorized("Company context not found");

            var revision = await _context.EstimationRevisions
                .FirstOrDefaultAsync(r =>
                    r.EstimationId == estimationId &&
                    r.RevisionLetter == letter.ToUpper() &&
                    r.CompanyId == companyId &&
                    !r.IsDeleted);

            if (revision == null)
                return NotFound($"Revision {letter} not found");

            await _calculationService.RecalculateRevisionAsync(revision.Id);
            var summary = await _calculationService.GetRevisionSummaryAsync(revision.Id);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating revision {Letter} for estimation {EstimationId}",
                letter, estimationId);
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Helpers

    private int? GetCompanyId()
    {
        var claim = User.FindFirst("company_id");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private async Task CopyPackagesFromRevision(int sourceRevisionId, int targetRevisionId, int userId)
    {
        var sourcePackages = await _context.EstimationRevisionPackages
            .Include(p => p.Worksheets.Where(w => !w.IsDeleted))
                .ThenInclude(w => w.Columns.Where(c => !c.IsDeleted))
            .Include(p => p.Worksheets.Where(w => !w.IsDeleted))
                .ThenInclude(w => w.Rows.Where(r => !r.IsDeleted))
            .Where(p => p.RevisionId == sourceRevisionId && !p.IsDeleted)
            .ToListAsync();

        foreach (var sourcePackage in sourcePackages)
        {
            var newPackage = new EstimationRevisionPackage
            {
                CompanyId = sourcePackage.CompanyId,
                RevisionId = targetRevisionId,
                Name = sourcePackage.Name,
                Description = sourcePackage.Description,
                SortOrder = sourcePackage.SortOrder,
                OverheadPercentage = sourcePackage.OverheadPercentage,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.EstimationRevisionPackages.Add(newPackage);
            await _context.SaveChangesAsync();

            // Copy worksheets
            foreach (var sourceWorksheet in sourcePackage.Worksheets.Where(w => !w.IsDeleted))
            {
                var newWorksheet = new EstimationWorksheet
                {
                    CompanyId = sourceWorksheet.CompanyId,
                    PackageId = newPackage.Id,
                    TemplateId = sourceWorksheet.TemplateId,
                    Name = sourceWorksheet.Name,
                    Description = sourceWorksheet.Description,
                    WorksheetType = sourceWorksheet.WorksheetType,
                    SortOrder = sourceWorksheet.SortOrder,
                    CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.EstimationWorksheets.Add(newWorksheet);
                await _context.SaveChangesAsync();

                // Copy columns (using EstimationWorksheetInstanceColumn)
                foreach (var sourceColumn in sourceWorksheet.Columns.Where(c => !c.IsDeleted))
                {
                    var newColumn = new EstimationWorksheetInstanceColumn
                    {
                        WorksheetId = newWorksheet.Id,
                        ColumnKey = sourceColumn.ColumnKey,
                        ColumnName = sourceColumn.ColumnName,
                        DataType = sourceColumn.DataType,
                        Width = sourceColumn.Width,
                        IsRequired = sourceColumn.IsRequired,
                        IsReadOnly = sourceColumn.IsReadOnly,
                        IsFrozen = sourceColumn.IsFrozen,
                        IsHidden = sourceColumn.IsHidden,
                        Formula = sourceColumn.Formula,
                        DefaultValue = sourceColumn.DefaultValue,
                        SelectOptions = sourceColumn.SelectOptions,
                        Precision = sourceColumn.Precision,
                        SortOrder = sourceColumn.SortOrder,
                        LinkToCatalogue = sourceColumn.LinkToCatalogue,
                        CatalogueField = sourceColumn.CatalogueField,
                        AutoPopulateFromCatalogue = sourceColumn.AutoPopulateFromCatalogue
                    };

                    _context.EstimationWorksheetInstanceColumns.Add(newColumn);
                }

                // Copy rows
                foreach (var sourceRow in sourceWorksheet.Rows.Where(r => !r.IsDeleted))
                {
                    var newRow = new EstimationWorksheetRow
                    {
                        CompanyId = sourceWorksheet.CompanyId,
                        WorksheetId = newWorksheet.Id,
                        RowData = sourceRow.RowData,
                        SortOrder = sourceRow.SortOrder,
                        IsGroupHeader = sourceRow.IsGroupHeader,
                        GroupName = sourceRow.GroupName,
                        ParentRowId = sourceRow.ParentRowId,
                        CalculatedTotal = sourceRow.CalculatedTotal,
                        CreatedBy = userId,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.EstimationWorksheetRows.Add(newRow);
                }

                await _context.SaveChangesAsync();
            }
        }
    }

    #endregion
}

#region DTOs

public class RevisionDetailDto
{
    public int Id { get; set; }
    public int EstimationId { get; set; }
    public string RevisionLetter { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalLaborHours { get; set; }
    public decimal TotalLaborCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal OverheadPercentage { get; set; }
    public decimal OverheadAmount { get; set; }
    public decimal MarginPercentage { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? ValidUntilDate { get; set; }
    public int PackageCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public IList<PackageSummaryDto> Packages { get; set; } = new List<PackageSummaryDto>();
}

public class PackageSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PackageTotal { get; set; }
    public int WorksheetCount { get; set; }
}

public class CreateRevisionRequest
{
    public string? Notes { get; set; }
    public DateTime? ValidUntilDate { get; set; }
    public decimal? OverheadPercentage { get; set; }
    public decimal? MarginPercentage { get; set; }
    public bool CopyFromPrevious { get; set; } = true;
}

public class UpdateRevisionRequest
{
    public string? Notes { get; set; }
    public DateTime? ValidUntilDate { get; set; }
    public decimal? OverheadPercentage { get; set; }
    public decimal? MarginPercentage { get; set; }
}

public class ApprovalRequest
{
    public string? Comments { get; set; }
}

public class RejectionRequest
{
    public string Reason { get; set; } = string.Empty;
}

#endregion
