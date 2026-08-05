namespace InnNou.Application.Common
{
    // Underlying int values must match RequisitionStatuses.RequisitionStatusId seed rows exactly
    // (see database/migrations/20260805_Requisitions_Create.sql).
    //
    // Partially_Issued/Closed_Short keep their underscore (not PascalCase) for the same Dapper
    // string-to-enum mapping reason as PurchaseOrderStatus.Partially_Received/
    // InternalOrderStatus.Partially_Received — the mapping matches the enum MEMBER NAME against
    // the SQL row's Code case-insensitively but does not strip underscores.
    public enum RequisitionStatus
    {
        Requested = 1,
        Approved = 2,
        Partially_Issued = 3,
        Issued = 4,
        Rejected = 5,
        Cancelled = 6,
        Closed_Short = 7
    }

    public static class RequisitionStatusCodes
    {
        public const string Requested = "REQUESTED";
        public const string Approved = "APPROVED";
        public const string PartiallyIssued = "PARTIALLY_ISSUED";
        public const string Issued = "ISSUED";
        public const string Rejected = "REJECTED";
        public const string Cancelled = "CANCELLED";
        public const string ClosedShort = "CLOSED_SHORT";

        public static string ToCode(RequisitionStatus status) => status switch
        {
            RequisitionStatus.Requested => Requested,
            RequisitionStatus.Approved => Approved,
            RequisitionStatus.Partially_Issued => PartiallyIssued,
            RequisitionStatus.Issued => Issued,
            RequisitionStatus.Rejected => Rejected,
            RequisitionStatus.Cancelled => Cancelled,
            RequisitionStatus.Closed_Short => ClosedShort,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static RequisitionStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Requested => RequisitionStatus.Requested,
            Approved => RequisitionStatus.Approved,
            PartiallyIssued => RequisitionStatus.Partially_Issued,
            Issued => RequisitionStatus.Issued,
            Rejected => RequisitionStatus.Rejected,
            Cancelled => RequisitionStatus.Cancelled,
            ClosedShort => RequisitionStatus.Closed_Short,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };

        // Non-throwing variant for caller-supplied filter values (e.g. GetPaged's optional status
        // filter) — an unrecognized code should just match nothing, not 500.
        public static bool TryFromCode(string? code, out RequisitionStatus status)
        {
            switch (code?.Trim().ToUpperInvariant())
            {
                case Requested: status = RequisitionStatus.Requested; return true;
                case Approved: status = RequisitionStatus.Approved; return true;
                case PartiallyIssued: status = RequisitionStatus.Partially_Issued; return true;
                case Issued: status = RequisitionStatus.Issued; return true;
                case Rejected: status = RequisitionStatus.Rejected; return true;
                case Cancelled: status = RequisitionStatus.Cancelled; return true;
                case ClosedShort: status = RequisitionStatus.Closed_Short; return true;
                default: status = default; return false;
            }
        }
    }
}
