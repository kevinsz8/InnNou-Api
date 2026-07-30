SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   GOODSRECEIPTLINE - GET ELIGIBLE FOR RETURN
   Every rejected line (QuantityRejected > 0) across every GoodsReceipt of
   a PurchaseOrder that hasn't already been claimed by a SupplierReturnLine
   — feeds the "new return" picker (a rejected line can only ever be
   claimed once, see UQ_SupplierReturnLines_GoodsReceiptLineId).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_GoodsReceiptLine_GetEligibleForReturn
(
    @PurchaseOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        grl.GoodsReceiptLineId, grl.GoodsReceiptLineToken,
        grl.GoodsReceiptId, gr.DeliveryNoteNumber, gr.CreatedUtc AS ReceivedUtc,
        grl.PurchaseOrderLineId,
        grl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        grl.QuantityRejected, grl.RejectionReason
    FROM dbo.GoodsReceiptLine grl
    JOIN dbo.GoodsReceipt gr ON gr.GoodsReceiptId = grl.GoodsReceiptId
    JOIN dbo.Articles a ON a.ArticleId = grl.ArticleId
    WHERE gr.PurchaseOrderId = @PurchaseOrderId
      AND grl.QuantityRejected > 0
      AND NOT EXISTS (SELECT 1 FROM dbo.SupplierReturnLines srl WHERE srl.GoodsReceiptLineId = grl.GoodsReceiptLineId)
    ORDER BY gr.CreatedUtc DESC;
END;
GO
