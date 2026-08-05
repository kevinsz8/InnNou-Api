namespace InnNou.Application.Common
{
    // Underlying int values must match NotificationTypes.NotificationTypeId seed rows exactly
    // (see database/migrations/20260805_Notifications_Create.sql,
    // 20260805_Notifications_AddBucket1Types.sql, and
    // 20260806_Notifications_AddRequisitionTypes.sql).
    public enum NotificationType
    {
        Order_Confirmed = 1,
        New_Purchase_Order = 2,
        Approval_Requested = 3,
        Approval_Step_Approved = 4,
        Approval_Step_Rejected = 5,
        Supplier_Price_Updated = 6,
        Goods_Receipt_Created = 7,
        Purchase_Order_Rectified = 8,
        Internal_Order_Shipped = 9,
        Internal_Order_Received = 10,
        Internal_Order_Cancelled = 11,
        Supplier_Return_Closed = 12,
        Impersonation_Started = 13,
        User_Role_Changed = 14,
        Requisition_Approved = 15,
        Requisition_Rejected = 16,
        Requisition_Issued = 17,
        Requisition_Closed_Short = 18
    }

    public static class NotificationTypeCodes
    {
        public const string OrderConfirmed = "ORDER_CONFIRMED";
        public const string NewPurchaseOrder = "NEW_PURCHASE_ORDER";
        public const string ApprovalRequested = "APPROVAL_REQUESTED";
        public const string ApprovalStepApproved = "APPROVAL_STEP_APPROVED";
        public const string ApprovalStepRejected = "APPROVAL_STEP_REJECTED";
        public const string SupplierPriceUpdated = "SUPPLIER_PRICE_UPDATED";
        public const string GoodsReceiptCreated = "GOODS_RECEIPT_CREATED";
        public const string PurchaseOrderRectified = "PURCHASE_ORDER_RECTIFIED";
        public const string InternalOrderShipped = "INTERNAL_ORDER_SHIPPED";
        public const string InternalOrderReceived = "INTERNAL_ORDER_RECEIVED";
        public const string InternalOrderCancelled = "INTERNAL_ORDER_CANCELLED";
        public const string SupplierReturnClosed = "SUPPLIER_RETURN_CLOSED";
        public const string ImpersonationStarted = "IMPERSONATION_STARTED";
        public const string UserRoleChanged = "USER_ROLE_CHANGED";
        public const string RequisitionApproved = "REQUISITION_APPROVED";
        public const string RequisitionRejected = "REQUISITION_REJECTED";
        public const string RequisitionIssued = "REQUISITION_ISSUED";
        public const string RequisitionClosedShort = "REQUISITION_CLOSED_SHORT";

        public static string ToCode(NotificationType type) => type switch
        {
            NotificationType.Order_Confirmed => OrderConfirmed,
            NotificationType.New_Purchase_Order => NewPurchaseOrder,
            NotificationType.Approval_Requested => ApprovalRequested,
            NotificationType.Approval_Step_Approved => ApprovalStepApproved,
            NotificationType.Approval_Step_Rejected => ApprovalStepRejected,
            NotificationType.Supplier_Price_Updated => SupplierPriceUpdated,
            NotificationType.Goods_Receipt_Created => GoodsReceiptCreated,
            NotificationType.Purchase_Order_Rectified => PurchaseOrderRectified,
            NotificationType.Internal_Order_Shipped => InternalOrderShipped,
            NotificationType.Internal_Order_Received => InternalOrderReceived,
            NotificationType.Internal_Order_Cancelled => InternalOrderCancelled,
            NotificationType.Supplier_Return_Closed => SupplierReturnClosed,
            NotificationType.Impersonation_Started => ImpersonationStarted,
            NotificationType.User_Role_Changed => UserRoleChanged,
            NotificationType.Requisition_Approved => RequisitionApproved,
            NotificationType.Requisition_Rejected => RequisitionRejected,
            NotificationType.Requisition_Issued => RequisitionIssued,
            NotificationType.Requisition_Closed_Short => RequisitionClosedShort,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static NotificationType FromCode(string code) => code.Trim().ToUpperInvariant() switch
        {
            OrderConfirmed => NotificationType.Order_Confirmed,
            NewPurchaseOrder => NotificationType.New_Purchase_Order,
            ApprovalRequested => NotificationType.Approval_Requested,
            ApprovalStepApproved => NotificationType.Approval_Step_Approved,
            ApprovalStepRejected => NotificationType.Approval_Step_Rejected,
            SupplierPriceUpdated => NotificationType.Supplier_Price_Updated,
            GoodsReceiptCreated => NotificationType.Goods_Receipt_Created,
            PurchaseOrderRectified => NotificationType.Purchase_Order_Rectified,
            InternalOrderShipped => NotificationType.Internal_Order_Shipped,
            InternalOrderReceived => NotificationType.Internal_Order_Received,
            InternalOrderCancelled => NotificationType.Internal_Order_Cancelled,
            SupplierReturnClosed => NotificationType.Supplier_Return_Closed,
            ImpersonationStarted => NotificationType.Impersonation_Started,
            UserRoleChanged => NotificationType.User_Role_Changed,
            RequisitionApproved => NotificationType.Requisition_Approved,
            RequisitionRejected => NotificationType.Requisition_Rejected,
            RequisitionIssued => NotificationType.Requisition_Issued,
            RequisitionClosedShort => NotificationType.Requisition_Closed_Short,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
