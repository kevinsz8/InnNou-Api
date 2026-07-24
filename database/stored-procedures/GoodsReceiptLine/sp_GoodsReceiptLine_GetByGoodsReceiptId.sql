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
        grl.CreatedUtc, grl.CreatedBy
    FROM dbo.GoodsReceiptLine grl
    JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = grl.PurchaseOrderLineId
    JOIN dbo.Articles a            ON a.ArticleId             = grl.ArticleId
    WHERE grl.GoodsReceiptId = @GoodsReceiptId
    ORDER BY grl.GoodsReceiptLineId;
END;
GO
