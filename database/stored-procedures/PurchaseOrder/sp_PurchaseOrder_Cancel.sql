SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - CANCEL
   SENT -> CANCELLED only. Re-checks current status in the WHERE
   itself (defense in depth), independent of the service-layer check.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Cancel
(
    @PurchaseOrderToken UNIQUEIDENTIFIER,
    @CancelledBy        VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.PurchaseOrder po
        JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
        WHERE po.PurchaseOrderToken = @PurchaseOrderToken AND pos.Code = 'SENT'
    )
    BEGIN
        RAISERROR('PURCHASE_ORDER_NOT_SENT', 16, 1);
        RETURN;
    END

    UPDATE dbo.PurchaseOrder
    SET
        PurchaseOrderStatusId = (SELECT PurchaseOrderStatusId FROM dbo.PurchaseOrderStatuses WHERE Code = 'CANCELLED'),
        CancelledUtc          = SYSUTCDATETIME(),
        CancelledBy           = @CancelledBy
    WHERE PurchaseOrderToken = @PurchaseOrderToken;

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken,
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
