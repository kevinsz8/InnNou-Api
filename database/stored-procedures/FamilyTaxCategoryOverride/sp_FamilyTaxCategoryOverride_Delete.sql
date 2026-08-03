SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   FAMILYTAXCATEGORYOVERRIDE - DELETE
   Removes a Family's jurisdiction-specific override, reverting that
   jurisdiction back to Families.DefaultTaxCategoryId. Hard delete — this
   table is admin configuration, not a historical/audit record (same shape
   as sp_TaxRate_Upsert's own rows), so nothing downstream depends on a
   deleted override ever being recoverable.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_FamilyTaxCategoryOverride_Delete
(
    @FamilyId          INT,
    @TaxJurisdictionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.FamilyTaxCategoryOverrides WHERE FamilyId = @FamilyId AND TaxJurisdictionId = @TaxJurisdictionId)
    BEGIN
        RAISERROR('FAMILY_TAX_CATEGORY_OVERRIDE_NOT_FOUND', 16, 1);
        RETURN;
    END

    DELETE FROM dbo.FamilyTaxCategoryOverrides
    WHERE FamilyId = @FamilyId AND TaxJurisdictionId = @TaxJurisdictionId;
END;
GO
