namespace FabOS.WebServer.Models.Constants;

/// <summary>
/// Constants for the QDocs module to replace magic strings
/// </summary>
public static class QDocsConstants
{
    /// <summary>
    /// Drawing stages in the IFA → IFC workflow
    /// </summary>
    public static class DrawingStage
    {
        public const string IFA = "IFA";
        public const string IFC = "IFC";
        public const string Superseded = "Superseded";

        public static readonly string[] ValidStages = { IFA, IFC, Superseded };
    }

    /// <summary>
    /// Revision status values for workflow management
    /// </summary>
    public static class RevisionStatus
    {
        public const string Draft = "Draft";
        public const string UnderReview = "UnderReview";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Superseded = "Superseded";

        public static readonly string[] ValidStatuses = { Draft, UnderReview, Approved, Rejected, Superseded };

        /// <summary>
        /// Valid status transitions for workflow enforcement
        /// </summary>
        public static readonly Dictionary<string, string[]> ValidTransitions = new()
        {
            { Draft, new[] { UnderReview, Approved } },
            { UnderReview, new[] { Approved, Rejected } },
            { Rejected, new[] { Draft, UnderReview } },
            { Approved, new[] { Superseded } }
        };

        public static bool IsValidTransition(string fromStatus, string toStatus)
        {
            if (ValidTransitions.TryGetValue(fromStatus, out var allowedStatuses))
            {
                return allowedStatuses.Contains(toStatus);
            }
            return false;
        }
    }

    /// <summary>
    /// Revision type values
    /// </summary>
    public static class RevisionType
    {
        public const string IFA = "IFA";
        public const string IFC = "IFC";

        public static readonly string[] ValidTypes = { IFA, IFC };
    }

    /// <summary>
    /// Part types for structural elements
    /// </summary>
    public static class PartType
    {
        public const string Beam = "Beam";
        public const string Plate = "Plate";
        public const string Member = "Member";
        public const string Column = "Column";
        public const string Footing = "Footing";
        public const string Slab = "Slab";
        public const string Pile = "Pile";
        public const string Misc = "Misc";
        public const string Bolt = "Bolt";
        public const string Nut = "Nut";
        public const string Washer = "Washer";
        public const string Weld = "Weld";
        public const string Unknown = "Unknown";

        public static readonly string[] StructuralTypes = { Beam, Plate, Member, Column, Footing, Slab, Pile };
        public static readonly string[] FastenerTypes = { Bolt, Nut, Washer };
    }

    /// <summary>
    /// File parse status values
    /// </summary>
    public static class ParseStatus
    {
        public const string Pending = "Pending";
        public const string Parsing = "Parsing";
        public const string Completed = "Completed";
        public const string Failed = "Failed";

        public static readonly string[] ValidStatuses = { Pending, Parsing, Completed, Failed };
    }

    /// <summary>
    /// CAD file types supported for import
    /// </summary>
    public static class FileType
    {
        public const string SMLX = "SMLX";
        public const string IFC = "IFC";
        public const string DXF = "DXF";
        public const string STEP = "STEP";
        public const string NC = "NC";
        public const string PDF = "PDF";

        public static readonly string[] CadTypes = { SMLX, IFC, DXF, STEP, NC };
        public static readonly string[] AllSupportedTypes = { SMLX, IFC, DXF, STEP, NC, PDF };
    }

    /// <summary>
    /// CAD import session status values
    /// </summary>
    public static class ImportSessionStatus
    {
        public const string Ready = "Ready";
        public const string PendingReview = "PendingReview";
        public const string Failed = "Failed";
    }

    /// <summary>
    /// Units used in QDocs
    /// </summary>
    public static class Unit
    {
        public const string Each = "EA";
        public const string Kilogram = "KG";
        public const string Meter = "M";
        public const string SquareMeter = "M2";
        public const string CubicMeter = "M3";
        public const string Length = "LEN";

        public static readonly string[] ValidUnits = { Each, Kilogram, Meter, SquareMeter, CubicMeter, Length };
    }
}
