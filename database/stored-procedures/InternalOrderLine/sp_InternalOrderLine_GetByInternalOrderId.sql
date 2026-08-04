SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERLINE - GET BY INTERNALORDERID
   Also carries QuantityShipped (already-shipped total across every
   InternalOrderShipmentLine referencing each line) and QuantityAccepted
   (already-accepted total across every InternalOrderReceiptLine, walked
   through the shipment lines) so the caller can compute
   remaining-to-ship/remaining-to-receive without a second round trip.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderLine_GetByInternalOrderId
(
    @InternalOrderId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        iol.InternalOrderLineId, iol.InternalOrderLineToken,
        iol.InternalOrderId, io.InternalOrderToken,
        iol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierSku,
        iol.Quantity, pu.Code AS PurchaseUnitCode,
        iol.UnitPrice, iol.CurrencyCode,
        iol.Notes,
        ISNULL(shipped.QuantityShipped, 0) AS QuantityShipped,
        ISNULL(received.QuantityAccepted, 0) AS QuantityAccepted,
        iol.CreatedUtc, iol.CreatedBy
    FROM dbo.InternalOrderLines iol
    JOIN dbo.InternalOrders io ON io.InternalOrderId = iol.InternalOrderId
    JOIN dbo.Articles a        ON a.ArticleId        = iol.ArticleId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId  = a.PurchaseUnitId
    OUTER APPLY (
        SELECT SUM(iosl.QuantityShipped) AS QuantityShipped
        FROM dbo.InternalOrderShipmentLines iosl
        WHERE iosl.InternalOrderLineId = iol.InternalOrderLineId
    ) shipped
    OUTER APPLY (
        SELECT SUM(iorl.QuantityAccepted) AS QuantityAccepted
        FROM dbo.InternalOrderShipmentLines iosl
        JOIN dbo.InternalOrderReceiptLines iorl ON iorl.InternalOrderShipmentLineId = iosl.InternalOrderShipmentLineId
        WHERE iosl.InternalOrderLineId = iol.InternalOrderLineId
    ) received
    WHERE iol.InternalOrderId = @InternalOrderId
    ORDER BY iol.InternalOrderLineId;
END;
GO
