/* =============================================================
   UNIT OF MEASURE - GET BY CODE
   Returns a single active unit of measure by its Code (globally
   unique via UQ_UnitsOfMeasure_Code), with the denormalized
   UnitTypeCode joined in — same shape sp_UnitOfMeasure_GetByToken
   returns, since callers (e.g. CreateArticleCommandHandler's
   COUNT/WEIGHT/VOLUME checks) rely on that column. Used by Article
   bulk import to resolve Excel "PurchaseUnitCode"/"ContentUnitCode"/
   "BaseUnitCode" columns to a UnitOfMeasureId.
   ============================================================= */
CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_GetByCode
    @Code VARCHAR(50)
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
        uom.IsActive
    FROM UnitsOfMeasure uom
    JOIN UnitTypes ut ON ut.UnitTypeId = uom.UnitTypeId
    WHERE uom.Code = @Code
      AND uom.IsActive = 1;
END;
GO
