SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DASHBOARD - GET BELOW PAR COUNT
   Same EVENT > SEASONAL > BASE resolution block as sp_ParLevel_GetBelowPar,
   copy-pasted per this codebase's own established convention for this
   exact logic (its own header comment documents the same choice against
   sp_ParLevel_GetEffective) — collapsed to a COUNT, no tokens/names/
   pagination needed for a dashboard tile.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard_GetBelowParCount
(
    @RootOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AsOfDate DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @AsOfMMDD INT = MONTH(@AsOfDate) * 100 + DAY(@AsOfDate);

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @RootOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT COUNT(*) AS BelowParCount
    FROM dbo.ParLevels pl
    JOIN dbo.Warehouses w ON w.WarehouseId = pl.WarehouseId
    LEFT JOIN dbo.StockLevels sl ON sl.WarehouseId = pl.WarehouseId AND sl.ArticleId = pl.ArticleId
    OUTER APPLY (
        SELECT TOP 1 o.MinimumQuantity
        FROM dbo.ParLevelOverrides o
        WHERE o.WarehouseId = pl.WarehouseId AND o.ArticleId = pl.ArticleId
          AND o.ParLevelOverrideTypeId = 2 -- EVENT
          AND @AsOfDate BETWEEN o.StartDate AND o.EndDate
    ) evt
    OUTER APPLY (
        SELECT TOP 1 o.MinimumQuantity
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
    WHERE ISNULL(sl.QuantityOnHand, 0) < COALESCE(evt.MinimumQuantity, seas.MinimumQuantity, pl.MinimumQuantity)
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = w.OrganizationId));
END;
GO
