-- ARTICLE CLASSIFICATION - GET EFFECTIVE FOR A SINGLE ARTICLE
-- Single-article resolve used by OrderService.AddLineAsync to snapshot the classification onto
-- a new OrderLine. Same ascending-CTE shape as sp_ArticleFavorite_GetEffective: walks UPWARD from
-- @OrganizationId through ParentOrganizationId, picks the nearest ancestor (including itself,
-- Depth 0) that has its own classification row for this article. No need to special-case
-- SUPER_ASSOCIATE in the walk itself — only a Super Asociado org can ever have a row here
-- (enforced at write time by ArticleClassificationService), so the nearest-ancestor-with-a-row
-- naturally lands on the right owner.
CREATE OR ALTER PROCEDURE sp_ArticleClassification_GetEffectiveForArticle
    @ArticleId      INT,
    @OrganizationId INT
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
    CandidateClassifications AS
    (
        SELECT ac.*, oa.Depth,
               ROW_NUMBER() OVER (PARTITION BY ac.ArticleId ORDER BY oa.Depth ASC) AS rn
        FROM   ArticleClassifications ac
        INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = ac.OrganizationId
        WHERE  ac.ArticleId = @ArticleId
    )
    SELECT
        cc.CategoryId,    c.Code AS CategoryCode,
        cc.SubCategoryId, sc.Code AS SubCategoryCode,
        CASE WHEN cc.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited
    FROM   CandidateClassifications cc
    JOIN   Categories c ON c.CategoryId = cc.CategoryId
    LEFT JOIN SubCategories sc ON sc.SubCategoryId = cc.SubCategoryId
    WHERE  cc.rn = 1;
END;
GO
