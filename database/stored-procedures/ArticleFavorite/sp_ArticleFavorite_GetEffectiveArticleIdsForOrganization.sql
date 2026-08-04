SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================================
-- ARTICLE FAVORITE - GET EFFECTIVE ARTICLE IDS FOR ORGANIZATION (membership check, batched)
-- Same ancestor-CTE shape as sp_ArticleFavorite_GetEffective, but answers a different question:
-- given a specific set of ArticleIds (e.g. the articles whose price just changed), which of them
-- are effectively favorited (own ∪ inherited-from-ancestors) for @OrganizationId — not a paged
-- browse of the whole favorites list. Built for SupplierPriceChangeSubscription notifications:
-- one call per subscriber, batched across every changed article in the triggering event.
-- =============================================================
CREATE OR ALTER PROCEDURE sp_ArticleFavorite_GetEffectiveArticleIdsForOrganization
    @OrganizationId INT,
    @ArticleIds     NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationAncestry AS
    (
        SELECT OrganizationId, ParentOrganizationId
        FROM   Organizations
        WHERE  OrganizationId = @OrganizationId
          AND  IsDeleted = 0
          AND  IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId
        FROM   Organizations o
        INNER JOIN OrganizationAncestry oa ON o.OrganizationId = oa.ParentOrganizationId
        WHERE  o.IsDeleted = 0
          AND  o.IsActive  = 1
    ),
    CandidateArticleIds AS
    (
        SELECT TRY_CAST(value AS INT) AS ArticleId FROM STRING_SPLIT(@ArticleIds, ',')
    )
    SELECT DISTINCT af.ArticleId
    FROM   ArticleFavorites af
    INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = af.OrganizationId
    INNER JOIN CandidateArticleIds  ca ON ca.ArticleId      = af.ArticleId;
END;
GO
