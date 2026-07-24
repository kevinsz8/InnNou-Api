SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   STOCKLEVEL - GET BY WAREHOUSE AND ARTICLE
   Single-row lookup for the current on-hand balance — used by
   InventoryService before validating an Adjustment/Transfer won't take
   stock negative. Returns nothing if no StockLevels row exists yet for
   this pair (treated as a current balance of 0 by the caller).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_StockLevel_GetByWarehouseAndArticle
(
    @WarehouseId INT,
    @ArticleId   INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sl.StockLevelId, sl.StockLevelToken,
        sl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        sl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.SupplierId, s.Name AS SupplierName,
        a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        sl.QuantityOnHand,
        sl.CreatedUtc, sl.CreatedBy, sl.LastUpdatedUtc, sl.LastUpdatedBy
    FROM dbo.StockLevels sl
    JOIN dbo.Warehouses w      ON w.WarehouseId      = sl.WarehouseId
    JOIN dbo.Articles a        ON a.ArticleId        = sl.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure u  ON u.UnitOfMeasureId  = a.PurchaseUnitId
    WHERE sl.WarehouseId = @WarehouseId AND sl.ArticleId = @ArticleId;
END;
GO
