SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   TAXJURISDICTION - GET BY TOKEN
   Resolves a caller-supplied token to its internal Id — used by
   WarehouseService when writing Warehouses.TaxJurisdictionId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_TaxJurisdiction_GetByToken
(
    @TaxJurisdictionToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT j.TaxJurisdictionId, j.TaxJurisdictionToken, j.Code, j.Name, j.IsActive,
           j.CountryId, c.Code AS CountryCode, c.Name AS CountryName
    FROM dbo.TaxJurisdictions j
    JOIN dbo.Countries c ON c.CountryId = j.CountryId
    WHERE j.TaxJurisdictionToken = @TaxJurisdictionToken;
END;
GO
