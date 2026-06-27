CREATE OR ALTER PROCEDURE sp_SubFamily_GetPaged
(
    @PageNumber INT,
    @PageSize   INT,
    @FamilyId   INT = NULL
)
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
        LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM SubFamilies
    WHERE IsActive = 1
      AND (@FamilyId IS NULL OR FamilyId = @FamilyId)
    ORDER BY FamilyId, Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
