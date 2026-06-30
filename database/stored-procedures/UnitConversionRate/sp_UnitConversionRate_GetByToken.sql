CREATE OR ALTER PROCEDURE sp_UnitConversionRate_GetByToken
    @UnitConversionRateToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.UnitConversionRateId,
        r.UnitConversionRateToken,
        r.FromUnitOfMeasureId,
        r.ToUnitOfMeasureId,
        f.Code   AS FromUOMCode,
        f.Symbol AS FromUOMSymbol,
        t.Code   AS ToUOMCode,
        t.Symbol AS ToUOMSymbol,
        r.Factor,
        r.IsActive,
        r.CreatedUtc,
        r.CreatedBy,
        r.LastUpdatedUtc,
        r.LastUpdatedBy
    FROM UnitConversionRates r
    JOIN UnitsOfMeasure f ON f.UnitOfMeasureId = r.FromUnitOfMeasureId
    JOIN UnitsOfMeasure t ON t.UnitOfMeasureId = r.ToUnitOfMeasureId
    WHERE r.UnitConversionRateToken = @UnitConversionRateToken;
END;
