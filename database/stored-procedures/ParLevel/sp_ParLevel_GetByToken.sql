SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - GET BY TOKEN
   Used by Edit/Delete to resolve the target row and its WarehouseId (for
   authorization) before writing.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_GetByToken
(
    @ParLevelToken UNIQUEIDENTIFIER
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
    WHERE pl.ParLevelToken = @ParLevelToken;
END;
GO
