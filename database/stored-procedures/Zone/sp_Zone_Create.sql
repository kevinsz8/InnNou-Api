SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_Zone_Create
    @ZoneToken uniqueidentifier,
    @CountryId int,
    @Code      varchar(50),
    @Name      varchar(150),
    @CreatedBy varchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Countries WHERE CountryId = @CountryId AND IsActive = 1)
    BEGIN
        RAISERROR('COUNTRY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Zones WHERE CountryId = @CountryId AND Code = @Code)
    BEGIN
        RAISERROR('ZONE_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO Zones (ZoneToken, CountryId, Code, Name, IsActive, CreatedUtc, CreatedBy)
    VALUES (@ZoneToken, @CountryId, @Code, @Name, 1, SYSUTCDATETIME(), @CreatedBy);

    SELECT
        z.ZoneId, z.ZoneToken, z.CountryId, c.Code AS CountryCode, c.Name AS CountryName,
        z.Code, z.Name, z.IsActive, z.CreatedUtc, z.CreatedBy, z.LastUpdatedUtc, z.LastUpdatedBy
    FROM Zones z
    JOIN Countries c ON c.CountryId = z.CountryId
    WHERE z.ZoneToken = @ZoneToken;
END;
GO
