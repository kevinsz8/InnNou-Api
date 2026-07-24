SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - CREATE
   Base par level for a (Warehouse, Article) pair. Plain insert + re-select,
   ParLevelService checks UX_ParLevels_Warehouse_Article via
   sp_ParLevel_GetByWarehouseAndArticle before calling this, same
   check-then-insert shape as sp_ArticlePrice_Create.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_Create
(
    @ParLevelToken   UNIQUEIDENTIFIER,
    @WarehouseId     INT,
    @ArticleId       INT,
    @MinimumQuantity DECIMAL(18,4),
    @ReorderQuantity DECIMAL(18,4),
    @CreatedBy       VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ParLevels (ParLevelToken, WarehouseId, ArticleId, MinimumQuantity, ReorderQuantity, CreatedBy)
    VALUES (@ParLevelToken, @WarehouseId, @ArticleId, @MinimumQuantity, @ReorderQuantity, @CreatedBy);

    SELECT
        pl.ParLevelId, pl.ParLevelToken,
        pl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        pl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        pl.MinimumQuantity, pl.ReorderQuantity,
        pl.CreatedUtc, pl.CreatedBy, pl.LastUpdatedUtc, pl.LastUpdatedBy
    FROM dbo.ParLevels pl
    JOIN dbo.Warehouses w ON w.WarehouseId = pl.WarehouseId
    JOIN dbo.Articles a   ON a.ArticleId   = pl.ArticleId
    WHERE pl.ParLevelToken = @ParLevelToken;
END;
GO
