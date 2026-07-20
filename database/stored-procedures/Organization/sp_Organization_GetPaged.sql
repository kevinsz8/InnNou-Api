SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - GET PAGED
   Returns a paginated list of organizations, per caller scope
   (resolved in OrganizationService.ResolveScope):
     - @RootOrganizationId and @ExactOrganizationId both NULL:
       unrestricted (all organizations) — SuperAdmin, or Admin
       with no organization assigned.
     - @RootOrganizationId set: that organization's subtree
       (itself + all descendants) — Admin/Manager with an
       organization assigned.
     - @ExactOrganizationId set: exactly that one organization,
       no descendants — anyone below Manager.
   Callers pass at most one of @RootOrganizationId/@ExactOrganizationId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_GetPaged
(
    @RootOrganizationId  INT          = NULL,
    @ExactOrganizationId INT          = NULL,
    @SearchText          VARCHAR(200) = NULL,
    @PageNumber          INT,
    @PageSize            INT,
    @IncludeInactive     BIT          = 0
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
        o.IsActive,
        o.IsDeleted,
        o.CreatedUtc,
        o.CreatedBy,
        o.LastUpdatedUtc,
        o.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Organizations o
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    LEFT JOIN dbo.Zones z ON z.ZoneId = o.ZoneId
    LEFT JOIN dbo.Countries zc ON zc.CountryId = z.CountryId
    WHERE
        o.IsDeleted = 0
        AND (@IncludeInactive = 1 OR o.IsActive = 1)
        AND
        (
            (@RootOrganizationId IS NULL AND @ExactOrganizationId IS NULL)
            OR (@RootOrganizationId IS NOT NULL AND EXISTS (SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = o.OrganizationId))
            OR (@ExactOrganizationId IS NOT NULL AND o.OrganizationId = @ExactOrganizationId)
        )
        AND
        (
            @SearchText IS NULL
            OR LOWER(o.Name)                   LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(o.LegalName, ''))  LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(o.Code, ''))       LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY o.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
