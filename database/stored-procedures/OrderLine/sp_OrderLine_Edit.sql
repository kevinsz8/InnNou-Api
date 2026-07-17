SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORDERLINE - EDIT
   Quantity-only change on an existing line. Price/unit snapshots are
   never re-touched here — only sp_OrderLine_Upsert's re-add path
   refreshes them.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrderLine_Edit
(
    @OrderLineToken UNIQUEIDENTIFIER,
    @Quantity       DECIMAL(18,4),
    @LastUpdatedBy  VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.OrderLine WHERE OrderLineToken = @OrderLineToken)
    BEGIN
        RAISERROR('ORDER_LINE_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE dbo.OrderLine
    SET
        Quantity       = @Quantity,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy  = @LastUpdatedBy
    WHERE OrderLineToken = @OrderLineToken;

    SELECT
        ol.OrderLineId, ol.OrderLineToken, ol.OrderId, ord.OrderToken,
        ol.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        ol.Quantity,
        ol.PurchaseUnitId, pu.Code AS PurchaseUnitCode,
        ol.PurchaseQuantity,
        ol.ContentUnitId, cu.Code AS ContentUnitCode,
        ol.ContentQuantity,
        ol.UnitPrice, ol.CurrencyCode,
        ol.Notes,
        ol.CreatedUtc, ol.CreatedBy, ol.LastUpdatedUtc, ol.LastUpdatedBy
    FROM dbo.OrderLine ol
    JOIN dbo.[Order] ord       ON ord.OrderId        = ol.OrderId
    JOIN dbo.Articles a        ON a.ArticleId        = ol.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure pu ON pu.UnitOfMeasureId = ol.PurchaseUnitId
    JOIN dbo.UnitsOfMeasure cu ON cu.UnitOfMeasureId = ol.ContentUnitId
    WHERE ol.OrderLineToken = @OrderLineToken;
END;
GO
