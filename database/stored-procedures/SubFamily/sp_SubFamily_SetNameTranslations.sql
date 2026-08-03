CREATE OR ALTER PROCEDURE sp_SubFamily_SetNameTranslations
    @SubFamilyToken    uniqueidentifier,
    @NameTranslations  nvarchar(1000) = NULL,
    @LastUpdatedBy     nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM SubFamilies WHERE SubFamilyToken = @SubFamilyToken)
    BEGIN
        RAISERROR('SUB_FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF @NameTranslations IS NOT NULL AND ISJSON(@NameTranslations) = 0
    BEGIN
        RAISERROR('INVALID_REQUEST', 16, 1);
        RETURN;
    END

    -- Deliberately no IsSystem guard, unlike sp_SubFamily_Update/sp_SubFamily_SetActive —
    -- translating the display name isn't a structural-identity change, same reasoning as
    -- sp_Family_SetNameTranslations.
    UPDATE SubFamilies
    SET    NameTranslations = @NameTranslations,
           LastUpdatedUtc   = SYSUTCDATETIME(),
           LastUpdatedBy    = @LastUpdatedBy
    WHERE  SubFamilyToken = @SubFamilyToken;

    SELECT
        SubFamilyId,
        SubFamilyToken,
        FamilyId,
        Code,
        NameTranslations,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM SubFamilies
    WHERE SubFamilyToken = @SubFamilyToken;
END;
