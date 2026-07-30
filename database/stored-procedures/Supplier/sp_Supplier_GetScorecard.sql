SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - GET SCORECARD
   Aggregates raw counts over every GoodsReceiptLine received against this
   Supplier within [@FromDate, @ToDate] (both NULL = all time), scoped by
   the receiving PurchaseOrder's own SentUtc — evaluates "everything we
   ordered from this supplier during this window", not receipt date.

   Stays dumb on purpose: returns raw counts, not percentages — the
   service layer computes rates (avoids divide-by-zero edge cases here,
   same reasoning as every other aggregation SP in this codebase).

   On-time = actual days from SentUtc to this receipt's CreatedUtc <=
   Article.LeadTimeDays — only evaluated over lines where LeadTimeDays is
   actually configured (@OtdEligibleLines), since there's nothing to judge
   punctuality against otherwise. In-full = on-time AND this was the only
   GoodsReceiptLine ever recorded against that PurchaseOrderLine AND it
   fully covered the line's (rectification-effective) Quantity with zero
   rejected in this same receipt — i.e. arrived complete in one shipment,
   not eventually completed across several partial receipts.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetScorecard
(
    @SupplierId INT,
    @FromDate   DATE = NULL,
    @ToDate     DATE = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ToDateExclusive DATETIME2 = CASE WHEN @ToDate IS NULL THEN NULL ELSE DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2)) END;

    ;WITH ReceiptLines AS
    (
        SELECT
            grl.GoodsReceiptLineId,
            grl.QuantityAccepted,
            grl.QuantityCourtesy,
            grl.QuantityRejected,
            a.LeadTimeDays,
            DATEDIFF(DAY, po.SentUtc, gr.CreatedUtc) AS ActualLeadTimeDays,
            CASE WHEN (
                    SELECT COUNT(*) FROM dbo.GoodsReceiptLine grl2 WHERE grl2.PurchaseOrderLineId = grl.PurchaseOrderLineId
                 ) = 1
                 AND grl.QuantityRejected = 0
                 AND grl.QuantityAccepted >= pol.Quantity
                 THEN 1 ELSE 0
            END AS IsSoleCompleteReceipt
        FROM dbo.GoodsReceiptLine grl
        JOIN dbo.GoodsReceipt gr ON gr.GoodsReceiptId = grl.GoodsReceiptId
        JOIN dbo.PurchaseOrderLine pol ON pol.PurchaseOrderLineId = grl.PurchaseOrderLineId
        JOIN dbo.PurchaseOrder po ON po.PurchaseOrderId = pol.PurchaseOrderId
        JOIN dbo.Articles a ON a.ArticleId = grl.ArticleId
        WHERE po.SupplierId = @SupplierId
          AND (@FromDate IS NULL OR po.SentUtc >= @FromDate)
          AND (@ToDateExclusive IS NULL OR po.SentUtc < @ToDateExclusive)
    )
    SELECT
        COUNT(*) AS TotalReceiptLines,
        SUM(QuantityAccepted) AS TotalAccepted,
        SUM(QuantityCourtesy) AS TotalCourtesy,
        SUM(QuantityRejected) AS TotalRejected,
        SUM(CASE WHEN LeadTimeDays IS NOT NULL THEN 1 ELSE 0 END) AS OtdEligibleLines,
        SUM(CASE WHEN LeadTimeDays IS NOT NULL AND ActualLeadTimeDays <= LeadTimeDays THEN 1 ELSE 0 END) AS OtdOnTimeLines,
        SUM(CASE WHEN LeadTimeDays IS NOT NULL AND ActualLeadTimeDays <= LeadTimeDays AND IsSoleCompleteReceipt = 1 THEN 1 ELSE 0 END) AS OtifLines,
        AVG(CAST(ActualLeadTimeDays AS DECIMAL(10,2))) AS AvgLeadTimeDays
    FROM ReceiptLines;
END;
GO
