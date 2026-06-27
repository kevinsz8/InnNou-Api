CREATE OR ALTER PROCEDURE sp_SubFamily_GetAll
    @FamilyId int = NULL   -- optional filter; NULL returns all active subfamilies
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
    WHERE IsActive = 1
      AND (@FamilyId IS NULL OR FamilyId = @FamilyId)
    ORDER BY FamilyId, Code;
END;
