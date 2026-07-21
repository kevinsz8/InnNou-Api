/* =============================================================
   ARTICLE - GET FAMILY IDS BY ARTICLE IDS (batch)
   Used by OrderService's approval-threshold evaluation to resolve every
   line's Family in one round trip instead of one query per line — same
   STRING_SPLIT batch convention as sp_ArticlePackagingLevel_GetByArticleIds.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Article_GetFamilyIdsByArticleIds
    @ArticleIds VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ArticleId, FamilyId
    FROM   Articles
    WHERE  ArticleId IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@ArticleIds, ','));
END;
GO
