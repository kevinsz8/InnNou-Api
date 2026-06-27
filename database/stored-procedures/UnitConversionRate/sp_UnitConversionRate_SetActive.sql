CREATE OR ALTER PROCEDURE sp_UnitConversionRate_SetActive
    @UnitConversionRateToken uniqueidentifier,
    @IsActive                bit,
    @LastUpdatedBy           nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitConversionRates WHERE UnitConversionRateToken = @UnitConversionRateToken)
    BEGIN
        RAISERROR('CONVERSION_NOT_FOUND', 16, 1);
        RETURN;
    END

    UPDATE UnitConversionRates
    SET    IsActive       = @IsActive,
           LastUpdatedUtc  = SYSUTCDATETIME(),
           LastUpdatedBy   = @LastUpdatedBy
    WHERE  UnitConversionRateToken = @UnitConversionRateToken;

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
