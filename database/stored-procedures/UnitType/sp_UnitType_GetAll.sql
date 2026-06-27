CREATE OR ALTER PROCEDURE sp_UnitType_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UnitTypeId,
        UnitTypeToken,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM UnitTypes
    WHERE IsActive = 1
    ORDER BY Code;
END;
