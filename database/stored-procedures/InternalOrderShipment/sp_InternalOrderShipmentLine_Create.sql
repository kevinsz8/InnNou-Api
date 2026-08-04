SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INTERNALORDERSHIPMENTLINE - CREATE
   Called once per line inside InternalOrderService.CreateShipmentAsync's
   transaction, right after the stock-out effect
   (sp_StockLevel_ApplyDelta + sp_InventoryMovement_Create, Type =
   INTERNAL_ORDER_OUT) is applied for the same line — see the service, not
   this SP, for that ordering.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InternalOrderShipmentLine_Create
(
    @InternalOrderShipmentLineToken UNIQUEIDENTIFIER,
    @InternalOrderShipmentId         INT,
    @InternalOrderLineId              INT,
    @QuantityShipped                  DECIMAL(18,8),
    @Notes                            NVARCHAR(500) = NULL,
    @CreatedBy                        VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InternalOrderShipmentLines
        (InternalOrderShipmentLineToken, InternalOrderShipmentId, InternalOrderLineId, QuantityShipped, Notes, CreatedBy)
    VALUES
        (@InternalOrderShipmentLineToken, @InternalOrderShipmentId, @InternalOrderLineId, @QuantityShipped, @Notes, @CreatedBy);

    SELECT
        iosl.InternalOrderShipmentLineId, iosl.InternalOrderShipmentLineToken,
        iosl.InternalOrderShipmentId, ios.InternalOrderShipmentToken,
        iosl.InternalOrderLineId, iol.InternalOrderLineToken,
        iol.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        iosl.QuantityShipped,
        iosl.Notes,
        iosl.CreatedUtc, iosl.CreatedBy
    FROM dbo.InternalOrderShipmentLines iosl
    JOIN dbo.InternalOrderShipments ios ON ios.InternalOrderShipmentId = iosl.InternalOrderShipmentId
    JOIN dbo.InternalOrderLines iol      ON iol.InternalOrderLineId    = iosl.InternalOrderLineId
    JOIN dbo.Articles a                  ON a.ArticleId                = iol.ArticleId
    WHERE iosl.InternalOrderShipmentLineToken = @InternalOrderShipmentLineToken;
END;
GO
