CREATE OR ALTER PROCEDURE sp_UnitConversionRate_GetByPair
    @FromUnitOfMeasureId int,
    @ToUnitOfMeasureId   int
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
    WHERE FromUnitOfMeasureId = @FromUnitOfMeasureId
      AND ToUnitOfMeasureId   = @ToUnitOfMeasureId;
END;
