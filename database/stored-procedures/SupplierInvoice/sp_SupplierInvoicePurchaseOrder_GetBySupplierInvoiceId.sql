SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEPURCHASEORDER - GET BY SUPPLIERINVOICE ID
   Which PurchaseOrders one SupplierInvoice consolidates — populates
   SupplierInvoiceDto.PurchaseOrders for the detail view.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoicePurchaseOrder_GetBySupplierInvoiceId
(
    @SupplierInvoiceId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        po.PurchaseOrderId, po.PurchaseOrderToken, po.PurchaseOrderNumber
    FROM dbo.SupplierInvoicePurchaseOrders sipo
    JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = sipo.PurchaseOrderId
    WHERE sipo.SupplierInvoiceId = @SupplierInvoiceId
    ORDER BY po.PurchaseOrderNumber;
END;
GO
