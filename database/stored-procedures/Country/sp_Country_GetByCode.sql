/* =============================================================
   COUNTRY - GET BY CODE
   Internal-only lookup (no ICountryService method — called via raw
   Dapper from ZoneService, same convention as
   sp_Supplier_GetByNormalizedName). Resolves an ISO 3166-1 alpha-2
   code to its CountryId for Zone creation.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_Country_GetByCode
    @Code VARCHAR(2)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CountryId, Code, Name, IsActive
    FROM Countries
    WHERE Code = @Code
      AND IsActive = 1;
END;
GO
