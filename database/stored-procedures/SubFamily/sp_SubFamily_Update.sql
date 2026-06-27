CREATE OR ALTER PROCEDURE sp_SubFamily_Update
    @SubFamilyToken uniqueidentifier,
    @Code           varchar(100),
    @LastUpdatedBy  nvarchar(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FamilyId int;
    SELECT @FamilyId = FamilyId FROM SubFamilies WHERE SubFamilyToken = @SubFamilyToken;

    IF @FamilyId IS NULL
    BEGIN
        RAISERROR('SUB_FAMILY_NOT_FOUND', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM SubFamilies WHERE SubFamilyToken = @SubFamilyToken AND IsSystem = 1)
    BEGIN
        RAISERROR('SUB_FAMILY_SYSTEM_READONLY', 16, 1);
        RETURN;
    END

    -- Code must be unique within the same family
    IF EXISTS (SELECT 1 FROM SubFamilies WHERE FamilyId = @FamilyId AND Code = @Code AND SubFamilyToken <> @SubFamilyToken)
    BEGIN
        RAISERROR('SUB_FAMILY_CODE_EXISTS', 16, 1);
        RETURN;
    END

    UPDATE SubFamilies
    SET    Code          = @Code,
           LastUpdatedUtc = SYSUTCDATETIME(),
           LastUpdatedBy  = @LastUpdatedBy
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
