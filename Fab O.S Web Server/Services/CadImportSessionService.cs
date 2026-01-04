using FabOS.WebServer.Models.DTOs;
using FabOS.WebServer.Models.DTOs.QDocs;
using FabOS.WebServer.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FabOS.WebServer.Services;

/// <summary>
/// Service for managing CAD import sessions.
/// Stores pending import data in memory cache until user confirms with part mappings.
/// Now includes tenant isolation for multi-tenant security.
/// </summary>
public class CadImportSessionService : ICadImportSessionService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CadImportSessionService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _defaultExpiration;

    public CadImportSessionService(
        IMemoryCache cache,
        ILogger<CadImportSessionService> logger,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _configuration = configuration;

        // Make expiration configurable via appsettings.json
        var timeoutMinutes = _configuration.GetValue<int>("CadImport:SessionTimeoutMinutes", 30);
        _defaultExpiration = TimeSpan.FromMinutes(timeoutMinutes);
    }

    /// <summary>
    /// Create a new import session and store the parse result with tenant isolation
    /// </summary>
    public CadImportPreviewDto CreateSession(
        int drawingId,
        int drawingRevisionId,
        string fileName,
        CadParseResultDto parseResult,
        int companyId,
        int userId)
    {
        var sessionId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.Add(_defaultExpiration);

        // Create the session data with tenant information
        var session = new CadImportSessionData
        {
            ImportSessionId = sessionId,
            DrawingId = drawingId,
            DrawingRevisionId = drawingRevisionId,
            FileName = fileName,
            ParseResult = parseResult,
            CompanyId = companyId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        // Store in cache
        var cacheKey = GetCacheKey(sessionId);
        _cache.Set(cacheKey, session, _defaultExpiration);

        _logger.LogInformation(
            "Created CAD import session {SessionId} for drawing {DrawingId}, company {CompanyId}, expires at {ExpiresAt}",
            sessionId, drawingId, companyId, expiresAt);

        // Convert to preview DTO for API response
        return CreatePreviewDto(session);
    }

    /// <summary>
    /// Legacy overload for backward compatibility - should be deprecated
    /// </summary>
    [Obsolete("Use CreateSession with companyId and userId parameters for proper tenant isolation")]
    public CadImportPreviewDto CreateSession(
        int drawingId,
        int drawingRevisionId,
        string fileName,
        CadParseResultDto parseResult)
    {
        // Default to company 0 to trigger validation errors if not properly handled
        return CreateSession(drawingId, drawingRevisionId, fileName, parseResult, 0, 0);
    }

    /// <summary>
    /// Get an existing import session by ID with tenant validation
    /// </summary>
    public CadImportSessionData? GetSession(string sessionId, int companyId, int userId)
    {
        var cacheKey = GetCacheKey(sessionId);
        if (_cache.TryGetValue(cacheKey, out CadImportSessionData? session))
        {
            // Validate tenant ownership
            if (session!.CompanyId != companyId)
            {
                _logger.LogWarning(
                    "Unauthorized session access attempt. SessionId: {SessionId}, RequestedCompany: {RequestedCompany}, SessionCompany: {SessionCompany}",
                    sessionId, companyId, session.CompanyId);
                return null;
            }

            return session;
        }

        _logger.LogWarning("CAD import session not found or expired: {SessionId}", sessionId);
        return null;
    }

    /// <summary>
    /// Legacy overload - does NOT validate tenant (security risk, should be deprecated)
    /// </summary>
    [Obsolete("Use GetSession with companyId and userId parameters for proper tenant isolation")]
    public CadImportSessionData? GetSession(string sessionId)
    {
        var cacheKey = GetCacheKey(sessionId);
        if (_cache.TryGetValue(cacheKey, out CadImportSessionData? session))
        {
            return session;
        }

        _logger.LogWarning("CAD import session not found or expired: {SessionId}", sessionId);
        return null;
    }

    /// <summary>
    /// Get the preview DTO for an existing session with tenant validation
    /// </summary>
    public CadImportPreviewDto? GetPreview(string sessionId, int companyId, int userId)
    {
        var session = GetSession(sessionId, companyId, userId);
        if (session == null) return null;

        return CreatePreviewDto(session);
    }

    /// <summary>
    /// Legacy overload without tenant validation
    /// </summary>
    [Obsolete("Use GetPreview with companyId and userId parameters for proper tenant isolation")]
    public CadImportPreviewDto? GetPreview(string sessionId)
    {
#pragma warning disable CS0618 // Using obsolete method intentionally for backward compatibility
        var session = GetSession(sessionId);
#pragma warning restore CS0618
        if (session == null) return null;

        return CreatePreviewDto(session);
    }

    /// <summary>
    /// Apply user-provided part mappings to the session and return the final parse result
    /// </summary>
    public CadParseResultDto? ApplyMappings(string sessionId, ConfirmCadImportRequestDto request, int companyId, int userId)
    {
        var session = GetSession(sessionId, companyId, userId);
        if (session == null) return null;

        var parseResult = session.ParseResult;

        // Create dictionaries for quick lookup
        var partMappings = request.PartMappings.ToDictionary(m => m.TempPartId, m => m.PartReference);
        var assemblyMappings = request.AssemblyMappings.ToDictionary(m => m.TempAssemblyId, m => m);

        // Apply mappings to all parts
        foreach (var part in parseResult.AllParts)
        {
            if (!part.IsIdentified)
            {
                if (partMappings.TryGetValue(part.TempPartId, out var newReference))
                {
                    part.PartReference = newReference;
                    part.IsIdentified = true;
                    _logger.LogDebug("Applied mapping for part {TempId}: {NewReference}", part.TempPartId, newReference);
                }
                else if (request.AutoGenerateRemainingReferences)
                {
                    // Use the suggested reference if auto-generate is enabled
                    part.PartReference = part.SuggestedReference ?? $"AUTO-{part.TempPartId.Substring(0, 8)}";
                    part.IsIdentified = true;
                }
            }
        }

        // Apply mappings to assemblies
        foreach (var assembly in parseResult.Assemblies)
        {
            if (!assembly.IsIdentified)
            {
                if (assemblyMappings.TryGetValue(assembly.TempAssemblyId, out var mapping))
                {
                    assembly.AssemblyMark = mapping.AssemblyMark;
                    assembly.AssemblyName = mapping.AssemblyName ?? mapping.AssemblyMark;
                    assembly.IsIdentified = true;
                }
            }

            // Move unidentified parts to identified if they now have references
            var nowIdentified = assembly.UnidentifiedParts
                .Where(p => p.IsIdentified)
                .ToList();

            foreach (var part in nowIdentified)
            {
                assembly.UnidentifiedParts.Remove(part);
                assembly.IdentifiedParts.Add(part);
            }
        }

        // Update counts
        parseResult.IdentifiedPartCount = parseResult.AllParts.Count(p => p.IsIdentified);
        parseResult.UnidentifiedPartCount = parseResult.AllParts.Count(p => !p.IsIdentified);

        _logger.LogInformation(
            "Applied mappings to session {SessionId}: {Identified} identified, {Unidentified} remaining",
            sessionId, parseResult.IdentifiedPartCount, parseResult.UnidentifiedPartCount);

        return parseResult;
    }

    /// <summary>
    /// Legacy overload without tenant validation
    /// </summary>
    [Obsolete("Use ApplyMappings with companyId and userId parameters for proper tenant isolation")]
    public CadParseResultDto? ApplyMappings(string sessionId, ConfirmCadImportRequestDto request)
    {
#pragma warning disable CS0618
        var session = GetSession(sessionId);
#pragma warning restore CS0618
        if (session == null) return null;

        return ApplyMappings(sessionId, request, session.CompanyId, session.UserId);
    }

    /// <summary>
    /// Remove a session from cache (after successful import or cancellation)
    /// </summary>
    public void RemoveSession(string sessionId, int companyId)
    {
        // Validate ownership before removal
        var session = GetSession(sessionId, companyId, 0);
        if (session != null)
        {
            var cacheKey = GetCacheKey(sessionId);
            _cache.Remove(cacheKey);
            _logger.LogInformation("Removed CAD import session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Legacy overload without tenant validation
    /// </summary>
    [Obsolete("Use RemoveSession with companyId parameter for proper tenant isolation")]
    public void RemoveSession(string sessionId)
    {
        var cacheKey = GetCacheKey(sessionId);
        _cache.Remove(cacheKey);
        _logger.LogInformation("Removed CAD import session {SessionId}", sessionId);
    }

    private string GetCacheKey(string sessionId) => $"CadImportSession:{sessionId}";

    private CadImportPreviewDto CreatePreviewDto(CadImportSessionData session)
    {
        var parseResult = session.ParseResult;

        // Determine status
        string status;
        if (parseResult.UnidentifiedPartCount > 0)
            status = "PendingReview";
        else
            status = "Ready";

        // Convert assemblies to preview format
        var assemblies = parseResult.Assemblies.Select(a => new CadImportAssemblyPreviewDto
        {
            TempAssemblyId = a.TempAssemblyId,
            AssemblyMark = a.AssemblyMark,
            AssemblyName = a.AssemblyName,
            NeedsIdentification = !a.IsIdentified,
            SuggestedAssemblyMark = a.SuggestedAssemblyMark,
            TotalWeight = a.TotalWeight,
            TotalPartCount = a.IdentifiedParts.Count + a.UnidentifiedParts.Count,
            IdentifiedParts = a.IdentifiedParts.Select(p => new IdentifiedPartPreviewDto
            {
                TempPartId = p.TempPartId,
                PartReference = p.PartReference,
                Description = p.Description,
                PartType = p.PartType,
                MaterialGrade = p.MaterialGrade,
                Profile = p.Description,
                Weight = p.Weight,
                Quantity = (int)p.Quantity,
                AssemblyMark = p.AssemblyMark
            }).ToList(),
            UnidentifiedParts = a.UnidentifiedParts.Select(p => new UnidentifiedPartDto
            {
                TempPartId = p.TempPartId,
                PartType = p.PartType,
                Profile = p.Description,
                Description = p.Description,
                MaterialGrade = p.MaterialGrade,
                ObjectType = p.IfcObjectType,
                ElementName = p.IfcElementName,
                Length = p.Length,
                Width = p.Width,
                Thickness = p.Thickness,
                Weight = p.Weight,
                TempAssemblyId = p.TempAssemblyId,
                AssemblyMark = p.AssemblyMark,
                Quantity = (int)p.Quantity,
                SuggestedReference = p.SuggestedReference
            }).ToList()
        }).ToList();

        // Convert loose parts (unidentified)
        var unidentifiedLooseParts = parseResult.LooseParts
            .Where(p => !p.IsIdentified)
            .Select(p => new UnidentifiedPartDto
            {
                TempPartId = p.TempPartId,
                PartType = p.PartType,
                Profile = p.Description,
                Description = p.Description,
                MaterialGrade = p.MaterialGrade,
                ObjectType = p.IfcObjectType,
                ElementName = p.IfcElementName,
                Length = p.Length,
                Width = p.Width,
                Thickness = p.Thickness,
                Weight = p.Weight,
                Quantity = (int)p.Quantity,
                SuggestedReference = p.SuggestedReference
            }).ToList();

        return new CadImportPreviewDto
        {
            ImportSessionId = session.ImportSessionId,
            DrawingId = session.DrawingId,
            DrawingRevisionId = session.DrawingRevisionId,
            FileName = session.FileName,
            FileType = parseResult.FileType,
            Status = status,
            TotalElementCount = parseResult.TotalElementCount,
            IdentifiedPartCount = parseResult.IdentifiedPartCount,
            UnidentifiedPartCount = parseResult.UnidentifiedPartCount,
            AssemblyCount = parseResult.AssemblyCount,
            Assemblies = assemblies,
            UnidentifiedParts = unidentifiedLooseParts,
            ParsedDate = session.CreatedAt,
            ExpiresAt = session.ExpiresAt
        };
    }
}

/// <summary>
/// Internal data structure for storing import session in cache
/// Includes tenant information for multi-tenant isolation
/// </summary>
public class CadImportSessionData
{
    public string ImportSessionId { get; set; } = string.Empty;
    public int DrawingId { get; set; }
    public int DrawingRevisionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public CadParseResultDto ParseResult { get; set; } = new();

    /// <summary>
    /// Company ID for tenant isolation - sessions can only be accessed by matching company
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// User ID who created the session - for audit purposes
    /// </summary>
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
