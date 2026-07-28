SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - CLOSE SHORT ("Caso B")
   PARTIALLY_RECEIVED -> CLOSED_SHORT only. Re-checks current status
   in the WHERE itself (defense in depth), independent of the
   service-layer check. Never touches PurchaseOrderLine/Quantity —
   see sp_PurchaseOrder_Cancel's own header comment for the same
   "check twice" shape this mirrors.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_CloseShort
(
    @PurchaseOrderToken UNIQUEIDENTIFIER,
    @ClosedShortBy      VARCHAR(150),
    @ClosedShortReason  NVARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.PurchaseOrder po
        JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
        WHERE po.PurchaseOrderToken = @PurchaseOrderToken AND pos.Code = 'PARTIALLY_RECEIVED'
    )
    BEGIN
        RAISERROR('PURCHASE_ORDER_CLOSE_SHORT_NOT_ALLOWED', 16, 1);
        RETURN;
    END

    UPDATE dbo.PurchaseOrder
    SET
        PurchaseOrderStatusId = (SELECT PurchaseOrderStatusId FROM dbo.PurchaseOrderStatuses WHERE Code = 'CLOSED_SHORT'),
        ClosedShortUtc         = SYSUTCDATETIME(),
        ClosedShortBy          = @ClosedShortBy,
        ClosedShortReason      = @ClosedShortReason
    WHERE PurchaseOrderToken = @PurchaseOrderToken;

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc, po.CancelledUtc, po.CancelledBy,
        po.ClosedShortUtc, po.ClosedShortBy, po.ClosedShortReason,
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
