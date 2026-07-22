namespace InnNou.Application.Common
{
    // Underlying int values must match PurchaseOrderStatuses.PurchaseOrderStatusId seed rows
    // exactly (see database/migrations/20260722_PurchaseOrderStatuses_ConvertToId.sql).
    public enum PurchaseOrderStatus
    {
        Sent = 1,
        Cancelled = 2
    }

    public static class PurchaseOrderStatusCodes
    {
        public const string Sent = "SENT";
        public const string Cancelled = "CANCELLED";

        public static string ToCode(PurchaseOrderStatus status) => status switch
        {
            PurchaseOrderStatus.Sent => Sent,
            PurchaseOrderStatus.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static PurchaseOrderStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Sent => PurchaseOrderStatus.Sent,
            Cancelled => PurchaseOrderStatus.Cancelled,
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
                default: status = default; return false;
            }
        }
    }
}
