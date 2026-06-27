CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_Create
    @UnitOfMeasureToken uniqueidentifier,
    @UnitTypeId         int,
    @Code               varchar(50),
    @Symbol             varchar(25),
    @Decimals           int = 0,
    @CreatedBy          nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitTypes WHERE UnitTypeId = @UnitTypeId AND IsActive = 1)
    BEGIN
        RAISERROR('UNIT_TYPE_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE Code = @Code)
    BEGIN
        RAISERROR('UNIT_OF_MEASURE_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO UnitsOfMeasure (UnitOfMeasureToken, UnitTypeId, Code, Symbol, Decimals, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@UnitOfMeasureToken, @UnitTypeId, @Code, @Symbol, @Decimals, 0, 1, SYSUTCDATETIME(), @CreatedBy);

    SELECT
        UnitOfMeasureId,
        UnitOfMeasureToken,
        UnitTypeId,
        Code,
        Symbol,
        Decimals,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM UnitsOfMeasure
    WHERE UnitOfMeasureToken = @UnitOfMeasureToken;
END;
