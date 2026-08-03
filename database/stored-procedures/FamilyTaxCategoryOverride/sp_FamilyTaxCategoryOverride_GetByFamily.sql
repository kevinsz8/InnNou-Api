SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   FAMILYTAXCATEGORYOVERRIDE - GET BY FAMILY
   Feeds the Family edit form's per-jurisdiction override list — one row per
   TaxJurisdiction this Family has an explicit override for. Jurisdictions
   with no row here fall back to Families.DefaultTaxCategoryId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_FamilyTaxCategoryOverride_GetByFamily
(
    @FamilyId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.FamilyTaxCategoryOverrideId, o.FamilyTaxCategoryOverrideToken, o.FamilyId, o.IsActive,
        j.TaxJurisdictionId, j.TaxJurisdictionToken, j.Code AS TaxJurisdictionCode, j.Name AS TaxJurisdictionName,
        c.TaxCategoryId, c.TaxCategoryToken, c.Code AS TaxCategoryCode
    FROM dbo.FamilyTaxCategoryOverrides o
    JOIN dbo.TaxJurisdictions j ON j.TaxJurisdictionId = o.TaxJurisdictionId
    JOIN dbo.TaxCategories c ON c.TaxCategoryId = o.TaxCategoryId
    WHERE o.FamilyId = @FamilyId AND o.IsActive = 1
    ORDER BY j.Code;
END;
GO
