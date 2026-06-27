CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_ExistsByCode
    @Code      NVARCHAR(50),
    @UnitTypeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM UnitsOfMeasure WHERE Code = @Code AND UnitTypeId = @UnitTypeId
    ) THEN 1 ELSE 0 END AS BIT);
END
