SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICELINE - GET BY SUPPLIERINVOICE ID
   Lines for a single SupplierInvoice — populates SupplierInvoiceDto.Lines.
   DeliveryNoteNumber (added 2026-08-04) lets the UI group lines by article
   and still show which albaran each one came from when expanded — LEFT JOIN
   since GoodsReceiptLineId is nullable (pre-2026-08-02 invoices, from before
   invoicing moved to receipt granularity, never had one).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceLine_GetBySupplierInvoiceId
(
    @SupplierInvoiceId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sil.SupplierInvoiceLineId, sil.SupplierInvoiceLineToken, sil.SupplierInvoiceId,
        sil.PurchaseOrderLineId, pol.PurchaseOrderLineToken, pol.Quantity AS OrderedQuantity, po.PurchaseOrderNumber,
        sil.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        sil.QuantityInvoiced, sil.UnitPriceInvoiced, sil.CurrencyCode,
        sil.TaxCategoryId, tc.Code AS TaxCategoryCode, sil.TaxRatePercent,
        sil.TaxableAmount, sil.TaxAmount, sil.TotalAmount,
        sil.IsWithinTolerance,
        gr.DeliveryNoteNumber,
        sil.CreatedUtc, sil.CreatedBy
    FROM dbo.SupplierInvoiceLines sil
    JOIN dbo.PurchaseOrderLine pol      ON pol.PurchaseOrderLineId = sil.PurchaseOrderLineId
    JOIN dbo.PurchaseOrder po           ON po.PurchaseOrderId      = pol.PurchaseOrderId
    JOIN dbo.Articles a                 ON a.ArticleId             = sil.ArticleId
    LEFT JOIN dbo.TaxCategories tc      ON tc.TaxCategoryId        = sil.TaxCategoryId
    LEFT JOIN dbo.GoodsReceiptLine grl  ON grl.GoodsReceiptLineId  = sil.GoodsReceiptLineId
    LEFT JOIN dbo.GoodsReceipt gr       ON gr.GoodsReceiptId       = grl.GoodsReceiptId
    WHERE sil.SupplierInvoiceId = @SupplierInvoiceId
    ORDER BY sil.SupplierInvoiceLineId;
END;
GO
