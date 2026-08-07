-- @UnitTypeId is intentionally unused: UQ_UnitsOfMeasure_Code enforces Code uniqueness
-- GLOBALLY (not per-UnitType), so the existence check must match that scope exactly —
-- otherwise it can return "available" (false) for a Code already taken by a different
-- UnitType, which then fails at the real INSERT/UPDATE instead. Parameter kept in the
-- signature to avoid rippling the change into every caller (UnitOfMeasureService,
-- CreateUnitOfMeasureCommandHandler) for what is otherwise a one-line predicate fix.
CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_ExistsByCode
    @Code      NVARCHAR(50),
    @UnitTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM UnitsOfMeasure WHERE Code = @Code
    ) THEN 1 ELSE 0 END AS BIT);
END
