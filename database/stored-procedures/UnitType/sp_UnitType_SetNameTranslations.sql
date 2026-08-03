CREATE OR ALTER PROCEDURE sp_UnitType_SetNameTranslations
    @UnitTypeToken     uniqueidentifier,
    @NameTranslations  nvarchar(1000) = NULL,
    @LastUpdatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM UnitTypes WHERE UnitTypeToken = @UnitTypeToken)
    BEGIN
        RAISERROR('UNIT_TYPE_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF @NameTranslations IS NOT NULL AND ISJSON(@NameTranslations) = 0
    BEGIN
        RAISERROR('INVALID_REQUEST', 16, 1);
        RETURN;
    END

    -- Deliberately no IsSystem guard, unlike sp_UnitType_Update/sp_UnitType_SetActive —
    -- translating the display name isn't a structural-identity change, same reasoning as
    -- sp_Family_SetNameTranslations.
    UPDATE UnitTypes
    SET    NameTranslations = @NameTranslations,
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  UnitTypeToken = @UnitTypeToken;

    SELECT
        UnitTypeId,
        UnitTypeToken,
        Code,
        NameTranslations,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM UnitTypes
    WHERE UnitTypeToken = @UnitTypeToken;
END;
