SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIODCOUNT - UPDATE COUNT
   Sets the physical CountedQuantity for one (InventoryPeriodId, ArticleId)
   line. Returns zero rows if the article isn't one of the period's existing
   lines (periods are seeded complete at open time, no ad hoc additions) —
   InventoryPeriodService.SubmitCountAsync treats a null result as
   INVENTORY_PERIOD_ARTICLE_NOT_IN_PERIOD (404).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriodCount_UpdateCount
(
    @InventoryPeriodId INT,
    @ArticleId         INT,
    @CountedQuantity   DECIMAL(18,8),
    @ActorBy           VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.InventoryPeriodCounts
    SET CountedQuantity = @CountedQuantity,
        LastUpdatedUtc = SYSUTCDATETIME(),
        LastUpdatedBy = @ActorBy
    WHERE InventoryPeriodId = @InventoryPeriodId AND ArticleId = @ArticleId;

    SELECT
        ipc.InventoryPeriodCountId, ipc.InventoryPeriodCountToken, ipc.InventoryPeriodId,
        ipc.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        ipc.OpeningQuantity, ipc.CountedQuantity, ipc.SystemQuantityAtClose, ipc.VarianceQuantity,
        ipc.CreatedUtc, ipc.CreatedBy, ipc.LastUpdatedUtc, ipc.LastUpdatedBy
    FROM dbo.InventoryPeriodCounts ipc
    JOIN dbo.Articles a ON a.ArticleId = ipc.ArticleId
    WHERE ipc.InventoryPeriodId = @InventoryPeriodId AND ipc.ArticleId = @ArticleId;
END;
GO
