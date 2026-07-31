SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - EDIT
   Update Minimum/ReorderQuantity for an existing base par level.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_Edit
(
    @ParLevelToken   UNIQUEIDENTIFIER,
    @MinimumQuantity DECIMAL(18,8),
    @ReorderQuantity DECIMAL(18,8),
    @LastUpdatedBy   VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ParLevels
    SET MinimumQuantity = @MinimumQuantity,
        ReorderQuantity = @ReorderQuantity,
        LastUpdatedUtc  = SYSUTCDATETIME(),
        LastUpdatedBy   = @LastUpdatedBy
    WHERE ParLevelToken = @ParLevelToken;

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
