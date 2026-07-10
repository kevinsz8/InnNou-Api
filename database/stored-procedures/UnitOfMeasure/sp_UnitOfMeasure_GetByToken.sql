CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_GetByToken
    @UnitOfMeasureToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        uom.UnitOfMeasureId,
        uom.UnitOfMeasureToken,
        uom.UnitTypeId,
        ut.Code AS UnitTypeCode,
        uom.Code,
        uom.Symbol,
        uom.Decimals,
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
