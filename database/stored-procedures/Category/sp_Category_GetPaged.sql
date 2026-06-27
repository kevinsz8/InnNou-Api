CREATE OR ALTER PROCEDURE sp_Category_GetPaged
(
    @PageNumber INT,
    @PageSize   INT
)
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
        LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM Categories
    WHERE IsActive = 1
    ORDER BY Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
