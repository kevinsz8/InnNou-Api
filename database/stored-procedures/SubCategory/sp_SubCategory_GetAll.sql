CREATE OR ALTER PROCEDURE sp_SubCategory_GetAll
    @CategoryId int = NULL   -- optional filter; NULL returns all active subcategories
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SubCategoryId,
        SubCategoryToken,
        CategoryId,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy
    FROM SubCategories
    WHERE IsActive = 1
      AND (@CategoryId IS NULL OR CategoryId = @CategoryId)
    ORDER BY CategoryId, Code;
END;
