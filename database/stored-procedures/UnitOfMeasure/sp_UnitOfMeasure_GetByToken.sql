CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_GetByToken
    @UnitOfMeasureToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

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
