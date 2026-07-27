SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIODCOUNT - CREATE
   Single-line insert, called once per article-with-a-StockLevel-row in a C#
   loop inside InventoryPeriodService.OpenAsync's shared transaction — same
   one-call-per-line shape as sp_GoodsReceiptLine_Create (no TVP/JSON batch
   parameter exists anywhere in this codebase). No re-select — the caller
   re-hydrates every line at once afterwards via
   sp_InventoryPeriodCount_GetByPeriodId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriodCount_Create
(
    @InventoryPeriodCountToken UNIQUEIDENTIFIER,
    @InventoryPeriodId         INT,
    @ArticleId                 INT,
    @OpeningQuantity           DECIMAL(18,4),
    @CreatedBy                 VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryPeriodCounts
        (InventoryPeriodCountToken, InventoryPeriodId, ArticleId, OpeningQuantity, CreatedBy)
    VALUES
        (@InventoryPeriodCountToken, @InventoryPeriodId, @ArticleId, @OpeningQuantity, @CreatedBy);
END;
GO
