SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - SET STATUS
   Generic status transition for Goods Receipts (SENT -> PARTIALLY_RECEIVED
   -> RECEIVED) — sp_PurchaseOrder_Cancel remains the dedicated SENT ->
   CANCELLED transition (it also stamps CancelledUtc/CancelledBy, which this
   one deliberately doesn't touch). Resolves @Status to Id via the same
   inline-subquery pattern as sp_Order_SetStatus. Called by
   PurchaseOrderService.CreateGoodsReceiptAsync inside the same transaction
   as the GoodsReceipt/GoodsReceiptLine inserts — the "is this fully
   received" decision itself is computed in C#, not here.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_SetStatus
(
    @PurchaseOrderToken UNIQUEIDENTIFIER,
    @Status             VARCHAR(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PurchaseOrder
    SET PurchaseOrderStatusId = (SELECT PurchaseOrderStatusId FROM dbo.PurchaseOrderStatuses WHERE Code = @Status)
    WHERE PurchaseOrderToken = @PurchaseOrderToken;

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc, po.CancelledUtc, po.CancelledBy,
        po.CreatedUtc, po.CreatedBy
    FROM dbo.PurchaseOrder po
    JOIN dbo.[Order] ord              ON ord.OrderId        = po.OrderId
    JOIN dbo.Suppliers s              ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org        ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w             ON w.WarehouseId      = po.WarehouseId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    WHERE po.PurchaseOrderToken = @PurchaseOrderToken;
END;
GO
