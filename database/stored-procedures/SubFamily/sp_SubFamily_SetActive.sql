CREATE OR ALTER PROCEDURE sp_SubFamily_SetActive
    @SubFamilyToken uniqueidentifier,
    @IsActive       bit,
    @LastUpdatedBy  nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM SubFamilies WHERE SubFamilyToken = @SubFamilyToken)
    BEGIN
        RAISERROR('SUB_FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM SubFamilies WHERE SubFamilyToken = @SubFamilyToken AND IsSystem = 1)
    BEGIN
        RAISERROR('SUB_FAMILY_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    UPDATE SubFamilies
    SET    IsActive       = @IsActive,
           LastUpdatedUtc  = SYSUTCDATETIME(),
           LastUpdatedBy   = @LastUpdatedBy
    WHERE  SubFamilyToken = @SubFamilyToken;

    SELECT
        SubFamilyId,
        SubFamilyToken,
        FamilyId,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM SubFamilies
    WHERE SubFamilyToken = @SubFamilyToken;
END;
