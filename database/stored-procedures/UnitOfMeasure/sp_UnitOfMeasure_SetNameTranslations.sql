CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_SetNameTranslations
    @UnitOfMeasureToken uniqueidentifier,
    @NameTranslations   nvarchar(1000) = NULL,
    @LastUpdatedBy      nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitsOfMeasure WHERE UnitOfMeasureToken = @UnitOfMeasureToken)
    BEGIN
        RAISERROR('UNIT_OF_MEASURE_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF @NameTranslations IS NOT NULL AND ISJSON(@NameTranslations) = 0
    BEGIN
        RAISERROR('INVALID_REQUEST', 16, 1);
        RETURN;
    END

    -- Deliberately no IsSystem guard, unlike sp_UnitOfMeasure_Update/sp_UnitOfMeasure_SetActive —
    -- translating the display name isn't a structural-identity change, same reasoning as
    -- sp_Family_SetNameTranslations.
    UPDATE UnitsOfMeasure
    SET    NameTranslations = @NameTranslations,
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  UnitOfMeasureToken = @UnitOfMeasureToken;

    SELECT
        uom.UnitOfMeasureId,
        uom.UnitOfMeasureToken,
        uom.UnitTypeId,
        ut.Code AS UnitTypeCode,
        uom.Code,
        uom.Symbol,
        uom.Decimals,
        uom.NameTranslations,
        uom.IsSystem,
        uom.IsActive,
        uom.CreatedUtc,
        uom.CreatedBy,
        uom.LastUpdatedUtc,
        uom.LastUpdatedBy
    FROM UnitsOfMeasure uom
    JOIN UnitTypes ut ON ut.UnitTypeId = uom.UnitTypeId
    WHERE uom.UnitOfMeasureToken = @UnitOfMeasureToken;
END;
