SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   SUPPLIER - GET PRIVATIZATION IMPACT
   Called before a SuperAdmin-initiated Global->Private or
   owner-reassignment edit is applied (see InnNou-Api CLAUDE.md,
   "Supplier global/private scoping"). Returns counts of OTHER
   organizations — excluding the new owner and the new owner's own
   ancestors, i.e. excluding every org that will still be able to
   see this supplier after the change — that currently have
   ArticleFavorites, DRAFT Order lines, or OrderTemplate lines
   referencing this supplier's articles, and would therefore lose
   access as a result of the change. @NewOwnerOrganizationId = NULL
   means the supplier is becoming Global (nothing is ever lost by
   that direction — callers should not invoke this procedure for a
   Private->Global change at all, but NULL is handled safely below
   regardless).

   Note the THIRD distinct hierarchy-CTE direction in this file —
   ascending from the NEW OWNER (not the caller, and not the
   descending walk used for supplier visibility itself) — since what
   we need here is "every organization that will still be able to
   see the supplier after the change", which is {new owner} union
   {new owner's ancestors}.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Supplier_GetPrivatizationImpact
(
    @SupplierId             INT,
    @NewOwnerOrganizationId INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH RetainedOrganizations AS
    (
        SELECT OrganizationId, ParentOrganizationId
        FROM dbo.Organizations
        WHERE OrganizationId = @NewOwnerOrganizationId
          AND IsDeleted = 0
          AND IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId, o.ParentOrganizationId
        FROM dbo.Organizations o
        INNER JOIN RetainedOrganizations ro ON o.OrganizationId = ro.ParentOrganizationId
        WHERE o.IsDeleted = 0
          AND o.IsActive  = 1
    ),
    ImpactedFavoriteOrgs AS
    (
        SELECT DISTINCT af.OrganizationId
        FROM dbo.ArticleFavorites af
        JOIN dbo.Articles a ON a.ArticleId = af.ArticleId
        WHERE a.SupplierId = @SupplierId
          AND af.OrganizationId NOT IN (SELECT OrganizationId FROM RetainedOrganizations)
    ),
    ImpactedOrderOrgs AS
    (
        SELECT DISTINCT ord.OrganizationId
        FROM dbo.[Order] ord
        JOIN dbo.OrderLine ol ON ol.OrderId = ord.OrderId
        JOIN dbo.Articles a   ON a.ArticleId = ol.ArticleId
        WHERE a.SupplierId = @SupplierId
          AND ord.Status = 'DRAFT'
          AND ord.OrganizationId NOT IN (SELECT OrganizationId FROM RetainedOrganizations)
    ),
    ImpactedTemplateOrgs AS
    (
        SELECT DISTINCT ot.OrganizationId
        FROM dbo.OrderTemplate ot
        JOIN dbo.OrderTemplateLine otl ON otl.OrderTemplateId = ot.OrderTemplateId
        JOIN dbo.Articles a            ON a.ArticleId          = otl.ArticleId
        WHERE a.SupplierId = @SupplierId
          AND ot.OrganizationId NOT IN (SELECT OrganizationId FROM RetainedOrganizations)
    )
    SELECT
        (SELECT COUNT(*) FROM ImpactedFavoriteOrgs) AS ImpactedFavoriteOrganizationCount,
        (SELECT COUNT(*) FROM ImpactedOrderOrgs)    AS ImpactedDraftOrderOrganizationCount,
        (SELECT COUNT(*) FROM ImpactedTemplateOrgs) AS ImpactedTemplateOrganizationCount,
        (SELECT COUNT(*) FROM (
            SELECT OrganizationId FROM ImpactedFavoriteOrgs
            UNION SELECT OrganizationId FROM ImpactedOrderOrgs
            UNION SELECT OrganizationId FROM ImpactedTemplateOrgs
        ) AllImpacted) AS TotalImpactedOrganizationCount;
END;
GO
