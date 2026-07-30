SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXJURISDICTION - GET ALL
   Small fixed lookup set (5 rows) — feeds the Warehouse edit form's
   tax-jurisdiction dropdown.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxJurisdiction_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT j.TaxJurisdictionId, j.TaxJurisdictionToken, j.Code, j.Name, j.IsActive,
           j.CountryId, c.Code AS CountryCode, c.Name AS CountryName
    FROM dbo.TaxJurisdictions j
    JOIN dbo.Countries c ON c.CountryId = j.CountryId
    WHERE j.IsActive = 1
    ORDER BY j.TaxJurisdictionId;
END;
GO
