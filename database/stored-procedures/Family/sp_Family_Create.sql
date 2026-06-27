CREATE OR ALTER PROCEDURE sp_Family_Create
    @FamilyToken uniqueidentifier,
    @Code        varchar(100),
    @CreatedBy   nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Families WHERE Code = @Code)
    BEGIN
        RAISERROR('FAMILY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    INSERT INTO Families (FamilyToken, Code, IsSystem, IsActive, CreatedUtc, CreatedBy)
    VALUES (@FamilyToken, @Code, 0, 1, SYSUTCDATETIME(), @CreatedBy);

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
