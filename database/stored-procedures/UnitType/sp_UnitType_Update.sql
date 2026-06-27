CREATE OR ALTER PROCEDURE sp_UnitType_Update
    @UnitTypeToken  uniqueidentifier,
    @Code           varchar(50),
    @LastUpdatedBy  nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitTypes WHERE UnitTypeToken = @UnitTypeToken)
    BEGIN
        RAISERROR('UNIT_TYPE_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM UnitTypes WHERE UnitTypeToken = @UnitTypeToken AND IsSystem = 1)
    BEGIN
        RAISERROR('UNIT_TYPE_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM UnitTypes WHERE Code = @Code AND UnitTypeToken <> @UnitTypeToken)
    BEGIN
        RAISERROR('UNIT_TYPE_CODE_EXISTS', 16, 1);
        RETURN;
    END

    UPDATE UnitTypes
    SET    Code          = @Code,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
    WHERE  UnitTypeToken = @UnitTypeToken;

    SELECT
        UnitTypeId,
        UnitTypeToken,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM UnitTypes
    WHERE UnitTypeToken = @UnitTypeToken;
END;
