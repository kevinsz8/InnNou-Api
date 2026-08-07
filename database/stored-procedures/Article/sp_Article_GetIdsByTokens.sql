/* =============================================================
   ARTICLE - GET IDS BY TOKENS (batch)
   Resolves a batch of ArticleTokens to (ArticleId, ArticleToken, SupplierId) in a single round
   trip, applying the same organization/supplier visibility rule as sp_Article_GetByToken/
   sp_Article_GetPaged (see either file's header comment for the two-CTE-direction reasoning —
   only the descending OrganizationDescendants walk is needed here, since this SP never surfaces
   favorites/classifications). Used by BulkAssignArticleClassificationCommandHandler to avoid the
   previous one-round-trip-per-token N+1 (same STRING_SPLIT batch convention as
   sp_Article_GetFamilyIdsByArticleIds/sp_ArticlePackagingLevel_GetByArticleIds).

   The caller must still apply the same extra narrowing ArticleService.GetByTokenAsync applies
   after this SP returns: a supplier-scoped caller keeps only rows whose SupplierId matches its
   own, and a caller with neither a SupplierId nor an OrganizationId below Admin role sees
   nothing at all — this SP's own WHERE is intentionally the same *broader* OR-block
   sp_Article_GetByToken/GetPaged use, narrowed further in C# exactly like those two are.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Article_GetIdsByTokens
    @ArticleTokens     VARCHAR(MAX), -- comma-separated list of ArticleToken GUIDs
    @OrganizationId    INT = NULL,
    @ContextRoleLevel  INT = 100, -- see sp_Article_GetByToken's own header comment for why this
                                   -- defaults to "bypass"
    @ContextSupplierId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationDescendants AS
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
    )
    SELECT
        a.ArticleId,
        a.ArticleToken,
        a.SupplierId
    FROM   Articles a
    JOIN   Suppliers s ON s.SupplierId = a.SupplierId
    WHERE  a.ArticleToken IN (SELECT CAST(value AS UNIQUEIDENTIFIER) FROM STRING_SPLIT(@ArticleTokens, ','))
      AND  a.IsDeleted = 0
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
GO
