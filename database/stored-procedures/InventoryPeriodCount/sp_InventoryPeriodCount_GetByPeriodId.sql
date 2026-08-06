SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   INVENTORYPERIODCOUNT - GET BY PERIOD ID
   Lines for a single InventoryPeriod — populates InventoryPeriodDto.Lines,
   same "eager hydrate, small bounded list" precedent as
   sp_GoodsReceiptLine_GetByGoodsReceiptId.

   FamilyCode/SubFamilyCode: simple LEFT JOIN off Articles.
   CategoryCode/SubCategoryCode: resolved via the same ascending-ancestry
   walk sp_ArticleClassification_GetEffectiveForArticle uses for a single
   article — here every row shares the one Organization the period's own
   Warehouse belongs to (a period is always scoped to one Warehouse), so
   the single-@OrganizationId shape is enough, unlike sp_StockLevel_GetPaged/
   sp_ParLevel_GetBelowPar which needed the set-based per-row version
   (their result can span more than one Organization).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_InventoryPeriodCount_GetByPeriodId
(
    @InventoryPeriodId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OrganizationId INT = (
        SELECT w.OrganizationId
        FROM dbo.InventoryPeriods ip
        JOIN dbo.Warehouses w ON w.WarehouseId = ip.WarehouseId
        WHERE ip.InventoryPeriodId = @InventoryPeriodId
    );

    ;WITH OrganizationAncestry AS
    (
        SELECT OrganizationId, ParentOrganizationId, 0 AS Depth
        FROM   dbo.Organizations
        WHERE  OrganizationId = @OrganizationId
          AND  IsDeleted = 0
          AND  IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId, oa.Depth + 1
        FROM   dbo.Organizations o
        INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE  o.IsDeleted = 0
          AND  o.IsActive  = 1
    ),
    EffectiveArticleClassification AS
    (
        SELECT ac.ArticleId, ac.CategoryId, ac.SubCategoryId,
               ROW_NUMBER() OVER (PARTITION BY ac.ArticleId ORDER BY oa.Depth ASC) AS rn
        FROM   dbo.ArticleClassifications ac
        INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = ac.OrganizationId
    )
    SELECT
        ipc.InventoryPeriodCountId, ipc.InventoryPeriodCountToken, ipc.InventoryPeriodId,
        ipc.ArticleId, a.ArticleToken, a.Name AS ArticleName, a.PurchaseUnitId, u.Code AS PurchaseUnitCode,
        f.Code AS FamilyCode, sf.Code AS SubFamilyCode,
        cat.Code AS CategoryCode, subcat.Code AS SubCategoryCode,
        ipc.OpeningQuantity, ipc.CountedQuantity, ipc.SystemQuantityAtClose, ipc.VarianceQuantity,
        ipc.CountedUnitId, cu.Code AS CountedUnitCode, ipc.CountedQuantityInUnit,
        ipc.CreatedUtc, ipc.CreatedBy, ipc.LastUpdatedUtc, ipc.LastUpdatedBy
    FROM dbo.InventoryPeriodCounts ipc
    JOIN dbo.Articles a ON a.ArticleId = ipc.ArticleId
    JOIN dbo.UnitsOfMeasure u ON u.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN dbo.UnitsOfMeasure cu ON cu.UnitOfMeasureId = ipc.CountedUnitId
    LEFT JOIN dbo.Families f      ON f.FamilyId      = a.FamilyId
    LEFT JOIN dbo.SubFamilies sf  ON sf.SubFamilyId  = a.SubFamilyId
    LEFT JOIN EffectiveArticleClassification eac ON eac.ArticleId = a.ArticleId AND eac.rn = 1
    LEFT JOIN dbo.Categories cat       ON cat.CategoryId = eac.CategoryId
    LEFT JOIN dbo.SubCategories subcat ON subcat.SubCategoryId = eac.SubCategoryId
    WHERE ipc.InventoryPeriodId = @InventoryPeriodId
    ORDER BY a.Name;
END;
GO
