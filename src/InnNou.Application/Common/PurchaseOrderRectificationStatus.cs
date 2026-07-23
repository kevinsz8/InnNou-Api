namespace InnNou.Application.Common
{
    // Underlying int values must match PurchaseOrderRectificationStatuses seed rows exactly
    // (see database/migrations/20260723_PurchaseOrderRectifications_Create.sql).
    //
    // Pending_Approval (not PendingApproval) is deliberate — same Dapper enum-binding gotcha
    // documented on OrderStatus.Pending_Approval: Dapper's built-in enum-column binding is a
    // plain case-insensitive Enum.Parse that does not ignore underscores, so the member name
    // must match the Code string's spelling (underscore included).
    public enum PurchaseOrderRectificationStatus
    {
        Pending_Approval = 1,
        Applied = 2,
        Rejected = 3
    }

    public static class PurchaseOrderRectificationStatusCodes
    {
        public const string PendingApproval = "PENDING_APPROVAL";
        public const string Applied = "APPLIED";
        public const string Rejected = "REJECTED";

        public static string ToCode(PurchaseOrderRectificationStatus status) => status switch
        {
            PurchaseOrderRectificationStatus.Pending_Approval => PendingApproval,
            PurchaseOrderRectificationStatus.Applied => Applied,
            PurchaseOrderRectificationStatus.Rejected => Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}
