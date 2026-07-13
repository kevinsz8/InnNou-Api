SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - GET BY NORMALIZED NAME
   Returns a single active, non-deleted organization by its
   normalized name. Mirrors sp_Role_GetByNormalizedName's shape;
   used by bulk user import to resolve an Excel "OrganizationName"
   column to an OrganizationId without requiring the caller to
   know the organization's token/id.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_GetByNormalizedName
(
    @NormalizedName VARCHAR(200)
)
AS
BEGIN
    SET NOCOUNT ON;

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
        o.IsActive,
        o.IsDeleted
    FROM dbo.Organizations o
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    WHERE o.NormalizedName = @NormalizedName
      AND o.IsActive = 1
      AND o.IsDeleted = 0;
END;
GO
