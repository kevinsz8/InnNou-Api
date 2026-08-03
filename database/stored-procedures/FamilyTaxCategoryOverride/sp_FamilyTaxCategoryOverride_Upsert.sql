SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   FAMILYTAXCATEGORYOVERRIDE - UPSERT
   Sets (or replaces) the tax category a Family resolves to in one specific
   TaxJurisdiction, overriding Families.DefaultTaxCategoryId for receipts at
   a Warehouse in that jurisdiction only. See
   20260805_FamilyTaxCategoryOverrides_Create.sql migration header for the
   full design rationale (SAP/Odoo/Avalara research).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_FamilyTaxCategoryOverride_Upsert
(
    @FamilyId          INT,
    @TaxJurisdictionId INT,
    @TaxCategoryId     INT,
    @LastUpdatedBy     VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Families WHERE FamilyId = @FamilyId)
    BEGIN
        RAISERROR('FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

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

    IF EXISTS (SELECT 1 FROM dbo.FamilyTaxCategoryOverrides WHERE FamilyId = @FamilyId AND TaxJurisdictionId = @TaxJurisdictionId)
    BEGIN
        UPDATE dbo.FamilyTaxCategoryOverrides
        SET TaxCategoryId   = @TaxCategoryId,
            IsActive        = 1,
            LastUpdatedUtc  = SYSUTCDATETIME(),
            LastUpdatedBy   = @LastUpdatedBy
        WHERE FamilyId = @FamilyId AND TaxJurisdictionId = @TaxJurisdictionId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.FamilyTaxCategoryOverrides (FamilyId, TaxJurisdictionId, TaxCategoryId, CreatedBy)
        VALUES (@FamilyId, @TaxJurisdictionId, @TaxCategoryId, @LastUpdatedBy);
    END

    SELECT
        o.FamilyTaxCategoryOverrideId, o.FamilyTaxCategoryOverrideToken, o.FamilyId, o.IsActive,
        j.TaxJurisdictionId, j.TaxJurisdictionToken, j.Code AS TaxJurisdictionCode, j.Name AS TaxJurisdictionName,
        c.TaxCategoryId, c.TaxCategoryToken, c.Code AS TaxCategoryCode
    FROM dbo.FamilyTaxCategoryOverrides o
    JOIN dbo.TaxJurisdictions j ON j.TaxJurisdictionId = o.TaxJurisdictionId
    JOIN dbo.TaxCategories c ON c.TaxCategoryId = o.TaxCategoryId
    WHERE o.FamilyId = @FamilyId AND o.TaxJurisdictionId = @TaxJurisdictionId;
END;
GO
