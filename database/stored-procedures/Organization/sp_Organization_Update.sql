SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - UPDATE
   Updates an existing organization's fields and returns the full
   updated row (joined with its OrganizationType code). Only acts
   on non-deleted records. Passing NULL for @OrganizationTypeId
   leaves the existing type unchanged.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_Update
(
    @OrganizationToken    UNIQUEIDENTIFIER,
    @Name                 VARCHAR(200),
    @NormalizedName       VARCHAR(200),
    @LegalName            VARCHAR(250)  = NULL,
    @Code                 VARCHAR(50)   = NULL,
    @ParentOrganizationId INT           = NULL,
    @OrganizationTypeId   INT           = NULL,
    @TimeZone             VARCHAR(100)  = NULL,
    @CurrencyCode         VARCHAR(10)   = NULL,
    @LanguageCode         VARCHAR(10)   = NULL,
    @LastUpdatedUtc       DATETIME2(7),
    @LastUpdatedBy        VARCHAR(150)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Organizations
    SET
        Name                 = @Name,
        NormalizedName       = @NormalizedName,
        LegalName            = @LegalName,
        Code                 = @Code,
        ParentOrganizationId = @ParentOrganizationId,
        OrganizationTypeId   = ISNULL(@OrganizationTypeId, OrganizationTypeId),
        TimeZone             = @TimeZone,
        CurrencyCode         = @CurrencyCode,
        LanguageCode         = @LanguageCode,
        LastUpdatedUtc       = @LastUpdatedUtc,
        LastUpdatedBy        = @LastUpdatedBy
    WHERE OrganizationToken = @OrganizationToken
      AND IsDeleted = 0;

    SELECT
        o.OrganizationId, o.OrganizationToken, o.Name, o.NormalizedName, o.LegalName, o.Code,
        o.ParentOrganizationId, o.OrganizationTypeId, ot.Code AS OrganizationTypeCode,
        o.TimeZone, o.CurrencyCode, o.LanguageCode,
        o.IsActive, o.IsDeleted, o.CreatedUtc, o.CreatedBy,
        o.LastUpdatedUtc, o.LastUpdatedBy, o.DeletedUtc, o.DeletedBy
    FROM dbo.Organizations o
    JOIN dbo.OrganizationTypes ot ON ot.OrganizationTypeId = o.OrganizationTypeId
    WHERE o.OrganizationToken = @OrganizationToken;
END;
GO
