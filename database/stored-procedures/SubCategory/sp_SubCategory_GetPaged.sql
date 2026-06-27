CREATE OR ALTER PROCEDURE sp_SubCategory_GetPaged
(
    @PageNumber INT,
    @PageSize   INT,
    @CategoryId INT = NULL
)
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
        LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM SubCategories
    WHERE IsActive = 1
      AND (@CategoryId IS NULL OR CategoryId = @CategoryId)
    ORDER BY CategoryId, Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
