CREATE OR ALTER PROCEDURE sp_UnitConversionRate_Create
    @UnitConversionRateToken uniqueidentifier,
    @FromUnitOfMeasureId     int,
    @ToUnitOfMeasureId       int,
    @Factor                  decimal(18,8),
    @CreatedBy               nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    -- Both units must exist and be active
    IF NOT EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE UnitOfMeasureId = @FromUnitOfMeasureId AND IsActive = 1)
    BEGIN
        RAISERROR('FROM_UNIT_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE UnitOfMeasureId = @ToUnitOfMeasureId AND IsActive = 1)
    BEGIN
        RAISERROR('TO_UNIT_NOT_FOUND', 16, 1);
        RETURN;
    END

    -- Units must belong to the same UnitType (no cross-type conversions)
    IF NOT EXISTS (
        SELECT 1
        FROM UnitsOfMeasure f
        JOIN UnitsOfMeasure t ON t.UnitTypeId = f.UnitTypeId
        WHERE f.UnitOfMeasureId = @FromUnitOfMeasureId
          AND t.UnitOfMeasureId = @ToUnitOfMeasureId
    )
    BEGIN
        RAISERROR('CONVERSION_CROSS_TYPE_INVALID', 16, 1);
        RETURN;
    END

    -- COUNT-type units (Caja/Botella/Pack/Pieza...) have no universal ratio to one another - how
    -- many Botellas fit in a Caja is a fact about one specific Article's own packaging, not a
    -- catalog-wide constant, and is already captured per-article by ArticlePackagingLevels (see
    -- ArticleUnitConversion.GetCumulativeChain). A global "1 Caja = 24 Botella" row here would
    -- silently be wrong for any article whose real ratio differs. Only physical/dimensional
    -- UnitTypes (MASS, VOLUME, ...) get a real universal Factor.
    IF EXISTS (
        SELECT 1
        FROM UnitsOfMeasure f
        JOIN UnitTypes ut ON ut.UnitTypeId = f.UnitTypeId
        WHERE f.UnitOfMeasureId = @FromUnitOfMeasureId
          AND ut.Code = 'COUNT'
    )
    BEGIN
        RAISERROR('CONVERSION_COUNT_TYPE_NOT_ALLOWED', 16, 1);
        RETURN;
    END

    -- Cannot convert a unit to itself
    IF @FromUnitOfMeasureId = @ToUnitOfMeasureId
    BEGIN
        RAISERROR('CONVERSION_SAME_UNIT_INVALID', 16, 1);
        RETURN;
    END

    -- Pair must be unique
    IF EXISTS (SELECT 1 FROM UnitConversionRates WHERE FromUnitOfMeasureId = @FromUnitOfMeasureId AND ToUnitOfMeasureId = @ToUnitOfMeasureId)
    BEGIN
        RAISERROR('CONVERSION_PAIR_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO UnitConversionRates (UnitConversionRateToken, FromUnitOfMeasureId, ToUnitOfMeasureId, Factor, IsActive, CreatedUtc, CreatedBy)
    VALUES (@UnitConversionRateToken, @FromUnitOfMeasureId, @ToUnitOfMeasureId, @Factor, 1, SYSUTCDATETIME(), @CreatedBy);

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
