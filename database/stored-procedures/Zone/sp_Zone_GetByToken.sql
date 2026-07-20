CREATE OR ALTER PROCEDURE sp_Zone_GetByToken
    @ZoneToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        z.ZoneId, z.ZoneToken, z.CountryId, c.Code AS CountryCode, c.Name AS CountryName,
        z.Code, z.Name, z.IsActive, z.CreatedUtc, z.CreatedBy, z.LastUpdatedUtc, z.LastUpdatedBy
    FROM Zones z
    JOIN Countries c ON c.CountryId = z.CountryId
    WHERE z.ZoneToken = @ZoneToken;
END;
GO
