CREATE OR ALTER PROCEDURE sp_Zone_SetActive
    @ZoneToken     uniqueidentifier,
    @IsActive      bit,
    @LastUpdatedBy varchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Zones WHERE ZoneToken = @ZoneToken)
    BEGIN
        RAISERROR('ZONE_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE Zones
    SET    IsActive       = @IsActive,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
    WHERE  ZoneToken = @ZoneToken;

    SELECT
        z.ZoneId, z.ZoneToken, z.CountryId, c.Code AS CountryCode, c.Name AS CountryName,
        z.Code, z.Name, z.IsActive, z.CreatedUtc, z.CreatedBy, z.LastUpdatedUtc, z.LastUpdatedBy
    FROM Zones z
    JOIN Countries c ON c.CountryId = z.CountryId
    WHERE z.ZoneToken = @ZoneToken;
END;
GO
