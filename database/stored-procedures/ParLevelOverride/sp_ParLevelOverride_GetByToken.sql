SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVELOVERRIDE - GET BY TOKEN
   Used by DeleteOverrideAsync to resolve the target row and its
   WarehouseId (for authorization) before deleting.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevelOverride_GetByToken
(
    @ParLevelOverrideToken UNIQUEIDENTIFIER
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
    WHERE o.ParLevelOverrideToken = @ParLevelOverrideToken;
END;
GO
