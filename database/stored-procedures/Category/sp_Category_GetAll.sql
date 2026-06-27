CREATE OR ALTER PROCEDURE sp_Category_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CategoryId,
        CategoryToken,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM Categories
    WHERE IsActive = 1
    ORDER BY Code;
END;
