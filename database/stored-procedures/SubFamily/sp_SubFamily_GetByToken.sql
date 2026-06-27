CREATE OR ALTER PROCEDURE sp_SubFamily_GetByToken
    @SubFamilyToken uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

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
