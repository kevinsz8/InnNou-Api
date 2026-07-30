SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PURCHASEORDER - GET ELIGIBLE FOR INVOICING
   Feeds the SupplierInvoice creation picker: RECEIVED (100%) PurchaseOrders
   for one Organization + Supplier, excluding any already consolidated into
   an existing SupplierInvoice (enforced by
   UX_SupplierInvoicePurchaseOrders_PurchaseOrderId at the DB level, but
   filtered here too so the picker never even shows an ineligible PO).
   No pagination — same "small, bounded picker list" shape as
   Order/OrderTemplate's article-search modal.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_PurchaseOrder_GetEligibleForInvoicing
(
    @OrganizationId INT,
    @SupplierId     INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber,
        po.SupplierId, s.Name AS SupplierName,
        po.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        po.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pos.Code AS Status, po.SentUtc,
        po.CreatedUtc, po.CreatedBy,
        lc.LineCount
    FROM dbo.PurchaseOrder po
    JOIN dbo.Suppliers s               ON s.SupplierId       = po.SupplierId
    JOIN dbo.Organizations org         ON org.OrganizationId = po.OrganizationId
    JOIN dbo.Warehouses w              ON w.WarehouseId      = po.WarehouseId
    JOIN dbo.PurchaseOrderStatuses pos ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    CROSS APPLY (SELECT COUNT(*) AS LineCount FROM dbo.PurchaseOrderLine pol WHERE pol.PurchaseOrderId = po.PurchaseOrderId) lc
    WHERE po.OrganizationId = @OrganizationId
      AND po.SupplierId = @SupplierId
      AND pos.Code = 'RECEIVED'
      AND NOT EXISTS (SELECT 1 FROM dbo.SupplierInvoicePurchaseOrders sipo WHERE sipo.PurchaseOrderId = po.PurchaseOrderId)
    ORDER BY po.CreatedUtc DESC;
END;
GO
