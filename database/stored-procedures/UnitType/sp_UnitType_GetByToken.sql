CREATE OR ALTER PROCEDURE sp_UnitType_GetByToken
    @UnitTypeToken uniqueidentifier
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
    WHERE UnitTypeToken = @UnitTypeToken;
END;
