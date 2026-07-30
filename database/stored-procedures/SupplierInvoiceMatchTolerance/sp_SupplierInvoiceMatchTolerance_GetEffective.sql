SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIERINVOICEMATCHTOLERANCE - GET EFFECTIVE
   Nearest-organization-wins resolution: ascending walk from @OrganizationId
   up through its parents, the row belonging to the SHALLOWEST ancestor
   (smallest Depth) wins. Same CTE shape as sp_Article_GetByToken's
   EffectiveFavorites/EffectiveClassifications and sp_ParLevel_GetBelowPar.

   Returns zero rows if no organization in the ancestry chain (including
   @OrganizationId itself) has ever configured a tolerance — the caller
   (CreateAsync) must treat that as a hard-block, not a silent default.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_SupplierInvoiceMatchTolerance_GetEffective
(
    @OrganizationId INT
)
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
    )
    SELECT TOP 1
        t.SupplierInvoiceMatchToleranceId,
        t.SupplierInvoiceMatchToleranceToken,
        t.OrganizationId AS EffectiveOrganizationId,
        org.OrganizationToken AS EffectiveOrganizationToken,
        org.Name         AS EffectiveOrganizationName,
        t.TolerancePercent,
        t.ToleranceAmount,
        CASE WHEN oa.Depth = 0 THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS IsInherited
    FROM SupplierInvoiceMatchTolerances t
    INNER JOIN OrganizationAncestry oa ON oa.OrganizationId = t.OrganizationId
    INNER JOIN Organizations org       ON org.OrganizationId = t.OrganizationId
    ORDER BY oa.Depth ASC;
END;
GO
