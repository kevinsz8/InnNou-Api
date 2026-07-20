CREATE OR ALTER PROCEDURE sp_Country_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CountryId, Code, Name, IsActive
    FROM   Countries
    WHERE  @IncludeInactive = 1 OR IsActive = 1
    ORDER BY Name;
END;
GO
