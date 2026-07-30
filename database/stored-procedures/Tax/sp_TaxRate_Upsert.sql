SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXRATE - UPSERT
   Creates or updates the current rate for a (Jurisdiction, Category) pair —
   used by the SuperAdmin-only Impuestos page, in particular to fill in
   ES_CEUTA/ES_MELILLA's deliberately-unseeded rates once a real IPSI figure
   is confirmed. Not historized — see 20260730_TaxModule_Create.sql header.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxRate_Upsert
(
    @TaxJurisdictionId INT,
    @TaxCategoryId     INT,
    @RatePercent       DECIMAL(6,3),
    @LastUpdatedBy     VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.TaxJurisdictions WHERE TaxJurisdictionId = @TaxJurisdictionId)
    BEGIN
        RAISERROR('TAX_JURISDICTION_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.TaxCategories WHERE TaxCategoryId = @TaxCategoryId)
    BEGIN
        RAISERROR('TAX_CATEGORY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.TaxRates WHERE TaxJurisdictionId = @TaxJurisdictionId AND TaxCategoryId = @TaxCategoryId)
    BEGIN
        UPDATE dbo.TaxRates
        SET RatePercent    = @RatePercent,
            IsActive       = 1,
            LastUpdatedUtc = SYSUTCDATETIME(),
            LastUpdatedBy  = @LastUpdatedBy
        WHERE TaxJurisdictionId = @TaxJurisdictionId AND TaxCategoryId = @TaxCategoryId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.TaxRates (TaxJurisdictionId, TaxCategoryId, RatePercent, CreatedBy)
        VALUES (@TaxJurisdictionId, @TaxCategoryId, @RatePercent, @LastUpdatedBy);
    END

    SELECT r.TaxRateId, r.TaxRateToken, r.TaxJurisdictionId, r.TaxCategoryId, tc.Code AS TaxCategoryCode, r.RatePercent, r.IsActive
    FROM dbo.TaxRates r
    JOIN dbo.TaxCategories tc ON tc.TaxCategoryId = r.TaxCategoryId
    WHERE r.TaxJurisdictionId = @TaxJurisdictionId AND r.TaxCategoryId = @TaxCategoryId;
END;
GO
