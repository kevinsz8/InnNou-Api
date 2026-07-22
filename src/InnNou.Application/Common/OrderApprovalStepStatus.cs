namespace InnNou.Application.Common
{
    // Underlying int values must match OrderApprovalStepStatuses.OrderApprovalStepStatusId
    // seed rows exactly (see database/migrations/20260722_OrderApprovalStepStatuses_ConvertToId.sql).
    public enum OrderApprovalStepStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4
    }

    public static class OrderApprovalStepStatusCodes
    {
        public const string Pending = "PENDING";
        public const string Approved = "APPROVED";
        public const string Rejected = "REJECTED";
        public const string Cancelled = "CANCELLED";

        public static string ToCode(OrderApprovalStepStatus status) => status switch
        {
            OrderApprovalStepStatus.Pending => Pending,
            OrderApprovalStepStatus.Approved => Approved,
            OrderApprovalStepStatus.Rejected => Rejected,
            OrderApprovalStepStatus.Cancelled => Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        public static OrderApprovalStepStatus FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            Pending => OrderApprovalStepStatus.Pending,
            Approved => OrderApprovalStepStatus.Approved,
            Rejected => OrderApprovalStepStatus.Rejected,
            Cancelled => OrderApprovalStepStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
