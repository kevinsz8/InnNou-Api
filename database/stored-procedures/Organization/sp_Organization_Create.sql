SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION - CREATE
   Inserts a new organization and returns the full created row
   (joined with its OrganizationType code). If @OrganizationTypeId
   is not supplied, it is derived from the hierarchy position:
   no parent -> SUPER_ASSOCIATE, has a parent -> ASSOCIATE.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Organization_Create
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
    @IsActive             BIT,
    @IsDeleted            BIT,
    @CreatedUtc           DATETIME2(7),
    @CreatedBy            VARCHAR(150)  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedOrganizationTypeId INT = @OrganizationTypeId;
    IF @ResolvedOrganizationTypeId IS NULL
    BEGIN
        SELECT @ResolvedOrganizationTypeId = OrganizationTypeId
        FROM dbo.OrganizationTypes
        WHERE Code = CASE WHEN @ParentOrganizationId IS NULL THEN 'SUPER_ASSOCIATE' ELSE 'ASSOCIATE' END;
    END;

    INSERT INTO dbo.Organizations
    (
        OrganizationToken, Name, NormalizedName, LegalName, Code,
        ParentOrganizationId, OrganizationTypeId, TimeZone, CurrencyCode, LanguageCode,
        IsActive, IsDeleted, CreatedUtc, CreatedBy
    )
    VALUES
    (
        @OrganizationToken, @Name, @NormalizedName, @LegalName, @Code,
        @ParentOrganizationId, @ResolvedOrganizationTypeId, @TimeZone, @CurrencyCode, @LanguageCode,
        @IsActive, @IsDeleted, @CreatedUtc, @CreatedBy
    );

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
