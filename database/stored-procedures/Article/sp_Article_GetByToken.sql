CREATE OR ALTER PROCEDURE sp_Article_GetByToken
    @ArticleToken   UNIQUEIDENTIFIER,
    @OrganizationId INT = NULL
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
        efo.Name        AS FavoriteOrganizationName
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
    WHERE  a.ArticleToken = @ArticleToken
      AND  a.IsDeleted    = 0;
END;
