SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   STOCKLEVEL - APPLY DELTA
   Upserts the running on-hand balance for (WarehouseId, ArticleId). This is
   a deliberate exception to this codebase's usual "SPs stay dumb, C# does
   the validation" convention — InventoryService/PurchaseOrderService
   already validate the resulting balance won't go negative BEFORE calling
   this (the primary, user-facing check), but the WHERE guard on the UPDATE
   is a defense-in-depth backstop against a concurrent-write race, same
   reasoning as sp_PurchaseOrder_Cancel's own "re-checks in the WHERE,
   independent of the service-layer check". @@ROWCOUNT = 0 after the UPDATE
   means the guard actually fired (the caller's own C# pre-check somehow
   missed a concurrent write) — RAISERROR here should be treated as a
   should-rarely-happen backstop, not the primary error path.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_StockLevel_ApplyDelta
(
    @WarehouseId INT,
    @ArticleId   INT,
    @Delta       DECIMAL(18,4),
    @ActorBy     VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.StockLevels WHERE WarehouseId = @WarehouseId AND ArticleId = @ArticleId)
    BEGIN
        IF @Delta < 0
        BEGIN
            RAISERROR('INVENTORY_NEGATIVE_STOCK_NOT_ALLOWED', 16, 1);
            RETURN;
        END

        INSERT INTO dbo.StockLevels (WarehouseId, ArticleId, QuantityOnHand, CreatedBy)
        VALUES (@WarehouseId, @ArticleId, @Delta, @ActorBy);
    END
    ELSE
    BEGIN
        UPDATE dbo.StockLevels
        SET QuantityOnHand = QuantityOnHand + @Delta,
            LastUpdatedUtc = SYSUTCDATETIME(),
            LastUpdatedBy = @ActorBy
        WHERE WarehouseId = @WarehouseId AND ArticleId = @ArticleId
          AND QuantityOnHand + @Delta >= 0;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('INVENTORY_NEGATIVE_STOCK_NOT_ALLOWED', 16, 1);
            RETURN;
        END
    END

    SELECT
        sl.StockLevelId, sl.StockLevelToken,
        sl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        sl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        sl.QuantityOnHand,
        sl.CreatedUtc, sl.CreatedBy, sl.LastUpdatedUtc, sl.LastUpdatedBy
    FROM dbo.StockLevels sl
    JOIN dbo.Warehouses w ON w.WarehouseId = sl.WarehouseId
    JOIN dbo.Articles a ON a.ArticleId = sl.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    WHERE sl.WarehouseId = @WarehouseId AND sl.ArticleId = @ArticleId;
END;
GO
