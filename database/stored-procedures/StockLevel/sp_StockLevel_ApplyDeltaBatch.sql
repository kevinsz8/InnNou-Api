SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   STOCKLEVEL - APPLY DELTA BATCH
   Table-valued-parameter sibling of sp_StockLevel_ApplyDelta — applies every (Warehouse,Article)
   delta for one operation (currently: PurchaseOrderService.CreateGoodsReceiptAsync) in a single
   MERGE instead of one round trip per line. @Deltas MUST already be aggregated per
   (WarehouseId, ArticleId) by the caller (SUM duplicates before building the table) — MERGE
   errors at runtime ("attempted to UPDATE the same row more than once") if the same target row
   is matched by more than one source row.

   Same negative-balance defense-in-depth guard as the single-row version (the caller's own C#
   pre-check remains the primary gate) — evaluated as one up-front existence check across the
   whole batch rather than per-row via @@ROWCOUNT, since MERGE has no per-row equivalent of the
   single-row UPDATE's own WHERE-clause guard.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_StockLevel_ApplyDeltaBatch
(
    @Deltas  dbo.StockLevelDeltaTableType READONLY,
    @ActorBy VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM @Deltas d
        LEFT JOIN dbo.StockLevels sl ON sl.WarehouseId = d.WarehouseId AND sl.ArticleId = d.ArticleId
        WHERE ISNULL(sl.QuantityOnHand, 0) + d.Delta < 0
    )
    BEGIN
        RAISERROR('INVENTORY_NEGATIVE_STOCK_NOT_ALLOWED', 16, 1);
        RETURN;
    END

    MERGE dbo.StockLevels AS target
    USING @Deltas AS source
        ON target.WarehouseId = source.WarehouseId AND target.ArticleId = source.ArticleId
    WHEN MATCHED THEN
        UPDATE SET QuantityOnHand = target.QuantityOnHand + source.Delta,
                   LastUpdatedUtc = SYSUTCDATETIME(),
                   LastUpdatedBy = @ActorBy
    WHEN NOT MATCHED THEN
        INSERT (WarehouseId, ArticleId, QuantityOnHand, CreatedBy)
        VALUES (source.WarehouseId, source.ArticleId, source.Delta, @ActorBy);
END;
GO
