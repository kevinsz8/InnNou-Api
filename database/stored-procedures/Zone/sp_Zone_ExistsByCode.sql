CREATE OR ALTER PROCEDURE sp_Zone_ExistsByCode
    @CountryId    INT,
    @Code         VARCHAR(50),
    @ExcludeZoneId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM Zones
        WHERE CountryId = @CountryId
          AND Code = @Code
          AND (@ExcludeZoneId IS NULL OR ZoneId <> @ExcludeZoneId)
    ) THEN 1 ELSE 0 END AS BIT);
END
GO
