CREATE OR ALTER PROCEDURE sp_Currency_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CurrencyId, Code, IsActive
    FROM   Currencies
    WHERE  @IncludeInactive = 1 OR IsActive = 1
    ORDER BY Code;
END;
