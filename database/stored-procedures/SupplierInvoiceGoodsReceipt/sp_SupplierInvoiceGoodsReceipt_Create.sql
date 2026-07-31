SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEGOODSRECEIPT - CREATE
   The new invoice-exclusivity gate for a GoodsReceipt (replaces
   sp_SupplierInvoicePurchaseOrder_Create's old PurchaseOrder-level role) —
   UQ_SupplierInvoiceGoodsReceipts_GoodsReceiptId means this insert fails
   with a unique-violation if the receipt was already invoiced by another
   concurrent request; SupplierInvoiceService.CreateAsync catches that and
   surfaces SUPPLIER_INVOICE_GOODS_RECEIPT_ALREADY_INVOICED.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceGoodsReceipt_Create
(
    @SupplierInvoiceId INT,
    @GoodsReceiptId    INT,
    @CreatedBy         VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierInvoiceGoodsReceipts (SupplierInvoiceId, GoodsReceiptId, CreatedBy)
    VALUES (@SupplierInvoiceId, @GoodsReceiptId, @CreatedBy);
END;
GO
