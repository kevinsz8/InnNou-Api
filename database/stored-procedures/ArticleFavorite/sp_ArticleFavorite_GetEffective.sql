-- =============================================================
-- ARTICLE FAVORITE - GET EFFECTIVE (own ∪ inherited-from-ancestors)
-- Walks UPWARD from @OrganizationId through ParentOrganizationId
-- (same recursive-CTE shape as sp_Organization_ResolveCurrencyCode)
-- to find every ancestor (including itself, Depth 0), then joins
-- ArticleFavorites against that ancestor set. ROW_NUMBER() PARTITION
-- BY ArticleId ORDER BY Depth ASC dedupes an article favorited at
-- more than one level in the chain and picks the nearest owner —
-- Depth 0 (the querying org's own row) always wins the tie, so
-- IsInherited is false whenever the org marked it itself, and true
-- only when the winning row belongs to an ancestor.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_ArticleFavorite_GetEffective
    @OrganizationId  INT,
    @PageNumber      INT,
    @PageSize        INT,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
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
    CandidateFavorites AS
    (
        SELECT af.*, oa.Depth,
               ROW_NUMBER() OVER (PARTITION BY af.ArticleId ORDER BY oa.Depth ASC) AS rn
        FROM   ArticleFavorites af
        INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = af.OrganizationId
    )
    SELECT
        cf.ArticleFavoriteId, cf.ArticleFavoriteToken,
        cf.ArticleId,      a.ArticleToken, a.Name AS ArticleName, a.SupplierSku,
        s.Name             AS SupplierName,
        cf.OrganizationId, o.OrganizationToken, o.Name AS OrganizationName,
        CASE WHEN cf.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited,
        cf.CreatedUtc, cf.CreatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM   CandidateFavorites cf
    JOIN   Articles      a ON a.ArticleId      = cf.ArticleId
    JOIN   Suppliers     s ON s.SupplierId     = a.SupplierId
    JOIN   Organizations o ON o.OrganizationId = cf.OrganizationId
    WHERE  cf.rn = 1
      AND  a.IsDeleted = 0
      AND  (@IncludeInactive = 1 OR a.IsActive = 1)
      AND  (@SearchText IS NULL OR
            a.NormalizedName LIKE '%' + UPPER(@SearchText) + '%' OR
            a.SupplierSku    LIKE '%' + @SearchText + '%')
    ORDER BY a.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
