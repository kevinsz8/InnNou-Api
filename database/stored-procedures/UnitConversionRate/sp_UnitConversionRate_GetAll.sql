CREATE OR ALTER PROCEDURE sp_UnitConversionRate_GetAll
    @UnitTypeId int = NULL   -- optional filter; NULL returns all active rates
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.UnitConversionRateId,
        r.UnitConversionRateToken,
        r.FromUnitOfMeasureId,
        r.ToUnitOfMeasureId,
        r.Factor,
        r.IsActive,
        r.CreatedUtc,
        r.CreatedBy,
        r.LastUpdatedUtc,
        r.LastUpdatedBy
    FROM UnitConversionRates r
    JOIN UnitsOfMeasure f ON f.UnitOfMeasureId = r.FromUnitOfMeasureId
    WHERE r.IsActive = 1
      AND (@UnitTypeId IS NULL OR f.UnitTypeId = @UnitTypeId)
    ORDER BY r.FromUnitOfMeasureId, r.ToUnitOfMeasureId;
END;
