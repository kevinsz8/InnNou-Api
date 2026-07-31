SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPT - EXISTS UNINVOICED FOR PURCHASEORDER
   Used by SupplierInvoiceService.CreateAsync, inside the same transaction
   as its own SupplierInvoiceGoodsReceipts inserts, to decide whether a
   fully-RECEIVED PurchaseOrder can now advance to INVOICED: only once
   every one of its GoodsReceipts has been invoiced, never just the ones
   selected in the current request (there may be earlier/other receipts
   still open).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceipt_ExistsUninvoicedForPurchaseOrder
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS (
        SELECT 1 FROM dbo.GoodsReceipt gr
        WHERE gr.PurchaseOrderId = @PurchaseOrderId
          AND NOT EXISTS (SELECT 1 FROM dbo.SupplierInvoiceGoodsReceipts sigr WHERE sigr.GoodsReceiptId = gr.GoodsReceiptId)
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasUninvoicedGoodsReceipts;
END;
GO
