SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   STOCKLEVEL - GET ALL BY WAREHOUSE ID
   Every StockLevel row for a warehouse, including zero-quantity articles —
   used by InventoryPeriodService.OpenAsync to seed one InventoryPeriodCount
   line per article with OpeningQuantity = the live balance at open time.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_StockLevel_GetAllByWarehouseId
(
    @WarehouseId INT
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
    WHERE sl.WarehouseId = @WarehouseId
    ORDER BY a.Name;
END;
GO
