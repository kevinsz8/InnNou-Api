SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - GET EFFECTIVE
   Single-row resolution of "what is the effective Minimum/Reorder for this
   (Warehouse, Article) as of @AsOfDate", priority EVENT > SEASONAL > BASE.
   This logic is a deliberate, narrow exception to "SPs stay dumb" (same
   justification as sp_StockLevel_ApplyDelta's own DB-layer logic) — it must
   be reused identically here and in sp_ParLevel_GetBelowPar's list query,
   is genuinely SQL-native (date-window resolution), and doing it in C#
   would risk the wrap-around math silently diverging between two
   hand-written copies.

   SEASONAL wrap-around: reduce month/day to MMDD = Month*100+Day
   (monotonic within a year). StartMMDD <= EndMMDD -> normal window,
   AsOfMMDD BETWEEN Start/End. StartMMDD > EndMMDD -> wraps across New
   Year's (e.g. Dec 20 -> Jan 6), AsOfMMDD >= Start OR AsOfMMDD <= End.

   Returns zero rows if no ParLevels base row exists yet for this pair.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_GetEffective
(
    @WarehouseId INT,
    @ArticleId   INT,
    @AsOfDate    DATE
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AsOfMMDD INT = MONTH(@AsOfDate) * 100 + DAY(@AsOfDate);

    SELECT
        pl.ParLevelId, pl.ParLevelToken,
        pl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName,
        pl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.LeadTimeDays,
        pl.MinimumQuantity AS BaseMinimumQuantity, pl.ReorderQuantity AS BaseReorderQuantity,
        COALESCE(evt.MinimumQuantity, seas.MinimumQuantity, pl.MinimumQuantity) AS EffectiveMinimumQuantity,
        COALESCE(evt.ReorderQuantity, seas.ReorderQuantity, pl.ReorderQuantity) AS EffectiveReorderQuantity,
        CASE WHEN evt.ParLevelOverrideId IS NOT NULL THEN 'EVENT'
             WHEN seas.ParLevelOverrideId IS NOT NULL THEN 'SEASONAL'
             ELSE 'BASE' END AS EffectiveSource,
        COALESCE(evt.ParLevelOverrideToken, seas.ParLevelOverrideToken) AS EffectiveOverrideToken,
        COALESCE(evt.Label, seas.Label) AS EffectiveOverrideLabel
    FROM dbo.ParLevels pl
    JOIN dbo.Warehouses w ON w.WarehouseId = pl.WarehouseId
    JOIN dbo.Articles a   ON a.ArticleId   = pl.ArticleId
    OUTER APPLY (
        SELECT TOP 1 o.ParLevelOverrideId, o.ParLevelOverrideToken, o.Label, o.MinimumQuantity, o.ReorderQuantity
        FROM dbo.ParLevelOverrides o
        WHERE o.WarehouseId = pl.WarehouseId AND o.ArticleId = pl.ArticleId
          AND o.ParLevelOverrideTypeId = 2 -- EVENT
          AND @AsOfDate BETWEEN o.StartDate AND o.EndDate
    ) evt
    OUTER APPLY (
        SELECT TOP 1 o.ParLevelOverrideId, o.ParLevelOverrideToken, o.Label, o.MinimumQuantity, o.ReorderQuantity
        FROM dbo.ParLevelOverrides o
        WHERE o.WarehouseId = pl.WarehouseId AND o.ArticleId = pl.ArticleId
          AND o.ParLevelOverrideTypeId = 1 -- SEASONAL
          AND (
                (o.StartMonth * 100 + o.StartDay <= o.EndMonth * 100 + o.EndDay
                    AND @AsOfMMDD BETWEEN (o.StartMonth * 100 + o.StartDay) AND (o.EndMonth * 100 + o.EndDay))
             OR (o.StartMonth * 100 + o.StartDay > o.EndMonth * 100 + o.EndDay
                    AND (@AsOfMMDD >= (o.StartMonth * 100 + o.StartDay) OR @AsOfMMDD <= (o.EndMonth * 100 + o.EndDay)))
          )
    ) seas
    WHERE pl.WarehouseId = @WarehouseId AND pl.ArticleId = @ArticleId;
END;
GO
