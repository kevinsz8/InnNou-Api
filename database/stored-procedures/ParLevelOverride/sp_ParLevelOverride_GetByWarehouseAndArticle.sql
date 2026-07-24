SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVELOVERRIDE - GET BY WAREHOUSE AND ARTICLE
   Two callers: ParLevelService.CreateOverrideAsync passes @Type to fetch
   only same-type candidates for the C#-side overlap check (EVENT vs
   SEASONAL overlap is allowed by design, so they're never compared); the
   "configure par level" panel passes @Type = NULL to list every override
   (both types) for the configuration view.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevelOverride_GetByWarehouseAndArticle
(
    @WarehouseId INT,
    @ArticleId   INT,
    @Type        VARCHAR(20) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.ParLevelOverrideId, o.ParLevelOverrideToken,
        o.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        o.ArticleId, a.ArticleToken, a.Name AS ArticleName,
        t.Code AS Type, o.Label,
        o.MinimumQuantity, o.ReorderQuantity,
        o.StartMonth, o.StartDay, o.EndMonth, o.EndDay,
        o.StartDate, o.EndDate,
        o.CreatedUtc, o.CreatedBy
    FROM dbo.ParLevelOverrides o
    JOIN dbo.Warehouses w ON w.WarehouseId = o.WarehouseId
    JOIN dbo.Articles a   ON a.ArticleId   = o.ArticleId
    JOIN dbo.ParLevelOverrideTypes t ON t.ParLevelOverrideTypeId = o.ParLevelOverrideTypeId
    WHERE o.WarehouseId = @WarehouseId AND o.ArticleId = @ArticleId
      AND (@Type IS NULL OR t.Code = @Type)
    ORDER BY o.CreatedUtc;
END;
GO
