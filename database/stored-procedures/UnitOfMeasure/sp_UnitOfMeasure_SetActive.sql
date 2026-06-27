CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_SetActive
    @UnitOfMeasureToken uniqueidentifier,
    @IsActive           bit,
    @LastUpdatedBy      nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE UnitOfMeasureToken = @UnitOfMeasureToken)
    BEGIN
        RAISERROR('UNIT_OF_MEASURE_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE UnitOfMeasureToken = @UnitOfMeasureToken AND IsSystem = 1)
    BEGIN
        RAISERROR('UNIT_OF_MEASURE_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    UPDATE UnitsOfMeasure
    SET    IsActive       = @IsActive,
           LastUpdatedUtc  = SYSUTCDATETIME(),
           LastUpdatedBy   = @LastUpdatedBy
    WHERE  UnitOfMeasureToken = @UnitOfMeasureToken;

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
