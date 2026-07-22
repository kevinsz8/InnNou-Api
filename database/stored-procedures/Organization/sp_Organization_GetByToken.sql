SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - GET BY TOKEN
   Returns a single organization by its token, scoped per caller
   (see sp_Organization_GetPaged for the @RootOrganizationId/
   @ExactOrganizationId scope semantics). Pass both NULL to bypass
   the scope check entirely (used by OrganizationService when it
   re-fetches unrestricted before applying its own authorization
   for edit/delete).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_GetByToken
(
    @OrganizationToken    UNIQUEIDENTIFIER,
    @RootOrganizationId   INT = NULL,
    @ExactOrganizationId  INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationHierarchy AS
    (
        SELECT o.OrganizationId
        FROM dbo.Organizations o
        WHERE o.OrganizationId = @RootOrganizationId

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
    )
    SELECT
        o.OrganizationId,
        o.OrganizationToken,
        o.Name,
        o.NormalizedName,
        o.LegalName,
        o.Code,
        o.ParentOrganizationId,
        o.OrganizationTypeId,
        ot.Code AS OrganizationTypeCode,
        o.TimeZone,
        o.CurrencyCode,
        o.LanguageCode,
        o.ZoneId,
        z.ZoneToken,
        z.Code AS ZoneCode,
        z.Name AS ZoneName,
        zc.Code AS CountryCode,
        zc.Name AS CountryName,
        o.AddressLine1,
        o.AddressLine2,
        o.City,
        o.State,
        o.PostalCode,
        o.Country,
        o.Description,
        o.IsActive,
        o.IsDeleted,
        o.CreatedUtc,
        o.CreatedBy,
        o.LastUpdatedUtc,
        o.LastUpdatedBy,
        o.DeletedUtc,
        o.DeletedBy
    FROM dbo.Organizations o
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    LEFT JOIN dbo.Zones z ON z.ZoneId = o.ZoneId
    LEFT JOIN dbo.Countries zc ON zc.CountryId = z.CountryId
    WHERE o.OrganizationToken = @OrganizationToken
      AND o.IsDeleted = 0
      AND
      (
          (@RootOrganizationId IS NULL AND @ExactOrganizationId IS NULL)
          OR (@RootOrganizationId IS NOT NULL AND EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = o.OrganizationId))
          OR (@ExactOrganizationId IS NOT NULL AND o.OrganizationId = @ExactOrganizationId)
      );
END;
GO
