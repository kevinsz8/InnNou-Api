SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - CREATE
   Called once per distinct Supplier inside OrderService.SubmitAsync's
   split transaction. V1 has no approval gate — creation directly is
   sending, so it starts in SENT.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_Create
(
    @PurchaseOrderToken UNIQUEIDENTIFIER,
    @OrderId            INT,
    @SupplierId         INT,
    @OrganizationId     INT,
    @WarehouseId        INT,
    @CreatedBy          VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PurchaseOrder
        (PurchaseOrderToken, OrderId, SupplierId, OrganizationId, WarehouseId, PurchaseOrderStatusId, CreatedBy)
    VALUES
        (@PurchaseOrderToken, @OrderId, @SupplierId, @OrganizationId, @WarehouseId, (SELECT PurchaseOrderStatusId FROM dbo.PurchaseOrderStatuses WHERE Code = 'SENT'), @CreatedBy);

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken,
        po.OrderId, ord.OrderToken,
        po.SupplierId, s.Name AS SupplierName, s.Email AS SupplierEmail,
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
