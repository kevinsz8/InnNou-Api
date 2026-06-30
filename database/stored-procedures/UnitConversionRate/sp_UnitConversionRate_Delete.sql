CREATE OR ALTER PROCEDURE sp_UnitConversionRate_Delete
    @UnitConversionRateToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitConversionRates WHERE UnitConversionRateToken = @UnitConversionRateToken)
    BEGIN
        RAISERROR('CONVERSION_NOT_FOUND', 16, 1);
        RETURN;
    END

    DELETE FROM UnitConversionRates
    OUTPUT
        DELETED.UnitConversionRateId,
        DELETED.UnitConversionRateToken,
        DELETED.FromUnitOfMeasureId,
        DELETED.ToUnitOfMeasureId,
        DELETED.Factor,
        DELETED.IsActive,
        DELETED.CreatedUtc,
        DELETED.CreatedBy,
        DELETED.LastUpdatedUtc,
        DELETED.LastUpdatedBy
    WHERE UnitConversionRateToken = @UnitConversionRateToken;
END;
