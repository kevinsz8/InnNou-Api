namespace InnNou.Application.Common
{
    // Underlying int values must match OrderStatuses.OrderStatusId seed rows exactly
    // (see database/migrations/20260722_OrderStatuses_ConvertToId.sql).
    //
    // Pending_Approval (not PendingApproval) is deliberate: every SP projects the lookup
    // table's Code column (e.g. "PENDING_APPROVAL") directly onto this entity-level enum
    // property, and Dapper's built-in enum-column binding always does a plain
    // Enum.Parse(type, value, ignoreCase: true) for a string column — it does NOT consult any
    // custom SqlMapper.TypeHandler<T> for enum-typed properties inside a mapped class (a known
    // Dapper limitation). "DRAFT"/"SUBMITTED"/"CANCELLED" happen to match Draft/Submitted/
    // Cancelled case-insensitively, but "PENDING_APPROVAL" never matched "PendingApproval" (the
    // underscore isn't ignored by Enum.Parse) — every query returning a PENDING_APPROVAL row
    // threw a SqlMapper.ColumnMapException. Matching the enum member's spelling to the Code
    // string exactly (case aside) is the minimal fix; do not rename this back without adding a
    // real Dapper type-handler workaround instead.
    public enum OrderStatus
    {
        Draft = 1,
        Pending_Approval = 2,
        Submitted = 3,
        Cancelled = 4
    }

    public static class OrderStatusCodes
    {
        public const string Draft = "DRAFT";
        public const string PendingApproval = "PENDING_APPROVAL";
        public const string Submitted = "SUBMITTED";
        public const string Cancelled = "CANCELLED";

        public static string ToCode(OrderStatus status) => status switch
        {
            OrderStatus.Draft => Draft,
            OrderStatus.Pending_Approval => PendingApproval,
            OrderStatus.Submitted => Submitted,
            OrderStatus.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static OrderStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Draft => OrderStatus.Draft,
            PendingApproval => OrderStatus.Pending_Approval,
            Submitted => OrderStatus.Submitted,
            Cancelled => OrderStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };

        // Non-throwing variant for caller-supplied filter values (e.g. GetPaged's optional
        // status filter) — an unrecognized code should just match nothing, not 500.
        public static bool TryFromCode(string? code, out OrderStatus status)
        {
            switch (code?.Trim().ToUpperInvariant())
            {
                case Draft: status = OrderStatus.Draft; return true;
                case PendingApproval: status = OrderStatus.Pending_Approval; return true;
                case Submitted: status = OrderStatus.Submitted; return true;
                case Cancelled: status = OrderStatus.Cancelled; return true;
                default: status = default; return false;
            }
        }
    }
}
