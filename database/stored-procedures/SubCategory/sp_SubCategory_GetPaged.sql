CREATE OR ALTER PROCEDURE sp_SubCategory_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @CategoryId      INT          = NULL,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
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
    WHERE (@IncludeInactive = 1 OR IsActive = 1)
      AND (@CategoryId IS NULL OR CategoryId = @CategoryId)
      AND (@SearchText IS NULL OR LOWER(Code) LIKE '%' + LOWER(@SearchText) + '%')
    ORDER BY CategoryId, Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
