CREATE OR ALTER PROCEDURE sp_Family_GetByToken
    @FamilyToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

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
