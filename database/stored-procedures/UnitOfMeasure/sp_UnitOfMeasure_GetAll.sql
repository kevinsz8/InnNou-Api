CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_GetAll
    @UnitTypeId int = NULL   -- optional filter; NULL returns all active units
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UnitOfMeasureId,
        u.UnitOfMeasureToken,
        u.UnitTypeId,
        u.Code,
        u.Symbol,
        u.Decimals,
        u.IsSystem,
        u.IsActive,
        u.CreatedUtc,
        u.CreatedBy,
        u.LastUpdatedUtc,
        u.LastUpdatedBy
    FROM UnitsOfMeasure u
    WHERE u.IsActive = 1
      AND (@UnitTypeId IS NULL OR u.UnitTypeId = @UnitTypeId)
    ORDER BY u.Code;
END;
