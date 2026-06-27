CREATE OR ALTER PROCEDURE sp_Category_GetByToken
    @CategoryToken uniqueidentifier
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
    WHERE CategoryToken = @CategoryToken;
END;
