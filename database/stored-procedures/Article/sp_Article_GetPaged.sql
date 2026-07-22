CREATE OR ALTER PROCEDURE sp_Article_GetPaged
(
    @PageNumber        INT,
    @PageSize          INT,
    @SupplierId        INT           = NULL,
    @FamilyId          INT           = NULL,
    @SubFamilyId       INT           = NULL,
    @SearchText        VARCHAR(200)  = NULL,
    @IncludeInactive   BIT           = 0,
    @OrganizationId    INT           = NULL,
    @FavoritesOnly     BIT           = 0,
    @ContextRoleLevel  INT           = 100, -- defaults to "bypass" (SuperAdmin-equivalent) so every existing
                                             -- internal call site (Edit/Supersede/SetActive/Delete/BulkImport/
                                             -- ArticlePriceService's plain article lookups, none of which pass
                                             -- this param) keeps working unchanged for a private supplier's own
                                             -- article. Only the real catalog-browse path (ArticleService.
                                             -- GetPagedAsync) and the Order/OrderTemplate add-line paths
                                             -- explicitly override this — the latter pass 0 on purpose, to
                                             -- force strict enforcement against the ORDER'S/TEMPLATE'S own
                                             -- organization regardless of the acting user's role.
    @ContextSupplierId INT           = NULL -- lets a supplier-scoped caller see its own (possibly private) catalog
)
AS
BEGIN
    SET NOCOUNT ON;

    -- Ancestor walk + effective-favorite resolution, same shape as
    -- sp_ArticleFavorite_GetEffective — lets the catalog list carry
    -- IsFavorite/IsInherited per row instead of requiring a separate
    -- favorites-only endpoint. @OrganizationId = NULL (supplier-scoped
    -- or org-less callers) makes the CTE anchor match nothing, so
    -- IsFavorite is always 0 for those callers.
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
    -- viewer), same shape as sp_Organization_IsInHierarchy's own body.
    -- Deliberately a SEPARATE CTE from OrganizationAncestry above:
    -- favorites cascade DOWN from an ancestor's marking to descendants
    -- (so a descendant queries UP for them, hence OrganizationAncestry's
    -- ascending walk), while a private supplier's visibility cascades
    -- UP from the owning descendant to its ancestors (so a viewer
    -- queries DOWN from itself to find suppliers owned within its own
    -- subtree). Do not conflate the two directions.
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
    -- ArticleClassifications instead — a Category/SubCategory assignment cascades down
    -- from the nearest Super Asociado ancestor exactly like a favorite does.
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
        a.DeletedBy,
        COUNT(*) OVER() AS TotalCount
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
    WHERE  a.IsDeleted = 0
      AND  (@IncludeInactive = 1 OR a.IsActive = 1)
      AND  (@SupplierId  IS NULL OR a.SupplierId  = @SupplierId)
      AND  (@FamilyId    IS NULL OR a.FamilyId    = @FamilyId)
      AND  (@SubFamilyId IS NULL OR a.SubFamilyId = @SubFamilyId)
      AND  (@SearchText  IS NULL OR
            a.NormalizedName LIKE '%' + UPPER(@SearchText) + '%' OR
            a.SupplierSku    LIKE '%' + @SearchText + '%' OR
            a.Barcode        LIKE '%' + @SearchText + '%' OR
            a.Brand          LIKE '%' + @SearchText + '%')
      AND  (@FavoritesOnly = 0 OR ef.ArticleId IS NOT NULL)
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
          )
    ORDER BY s.Name, a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
