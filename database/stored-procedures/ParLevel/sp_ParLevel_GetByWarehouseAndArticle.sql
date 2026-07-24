SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - GET BY WAREHOUSE AND ARTICLE
   Single-row lookup for the base par level, same "no row = not configured"
   convention as sp_StockLevel_GetByWarehouseAndArticle. Used before Create
   (reject duplicate) and before CreateOverride (require an existing base).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_GetByWarehouseAndArticle
(
    @WarehouseId INT,
    @ArticleId   INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pl.ParLevelId, pl.ParLevelToken,
        pl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        pl.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        pl.MinimumQuantity, pl.ReorderQuantity,
        pl.CreatedUtc, pl.CreatedBy, pl.LastUpdatedUtc, pl.LastUpdatedBy
    FROM dbo.ParLevels pl
    JOIN dbo.Warehouses w ON w.WarehouseId = pl.WarehouseId
    JOIN dbo.Articles a   ON a.ArticleId   = pl.ArticleId
    WHERE pl.WarehouseId = @WarehouseId AND pl.ArticleId = @ArticleId;
END;
GO
