SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEPURCHASEORDER - CREATE
   Links one PurchaseOrder into a SupplierInvoice's consolidation. The real
   "invoiced at most once" guarantee is UX_SupplierInvoicePurchaseOrders_PurchaseOrderId
   (a unique index on PurchaseOrderId alone) — this insert fails with 2601/2627
   if the PO was already linked to another invoice by a concurrent request.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoicePurchaseOrder_Create
(
    @SupplierInvoiceId INT,
    @PurchaseOrderId   INT
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.SupplierInvoicePurchaseOrders (SupplierInvoiceId, PurchaseOrderId)
    VALUES (@SupplierInvoiceId, @PurchaseOrderId);
END;
GO
