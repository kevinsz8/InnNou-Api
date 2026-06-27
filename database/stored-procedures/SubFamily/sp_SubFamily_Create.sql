CREATE OR ALTER PROCEDURE sp_SubFamily_Create
    @SubFamilyToken uniqueidentifier,
    @FamilyId       int,
    @Code           varchar(100),
    @CreatedBy      nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Families WHERE FamilyId = @FamilyId AND IsActive = 1)
    BEGIN
        RAISERROR('FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    -- Code must be unique within the same family
    IF EXISTS (SELECT 1 FROM SubFamilies WHERE FamilyId = @FamilyId AND Code = @Code)
    BEGIN
        RAISERROR('SUB_FAMILY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO SubFamilies (SubFamilyToken, FamilyId, Code, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@SubFamilyToken, @FamilyId, @Code, 0, 1, SYSUTCDATETIME(), @CreatedBy);

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
