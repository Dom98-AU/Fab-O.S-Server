using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Models.Entities;
using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Controllers;

/// <summary>
/// API controller for QDocs module - drawings, revisions, and CAD file uploads
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QDocsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISmlxParserService _smlxParser;
    private readonly IIfcParserService _ifcParser;
    private readonly ILogger<QDocsController> _logger;

    public QDocsController(
        ApplicationDbContext context,
        ISmlxParserService smlxParser,
        IIfcParserService ifcParser,
        ILogger<QDocsController> logger)
    {
        _context = context;
        _smlxParser = smlxParser;
        _ifcParser = ifcParser;
        _logger = logger;
    }

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
                    Description = null, // ParsedAssemblyDto doesn't have Description
                    TotalWeight = assemblyDto.TotalWeight,
                    PartCount = assemblyDto.Parts.Count,
                    CompanyId = companyId,
                    CreatedDate = DateTime.UtcNow
                };

                await _context.DrawingAssemblies.AddAsync(assembly);
                await _context.SaveChangesAsync(); // Save to get assembly ID

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

            // Save loose parts (parts not in any assembly)
            foreach (var partDto in parsedLooseParts)
            {
                var part = MapParsedPartToEntity(partDto, drawingId, revisionId, companyId);
                part.ParentAssemblyId = null; // Loose part
                part.AssemblyMark = null;
                part.AssemblyName = null;

                await _context.DrawingParts.AddAsync(part);
                partsSaved++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("CAD upload complete: Saved {AssemblyCount} assemblies and {PartCount} parts",
                assembliesSaved, partsSaved);

            // Return result
            return Ok(new DrawingUploadResultDto
            {
                DrawingId = drawingId,
                DrawingRevisionId = revisionId,
                DrawingFileId = 0, // We're not creating DrawingFile record yet
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
            return StatusCode(500, new { message = "Error retrieving assemblies", error = ex.Message });
        }
    }

    /// <summary>
    /// Get loose parts (parts not in any assembly) for a drawing revision
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
            return StatusCode(500, new { message = "Error retrieving loose parts", error = ex.Message });
        }
    }

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

            // Geometry fields (ParsedPartDto only has these 9 fields)
            Length = dto.Length,
            Width = dto.Width,
            Thickness = dto.Thickness,
            FlangeThickness = dto.FlangeThickness,
            FlangeWidth = dto.FlangeWidth,
            WebThickness = dto.WebThickness,
            WebDepth = dto.WebDepth,
            LegA = dto.LegA,
            LegB = dto.LegB,

            // Missing in ParsedPartDto: Diameter, OutsideDiameter, WallThickness
            Diameter = null,
            OutsideDiameter = null,
            WallThickness = null,

            // Assembly tracking fields
            Coating = dto.Coating,
            Weight = dto.Weight,
            Volume = dto.Volume,
            PaintedArea = dto.PaintedArea,

            CompanyId = companyId
        };
    }

    private DrawingPartDto MapEntityToPartDto(DrawingPart entity)
    {
        return new DrawingPartDto
        {
            Id = entity.Id,
            PartReference = entity.PartReference,
            Description = entity.Description,
            PartType = entity.PartType,
            MaterialGrade = entity.MaterialGrade,
            MaterialStandard = entity.MaterialStandard,
            Quantity = entity.Quantity,
            Unit = entity.Unit,

            // Geometry
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

            // Assembly
            AssemblyMark = entity.AssemblyMark,
            AssemblyName = entity.AssemblyName,
            ParentAssemblyId = entity.ParentAssemblyId,
            Coating = entity.Coating,
            Weight = entity.Weight,
            Volume = entity.Volume,
            PaintedArea = entity.PaintedArea
        };
    }

    #endregion
}
