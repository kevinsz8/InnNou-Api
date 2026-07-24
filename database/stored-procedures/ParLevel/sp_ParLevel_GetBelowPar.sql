SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   PARLEVEL - GET BELOW PAR
   Paged list of (Warehouse, Article) pairs whose current StockLevels
   balance is below their effective Minimum for @AsOfDate. Same hierarchy-
   descent CTE shape as sp_StockLevel_GetPaged; same EVENT > SEASONAL > BASE
   resolution as sp_ParLevel_GetEffective (copy-pasted rather than shared,
   this codebase's established per-SP convention). Driven FROM ParLevels —
   a pair only appears here if a base row has been configured for it.

   LeadTimeDays is returned as-is from Articles, never turned into a
   computed urgency/days-until-stockout score — InnNou has no consumption-
   rate data to back that up (see .claude/InventoryModule.md).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_ParLevel_GetBelowPar
(
    @RootOrganizationId INT = NULL,
    @WarehouseId        INT = NULL,
    @ArticleId          INT = NULL,
    @AsOfDate           DATE,
    @PageNumber         INT,
    @PageSize           INT
)
AS
BEGIN
    SET NOCOUNT ON;

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
    SELECT
        pl.ParLevelId, pl.ParLevelToken,
        pl.WarehouseId, w.WarehouseToken, w.Name AS WarehouseName, w.OrganizationId,
        pl.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.LeadTimeDays,
        a.SupplierId, s.Name AS SupplierName,
        a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        ISNULL(sl.QuantityOnHand, 0) AS QuantityOnHand,
        COALESCE(evt.MinimumQuantity, seas.MinimumQuantity, pl.MinimumQuantity) AS EffectiveMinimumQuantity,
        COALESCE(evt.ReorderQuantity, seas.ReorderQuantity, pl.ReorderQuantity) AS EffectiveReorderQuantity,
        CASE WHEN evt.ParLevelOverrideId IS NOT NULL THEN 'EVENT'
             WHEN seas.ParLevelOverrideId IS NOT NULL THEN 'SEASONAL'
             ELSE 'BASE' END AS EffectiveSource,
        COALESCE(evt.Label, seas.Label) AS OverrideLabel,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.ParLevels pl
    JOIN dbo.Warehouses w      ON w.WarehouseId      = pl.WarehouseId
    JOIN dbo.Articles a        ON a.ArticleId        = pl.ArticleId
    JOIN dbo.Suppliers s       ON s.SupplierId       = a.SupplierId
    JOIN dbo.UnitsOfMeasure u  ON u.UnitOfMeasureId  = a.PurchaseUnitId
    LEFT JOIN dbo.StockLevels sl ON sl.WarehouseId = pl.WarehouseId AND sl.ArticleId = pl.ArticleId
    OUTER APPLY (
        SELECT TOP 1 o.ParLevelOverrideId, o.Label, o.MinimumQuantity, o.ReorderQuantity
        FROM dbo.ParLevelOverrides o
        WHERE o.WarehouseId = pl.WarehouseId AND o.ArticleId = pl.ArticleId
          AND o.ParLevelOverrideTypeId = 2 -- EVENT
          AND @AsOfDate BETWEEN o.StartDate AND o.EndDate
    ) evt
    OUTER APPLY (
        SELECT TOP 1 o.ParLevelOverrideId, o.Label, o.MinimumQuantity, o.ReorderQuantity
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
      AND (@RootOrganizationId IS NULL OR EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = w.OrganizationId))
      AND (@WarehouseId IS NULL OR pl.WarehouseId = @WarehouseId)
      AND (@ArticleId IS NULL OR pl.ArticleId = @ArticleId)
    ORDER BY a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
