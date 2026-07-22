namespace InnNou.Application.Common
{
    // Underlying int values must match OrderStatuses.OrderStatusId seed rows exactly
    // (see database/migrations/20260722_OrderStatuses_ConvertToId.sql).
    public enum OrderStatus
    {
        Draft = 1,
        PendingApproval = 2,
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
            OrderStatus.PendingApproval => PendingApproval,
            OrderStatus.Submitted => Submitted,
            OrderStatus.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static OrderStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Draft => OrderStatus.Draft,
            PendingApproval => OrderStatus.PendingApproval,
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
                case PendingApproval: status = OrderStatus.PendingApproval; return true;
                case Submitted: status = OrderStatus.Submitted; return true;
                case Cancelled: status = OrderStatus.Cancelled; return true;
                default: status = default; return false;
            }
        }
    }
}
