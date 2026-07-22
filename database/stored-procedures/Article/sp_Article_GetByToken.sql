CREATE OR ALTER PROCEDURE sp_Article_GetByToken
    @ArticleToken      UNIQUEIDENTIFIER,
    @OrganizationId    INT = NULL,
    @ContextRoleLevel  INT = 100, -- defaults to "bypass" (SuperAdmin-equivalent) so every existing internal
                                   -- call site (EditAsync/SupersedeAsync/SetActiveAsync/DeleteAsync/
                                   -- BulkImportArticlesAsync/ArticlePriceService's plain article lookups, none
                                   -- of which pass this param) keeps working unchanged for a private
                                   -- supplier's own article — those are ownership-gated via
                                   -- ArticleService.CanManage AFTER the fetch, not by hiding the row here.
                                   -- Only ArticleService.GetByTokenAsync (the real catalog-browse path) and
                                   -- the Order/OrderTemplate add-line paths explicitly override this — the
                                   -- latter pass 0 on purpose, to force strict enforcement against the
                                   -- ORDER'S/TEMPLATE'S own organization regardless of the acting user's role.
    @ContextSupplierId INT = NULL  -- lets a supplier-scoped caller see its own (possibly private) catalog
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationAncestry AS
    (
        SELECT OrganizationId, ParentOrganizationId, 0 AS Depth
        FROM   Organizations
        WHERE  OrganizationId = @OrganizationId
          AND  IsDeleted = 0
          AND  IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId, oa.Depth + 1
        FROM   Organizations o
        INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE  o.IsDeleted = 0
          AND  o.IsActive  = 1
    ),
    -- Supplier-visibility walk — DESCENDING from @OrganizationId (the
    -- viewer). Deliberately separate from OrganizationAncestry above —
    -- see sp_Article_GetPaged.sql's header comment for the full
    -- reasoning on why these two CTEs must not be conflated.
    OrganizationDescendants AS
    (
        SELECT OrganizationId, ParentOrganizationId
        FROM   Organizations
        WHERE  OrganizationId = @OrganizationId
          AND  IsDeleted = 0
          AND  IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId
        FROM   Organizations o
        INNER JOIN OrganizationDescendants od ON o.ParentOrganizationId = od.OrganizationId
        WHERE  o.IsDeleted = 0
          AND  o.IsActive  = 1
    ),
    EffectiveFavorites AS
    (
        SELECT af.ArticleId, af.OrganizationId,
               ROW_NUMBER() OVER (PARTITION BY af.ArticleId ORDER BY oa.Depth ASC) AS rn,
               CASE WHEN oa.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited
        FROM   ArticleFavorites af
        INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = af.OrganizationId
    ),
    -- Same ascending-walk shape as EffectiveFavorites above, applied to
    -- ArticleClassifications instead — see sp_Article_GetPaged.sql's header comment.
    EffectiveClassifications AS
    (
        SELECT ac.ArticleId, ac.OrganizationId, ac.CategoryId, ac.SubCategoryId,
               ROW_NUMBER() OVER (PARTITION BY ac.ArticleId ORDER BY oa.Depth ASC) AS rn,
               CASE WHEN oa.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited
        FROM   ArticleClassifications ac
        INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = ac.OrganizationId
    )
    SELECT
        a.ArticleId,
        a.ArticleToken,
        a.SupplierId,
        s.Name          AS SupplierName,
        st.Code         AS SupplierType,
        a.Name,
        a.NormalizedName,
        a.Description,
        a.SupplierSku,
        a.Barcode,
        a.Brand,
        a.FamilyId,
        f.Code          AS FamilyCode,
        a.SubFamilyId,
        sf.Code         AS SubFamilyCode,
        a.PurchaseUnitId,
        pu.Code         AS PurchaseUnitCode,
        pu.Symbol       AS PurchaseUnitSymbol,
        a.MinimumOrderQty,
        a.LeadTimeDays,
        a.IsActive,
        a.IsDeleted,
        a.ReplacedByArticleId,
        r.ArticleToken  AS ReplacedByArticleToken,
        CASE WHEN ef.ArticleId IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsFavorite,
        ISNULL(ef.IsInherited, CAST(0 AS BIT)) AS IsInherited,
        efo.Name        AS FavoriteOrganizationName,
        ec.CategoryId,
        cat.CategoryToken,
        cat.Code        AS CategoryCode,
        ec.SubCategoryId,
        subcat.SubCategoryToken,
        subcat.Code     AS SubCategoryCode,
        ISNULL(ec.IsInherited, CAST(0 AS BIT)) AS IsCategoryInherited,
        eco.Name        AS ClassificationOrganizationName,
        a.DeletedUtc,
        a.DeletedBy
    FROM   Articles        a
    JOIN   Suppliers       s  ON  s.SupplierId       = a.SupplierId
    JOIN   SupplierTypes   st ON  st.SupplierTypeId  = s.SupplierTypeId
    JOIN   UnitsOfMeasure  pu ON  pu.UnitOfMeasureId = a.PurchaseUnitId
    LEFT JOIN Families     f  ON  f.FamilyId         = a.FamilyId
    LEFT JOIN SubFamilies  sf ON  sf.SubFamilyId      = a.SubFamilyId
    LEFT JOIN Articles     r  ON  r.ArticleId         = a.ReplacedByArticleId
    LEFT JOIN (SELECT ArticleId, OrganizationId, IsInherited FROM EffectiveFavorites WHERE rn = 1) ef ON ef.ArticleId = a.ArticleId
    LEFT JOIN Organizations efo ON efo.OrganizationId = ef.OrganizationId
    LEFT JOIN (SELECT ArticleId, OrganizationId, CategoryId, SubCategoryId, IsInherited FROM EffectiveClassifications WHERE rn = 1) ec ON ec.ArticleId = a.ArticleId
    LEFT JOIN Categories    cat    ON cat.CategoryId       = ec.CategoryId
    LEFT JOIN SubCategories subcat ON subcat.SubCategoryId = ec.SubCategoryId
    LEFT JOIN Organizations eco    ON eco.OrganizationId   = ec.OrganizationId
    WHERE  a.ArticleToken = @ArticleToken
      AND  a.IsDeleted    = 0
      AND  (
            @ContextRoleLevel >= 100
            OR (@ContextSupplierId IS NOT NULL AND a.SupplierId = @ContextSupplierId)
            OR s.IsGlobal = 1
            OR EXISTS (
                SELECT 1 FROM OrganizationSuppliers os
                JOIN OrganizationDescendants od ON od.OrganizationId = os.OrganizationId
                WHERE os.SupplierId = a.SupplierId
                  AND os.IsActive = 1
            )
          );
END;
