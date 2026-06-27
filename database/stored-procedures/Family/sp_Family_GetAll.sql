CREATE OR ALTER PROCEDURE sp_Family_GetAll
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
    WHERE IsActive = 1
    ORDER BY Code;
END;
