CREATE OR ALTER PROCEDURE sp_SubCategory_GetByToken
    @SubCategoryToken uniqueidentifier
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
    WHERE SubCategoryToken = @SubCategoryToken;
END;
