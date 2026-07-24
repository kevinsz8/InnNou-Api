SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPTLINE - GET BY PURCHASEORDER ID
   Plain flat SELECT of every GoodsReceiptLine across every GoodsReceipt for
   one PurchaseOrder — no aggregation here. PurchaseOrderService aggregates
   Sum(QuantityAccepted) grouped by PurchaseOrderLineId via LINQ, same
   "SPs stay dumb, aggregation lives in C#" convention as
   OrderService.ApproveStepAndAdvanceAsync's All(...) check.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceiptLine_GetByPurchaseOrderId
(
    @PurchaseOrderId INT
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
    JOIN dbo.GoodsReceipt gr        ON gr.GoodsReceiptId       = grl.GoodsReceiptId
    JOIN dbo.PurchaseOrderLine pol  ON pol.PurchaseOrderLineId = grl.PurchaseOrderLineId
    JOIN dbo.Articles a             ON a.ArticleId             = grl.ArticleId
    WHERE gr.PurchaseOrderId = @PurchaseOrderId
    ORDER BY grl.GoodsReceiptLineId;
END;
GO
