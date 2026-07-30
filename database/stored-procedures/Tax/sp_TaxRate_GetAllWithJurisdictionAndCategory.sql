SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXRATE - GET ALL WITH JURISDICTION AND CATEGORY (admin grid)
   Every (Jurisdiction, Category) combination, LEFT JOINed to TaxRates so a
   still-unconfigured pair (e.g. ES_CEUTA/ES_MELILLA, deliberately unseeded —
   see 20260730_TaxModule_Create.sql) shows up with a NULL RatePercent
   instead of being silently absent from the Impuestos admin page.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxRate_GetAllWithJurisdictionAndCategory
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        j.TaxJurisdictionId, j.TaxJurisdictionToken, j.Code AS TaxJurisdictionCode, j.Name AS TaxJurisdictionName,
        tc.TaxCategoryId, tc.TaxCategoryToken, tc.Code AS TaxCategoryCode,
        r.TaxRateId, r.TaxRateToken, r.RatePercent
    FROM dbo.TaxJurisdictions j
    CROSS JOIN dbo.TaxCategories tc
    LEFT JOIN dbo.TaxRates r ON r.TaxJurisdictionId = j.TaxJurisdictionId AND r.TaxCategoryId = tc.TaxCategoryId AND r.IsActive = 1
    WHERE j.IsActive = 1 AND tc.IsActive = 1
    ORDER BY j.TaxJurisdictionId, tc.TaxCategoryId;
END;
GO
