SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXJURISDICTION - CREATE
   Lets a SuperAdmin add a brand-new jurisdiction (e.g. a new country a
   Super Asociado operates in) from the Impuestos admin page, instead of
   requiring a migration the way ES_CEUTA/ES_MELILLA/AD_STANDARD originally
   were seeded. Created with zero TaxRates — same "exists but unconfigured"
   shape Ceuta/Melilla already use; sp_TaxRate_Upsert fills them in row by
   row from the existing grid, no separate seeding step needed here.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxJurisdiction_Create
(
    @CountryId INT,
    @Code      VARCHAR(30),
    @Name      VARCHAR(100)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE CountryId = @CountryId)
    BEGIN
        RAISERROR('TAX_JURISDICTION_COUNTRY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.TaxJurisdictions WHERE Code = @Code)
    BEGIN
        RAISERROR('TAX_JURISDICTION_CODE_ALREADY_EXISTS', 16, 1);
        RETURN;
    END

    DECLARE @NewId INT;

    INSERT INTO dbo.TaxJurisdictions (TaxJurisdictionToken, Code, CountryId, Name, IsActive)
    VALUES (NEWID(), @Code, @CountryId, @Name, 1);

    SET @NewId = SCOPE_IDENTITY();

    SELECT j.TaxJurisdictionId, j.TaxJurisdictionToken, j.Code, j.Name, j.IsActive,
           j.CountryId, c.Code AS CountryCode, c.Name AS CountryName
    FROM dbo.TaxJurisdictions j
    JOIN dbo.Countries c ON c.CountryId = j.CountryId
    WHERE j.TaxJurisdictionId = @NewId;
END;
GO
