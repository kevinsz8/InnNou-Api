CREATE OR ALTER PROCEDURE sp_UnitType_Create
    @UnitTypeToken uniqueidentifier,
    @Code          varchar(50),
    @CreatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM UnitTypes WHERE Code = @Code)
    BEGIN
        RAISERROR('UNIT_TYPE_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO UnitTypes (UnitTypeToken, Code, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@UnitTypeToken, @Code, 0, 1, SYSUTCDATETIME(), @CreatedBy);

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
