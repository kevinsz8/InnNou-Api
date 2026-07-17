CREATE OR ALTER PROCEDURE sp_Article_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @SupplierId      INT           = NULL,
    @FamilyId        INT           = NULL,
    @SubFamilyId     INT           = NULL,
    @SearchText      VARCHAR(200)  = NULL,
    @IncludeInactive BIT           = 0,
    @OrganizationId  INT           = NULL,
    @FavoritesOnly   BIT           = 0
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
    EffectiveFavorites AS
    (
        SELECT af.ArticleId, af.OrganizationId,
               ROW_NUMBER() OVER (PARTITION BY af.ArticleId ORDER BY oa.Depth ASC) AS rn,
               CASE WHEN oa.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited
        FROM   ArticleFavorites af
        INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = af.OrganizationId
    )
    SELECT
        a.ArticleId,
        a.ArticleToken,
        a.SupplierId,
        s.Name          AS SupplierName,
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
        a.PurchaseQuantity,
        a.ContentUnitId,
        cu.Code         AS ContentUnitCode,
        cu.Symbol       AS ContentUnitSymbol,
        a.ContentQuantity,
        a.BaseUnitId,
        bu.Code         AS BaseUnitCode,
        bu.Symbol       AS BaseUnitSymbol,
        a.MinimumOrderQty,
        a.LeadTimeDays,
        a.IsActive,
        a.IsDeleted,
        a.ReplacedByArticleId,
        r.ArticleToken  AS ReplacedByArticleToken,
        CASE WHEN ef.ArticleId IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsFavorite,
        ISNULL(ef.IsInherited, CAST(0 AS BIT)) AS IsInherited,
        efo.Name        AS FavoriteOrganizationName,
        a.DeletedUtc,
        a.DeletedBy,
        COUNT(*) OVER() AS TotalCount
    FROM   Articles        a
    JOIN   Suppliers       s  ON  s.SupplierId       = a.SupplierId
    JOIN   UnitsOfMeasure  pu ON  pu.UnitOfMeasureId = a.PurchaseUnitId
    JOIN   UnitsOfMeasure  cu ON  cu.UnitOfMeasureId = a.ContentUnitId
    LEFT JOIN UnitsOfMeasure bu ON bu.UnitOfMeasureId = a.BaseUnitId
    LEFT JOIN Families     f  ON  f.FamilyId         = a.FamilyId
    LEFT JOIN SubFamilies  sf ON  sf.SubFamilyId      = a.SubFamilyId
    LEFT JOIN Articles     r  ON  r.ArticleId         = a.ReplacedByArticleId
    LEFT JOIN (SELECT ArticleId, OrganizationId, IsInherited FROM EffectiveFavorites WHERE rn = 1) ef ON ef.ArticleId = a.ArticleId
    LEFT JOIN Organizations efo ON efo.OrganizationId = ef.OrganizationId
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
    ORDER BY s.Name, a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
