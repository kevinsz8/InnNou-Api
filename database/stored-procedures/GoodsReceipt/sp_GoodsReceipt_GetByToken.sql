SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - GET BY TOKEN
   Header + owning-PurchaseOrder context (Organization/Supplier/Warehouse/
   Status) in one row — feeds SupplierInvoiceService.CreateAsync's per-
   selected-receipt validation (scope match, status, exclusivity), same
   shape sp_PurchaseOrder_GetByToken already provides for the PO itself.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_GetByToken
(
    @GoodsReceiptToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        gr.GoodsReceiptId, gr.GoodsReceiptToken,
        gr.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber, po.SentUtc AS PurchaseOrderSentUtc,
        po.OrganizationId, po.SupplierId,
        pos.Code AS PurchaseOrderStatus,
        gr.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        gr.DeliveryNoteNumber, gr.DeliveryNoteDate, gr.Notes, gr.CreatedUtc, gr.CreatedBy
    FROM dbo.GoodsReceipt gr
    JOIN dbo.PurchaseOrder po           ON po.PurchaseOrderId = gr.PurchaseOrderId
    JOIN dbo.PurchaseOrderStatuses pos  ON pos.PurchaseOrderStatusId = po.PurchaseOrderStatusId
    JOIN dbo.Warehouses w               ON w.WarehouseId      = gr.WarehouseId
    WHERE gr.GoodsReceiptToken = @GoodsReceiptToken;
END;
GO
