CREATE OR ALTER PROCEDURE sp_Family_SetActive
    @FamilyToken   uniqueidentifier,
    @IsActive      bit,
    @LastUpdatedBy nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Families WHERE FamilyToken = @FamilyToken)
    BEGIN
        RAISERROR('FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Families WHERE FamilyToken = @FamilyToken AND IsSystem = 1)
    BEGIN
        RAISERROR('FAMILY_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    UPDATE Families
    SET    IsActive       = @IsActive,
           LastUpdatedUtc  = SYSUTCDATETIME(),
           LastUpdatedBy   = @LastUpdatedBy
    WHERE  FamilyToken = @FamilyToken;

    SELECT
        FamilyId,
        FamilyToken,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM Families
    WHERE FamilyToken = @FamilyToken;
END;
