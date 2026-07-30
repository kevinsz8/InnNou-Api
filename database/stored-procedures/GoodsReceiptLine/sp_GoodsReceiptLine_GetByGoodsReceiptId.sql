SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPTLINE - GET BY GOODSRECEIPT ID
   Lines for a single GoodsReceipt — populates GoodsReceiptDto.Lines.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceiptLine_GetByGoodsReceiptId
(
    @GoodsReceiptId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        grl.GoodsReceiptLineId, grl.GoodsReceiptLineToken, grl.GoodsReceiptId,
        grl.PurchaseOrderLineId, pol.PurchaseOrderLineToken, pol.Quantity AS OrderedQuantity,
        grl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        grl.QuantityAccepted, grl.QuantityCourtesy, grl.QuantityRejected, grl.RejectionReason,
        grl.LotNumber, grl.ExpirationDate, grl.SerialNumber, grl.Notes,
        grl.TaxCategoryId, tc.Code AS TaxCategoryCode, grl.TaxRatePercent,
        grl.TaxableAmount, grl.TaxAmount, grl.TotalAmount,
        grl.CreatedUtc, grl.CreatedBy
    FROM dbo.GoodsReceiptLine grl
    JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = grl.PurchaseOrderLineId
    JOIN dbo.Articles a            ON a.ArticleId             = grl.ArticleId
    LEFT JOIN dbo.TaxCategories tc  ON tc.TaxCategoryId        = grl.TaxCategoryId
    WHERE grl.GoodsReceiptId = @GoodsReceiptId
    ORDER BY grl.GoodsReceiptLineId;
END;
GO
