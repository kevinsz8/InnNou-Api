SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - GET BY TOKEN
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetByToken
(
    @PurchaseOrderToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

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
