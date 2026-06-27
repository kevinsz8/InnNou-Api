CREATE OR ALTER PROCEDURE sp_UnitConversionRate_GetByToken
    @UnitConversionRateToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UnitConversionRateId,
        UnitConversionRateToken,
        FromUnitOfMeasureId,
        ToUnitOfMeasureId,
        Factor,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM UnitConversionRates
    WHERE UnitConversionRateToken = @UnitConversionRateToken;
END;
