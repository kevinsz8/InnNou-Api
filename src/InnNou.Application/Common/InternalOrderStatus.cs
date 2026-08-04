namespace InnNou.Application.Common
{
    // Underlying int values must match InternalOrderStatuses.InternalOrderStatusId seed rows
    // exactly (see database/migrations/20260804_InternalOrders_Create.sql).
    //
    // Partially_Received keeps the underscore (not PascalCase "PartiallyReceived") for the same
    // Dapper string-to-enum mapping reason as PurchaseOrderStatus.Partially_Received/
    // OrderStatus.Pending_Approval — the mapping matches the enum MEMBER NAME against the SQL
    // row's Code case-insensitively but does not strip underscores.
    public enum InternalOrderStatus
    {
        Requested = 1,
        Shipped = 2,
        Partially_Received = 3,
        Received = 4,
        Cancelled = 5
    }

    public static class InternalOrderStatusCodes
    {
        public const string Requested = "REQUESTED";
        public const string Shipped = "SHIPPED";
        public const string PartiallyReceived = "PARTIALLY_RECEIVED";
        public const string Received = "RECEIVED";
        public const string Cancelled = "CANCELLED";

        public static string ToCode(InternalOrderStatus status) => status switch
        {
            InternalOrderStatus.Requested => Requested,
            InternalOrderStatus.Shipped => Shipped,
            InternalOrderStatus.Partially_Received => PartiallyReceived,
            InternalOrderStatus.Received => Received,
            InternalOrderStatus.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static InternalOrderStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Requested => InternalOrderStatus.Requested,
            Shipped => InternalOrderStatus.Shipped,
            PartiallyReceived => InternalOrderStatus.Partially_Received,
            Received => InternalOrderStatus.Received,
            Cancelled => InternalOrderStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };

        // Non-throwing variant for caller-supplied filter values (e.g. GetPaged's optional
        // status filter) — an unrecognized code should just match nothing, not 500.
        public static bool TryFromCode(string? code, out InternalOrderStatus status)
        {
            switch (code?.Trim().ToUpperInvariant())
            {
                case Requested: status = InternalOrderStatus.Requested; return true;
                case Shipped: status = InternalOrderStatus.Shipped; return true;
                case PartiallyReceived: status = InternalOrderStatus.Partially_Received; return true;
                case Received: status = InternalOrderStatus.Received; return true;
                case Cancelled: status = InternalOrderStatus.Cancelled; return true;
                default: status = default; return false;
            }
        }
    }
}
