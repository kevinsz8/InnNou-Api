namespace InnNou.Application.Common
{
    // Underlying int values must match PurchaseOrderStatuses.PurchaseOrderStatusId seed rows
    // exactly (see database/migrations/20260722_PurchaseOrderStatuses_ConvertToId.sql and
    // 20260726_PurchaseOrderStatuses_AddReceivingStatuses.sql).
    //
    // Partially_Received keeps the underscore (not PascalCase "PartiallyReceived") because
    // Dapper's default string-to-enum column mapping matches the enum MEMBER NAME against the
    // SQL row's string value case-insensitively but does NOT strip underscores — same real bug
    // hit and fixed for OrderStatus.Pending_Approval (see OrderStatus.cs). Renaming this to
    // PascalCase would silently break deserializing any row with Status = 'PARTIALLY_RECEIVED'.
    public enum PurchaseOrderStatus
    {
        Sent = 1,
        Cancelled = 2,
        Partially_Received = 3,
        Received = 4
    }

    public static class PurchaseOrderStatusCodes
    {
        public const string Sent = "SENT";
        public const string Cancelled = "CANCELLED";
        public const string PartiallyReceived = "PARTIALLY_RECEIVED";
        public const string Received = "RECEIVED";

        public static string ToCode(PurchaseOrderStatus status) => status switch
        {
            PurchaseOrderStatus.Sent => Sent,
            PurchaseOrderStatus.Cancelled => Cancelled,
            PurchaseOrderStatus.Partially_Received => PartiallyReceived,
            PurchaseOrderStatus.Received => Received,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static PurchaseOrderStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Sent => PurchaseOrderStatus.Sent,
            Cancelled => PurchaseOrderStatus.Cancelled,
            PartiallyReceived => PurchaseOrderStatus.Partially_Received,
            Received => PurchaseOrderStatus.Received,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };

        // Non-throwing variant for caller-supplied filter values (e.g. GetPaged's optional
        // status filter) — an unrecognized code should just match nothing, not 500.
        public static bool TryFromCode(string? code, out PurchaseOrderStatus status)
        {
            switch (code?.Trim().ToUpperInvariant())
            {
                case Sent: status = PurchaseOrderStatus.Sent; return true;
                case Cancelled: status = PurchaseOrderStatus.Cancelled; return true;
                case PartiallyReceived: status = PurchaseOrderStatus.Partially_Received; return true;
                case Received: status = PurchaseOrderStatus.Received; return true;
                default: status = default; return false;
            }
        }
    }
}
