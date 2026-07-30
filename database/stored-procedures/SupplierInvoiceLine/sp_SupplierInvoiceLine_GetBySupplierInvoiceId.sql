SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICELINE - GET BY SUPPLIERINVOICE ID
   Lines for a single SupplierInvoice — populates SupplierInvoiceDto.Lines.
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
        sil.CreatedUtc, sil.CreatedBy
    FROM dbo.SupplierInvoiceLines sil
    JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = sil.PurchaseOrderLineId
    JOIN dbo.PurchaseOrder po      ON po.PurchaseOrderId      = pol.PurchaseOrderId
    JOIN dbo.Articles a            ON a.ArticleId             = sil.ArticleId
    LEFT JOIN dbo.TaxCategories tc  ON tc.TaxCategoryId        = sil.TaxCategoryId
    WHERE sil.SupplierInvoiceId = @SupplierInvoiceId
    ORDER BY sil.SupplierInvoiceLineId;
END;
GO
