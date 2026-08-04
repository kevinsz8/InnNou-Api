SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERRECEIPTLINE - GET BY INTERNALORDERRECEIPTID
   Lines for one receiving event — detail view.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderReceiptLine_GetByInternalOrderReceiptId
(
    @InternalOrderReceiptId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        iorl.InternalOrderReceiptLineId, iorl.InternalOrderReceiptLineToken, iorl.InternalOrderReceiptId,
        iorl.InternalOrderShipmentLineId, iosl.InternalOrderShipmentLineToken, iosl.QuantityShipped,
        iol.InternalOrderLineId, iol.InternalOrderLineToken,
        a.ArticleId, a.ArticleToken, a.Name AS ArticleName, pu.Code AS PurchaseUnitCode,
        iorl.QuantityAccepted, iorl.QuantityRejected, iorl.RejectionReason,
        iorl.TaxCategoryId, tc.Code AS TaxCategoryCode, iorl.TaxRatePercent,
        iorl.TaxableAmount, iorl.TaxAmount, iorl.TotalAmount,
        iorl.Notes,
        iorl.CreatedUtc, iorl.CreatedBy
    FROM dbo.InternalOrderReceiptLines iorl
    JOIN dbo.InternalOrderShipmentLines iosl ON iosl.InternalOrderShipmentLineId = iorl.InternalOrderShipmentLineId
    JOIN dbo.InternalOrderLines iol           ON iol.InternalOrderLineId          = iosl.InternalOrderLineId
    JOIN dbo.Articles a                       ON a.ArticleId                      = iol.ArticleId
    JOIN dbo.UnitsOfMeasure pu                ON pu.UnitOfMeasureId               = a.PurchaseUnitId
    LEFT JOIN dbo.TaxCategories tc            ON tc.TaxCategoryId                 = iorl.TaxCategoryId
    WHERE iorl.InternalOrderReceiptId = @InternalOrderReceiptId
    ORDER BY iorl.InternalOrderReceiptLineId;
END;
GO
