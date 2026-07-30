SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXRATE - GET BY JURISDICTION ID
   All active rates for one jurisdiction (at most 4 rows, one per
   TaxCategory) — a GoodsReceipt only ever touches one Warehouse's one
   jurisdiction, so this is fetched once per receipt and matched in-memory
   per line in C#, never queried per-line.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxRate_GetByJurisdictionId
(
    @TaxJurisdictionId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.TaxRateId, r.TaxRateToken, r.TaxJurisdictionId, r.TaxCategoryId,
        tc.Code AS TaxCategoryCode, r.RatePercent, r.IsActive
    FROM dbo.TaxRates r
    JOIN dbo.TaxCategories tc ON tc.TaxCategoryId = r.TaxCategoryId
    WHERE r.TaxJurisdictionId = @TaxJurisdictionId
      AND r.IsActive = 1;
END;
GO
