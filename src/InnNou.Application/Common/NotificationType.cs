namespace InnNou.Application.Common
{
    // Underlying int values must match NotificationTypes.NotificationTypeId seed rows exactly
    // (see database/migrations/20260805_Notifications_Create.sql).
    public enum NotificationType
    {
        Order_Confirmed = 1,
        New_Purchase_Order = 2,
        Approval_Requested = 3,
        Approval_Step_Approved = 4,
        Approval_Step_Rejected = 5
    }

    public static class NotificationTypeCodes
    {
        public const string OrderConfirmed = "ORDER_CONFIRMED";
        public const string NewPurchaseOrder = "NEW_PURCHASE_ORDER";
        public const string ApprovalRequested = "APPROVAL_REQUESTED";
        public const string ApprovalStepApproved = "APPROVAL_STEP_APPROVED";
        public const string ApprovalStepRejected = "APPROVAL_STEP_REJECTED";

        public static string ToCode(NotificationType type) => type switch
        {
            NotificationType.Order_Confirmed => OrderConfirmed,
            NotificationType.New_Purchase_Order => NewPurchaseOrder,
            NotificationType.Approval_Requested => ApprovalRequested,
            NotificationType.Approval_Step_Approved => ApprovalStepApproved,
            NotificationType.Approval_Step_Rejected => ApprovalStepRejected,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static NotificationType FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            OrderConfirmed => NotificationType.Order_Confirmed,
            NewPurchaseOrder => NotificationType.New_Purchase_Order,
            ApprovalRequested => NotificationType.Approval_Requested,
            ApprovalStepApproved => NotificationType.Approval_Step_Approved,
            ApprovalStepRejected => NotificationType.Approval_Step_Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
