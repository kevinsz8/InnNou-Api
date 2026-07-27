SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIODCOUNT - GET BY PERIOD ID
   Lines for a single InventoryPeriod — populates InventoryPeriodDto.Lines,
   same "eager hydrate, small bounded list" precedent as
   sp_GoodsReceiptLine_GetByGoodsReceiptId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriodCount_GetByPeriodId
(
    @InventoryPeriodId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ipc.InventoryPeriodCountId, ipc.InventoryPeriodCountToken, ipc.InventoryPeriodId,
        ipc.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        ipc.OpeningQuantity, ipc.CountedQuantity, ipc.SystemQuantityAtClose, ipc.VarianceQuantity,
        ipc.CreatedUtc, ipc.CreatedBy, ipc.LastUpdatedUtc, ipc.LastUpdatedBy
    FROM dbo.InventoryPeriodCounts ipc
    JOIN dbo.Articles a ON a.ArticleId = ipc.ArticleId
    WHERE ipc.InventoryPeriodId = @InventoryPeriodId
    ORDER BY a.Name;
END;
GO
