SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERSHIPMENTLINE - GET BY INTERNALORDERSHIPMENTID
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderShipmentLine_GetByInternalOrderShipmentId
(
    @InternalOrderShipmentId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        iosl.InternalOrderShipmentLineId, iosl.InternalOrderShipmentLineToken,
        iosl.InternalOrderShipmentId, ios.InternalOrderShipmentToken,
        iosl.InternalOrderLineId, iol.InternalOrderLineToken,
        iol.ArticleId, a.ArticleToken, a.Name AS ArticleName, pu.Code AS PurchaseUnitCode,
        iosl.QuantityShipped,
        ISNULL(received.QuantityAccepted, 0) + ISNULL(received.QuantityRejected, 0) AS QuantityReceived,
        iosl.Notes,
        iosl.CreatedUtc, iosl.CreatedBy
    FROM dbo.InternalOrderShipmentLines iosl
    JOIN dbo.InternalOrderShipments ios ON ios.InternalOrderShipmentId = iosl.InternalOrderShipmentId
    JOIN dbo.InternalOrderLines iol      ON iol.InternalOrderLineId    = iosl.InternalOrderLineId
    JOIN dbo.Articles a                  ON a.ArticleId                = iol.ArticleId
    JOIN dbo.UnitsOfMeasure pu           ON pu.UnitOfMeasureId         = a.PurchaseUnitId
    OUTER APPLY (
        SELECT SUM(iorl.QuantityAccepted) AS QuantityAccepted, SUM(iorl.QuantityRejected) AS QuantityRejected
        FROM dbo.InternalOrderReceiptLines iorl
        WHERE iorl.InternalOrderShipmentLineId = iosl.InternalOrderShipmentLineId
    ) received
    WHERE iosl.InternalOrderShipmentId = @InternalOrderShipmentId
    ORDER BY iosl.InternalOrderShipmentLineId;
END;
GO
