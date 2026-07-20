SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE sp_Zone_Update
    @ZoneToken     uniqueidentifier,
    @Code          varchar(50),
    @Name          varchar(150),
    @LastUpdatedBy varchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CountryId int;
    SELECT @CountryId = CountryId FROM Zones WHERE ZoneToken = @ZoneToken;

    IF @CountryId IS NULL
    BEGIN
        RAISERROR('ZONE_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Zones WHERE CountryId = @CountryId AND Code = @Code AND ZoneToken <> @ZoneToken)
    BEGIN
        RAISERROR('ZONE_CODE_EXISTS', 16, 1);
        RETURN;
    END

    UPDATE Zones
    SET    Code           = @Code,
           Name           = @Name,
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
