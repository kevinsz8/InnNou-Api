CREATE OR ALTER PROCEDURE sp_Family_GetPaged
(
    @PageNumber INT,
    @PageSize   INT
)
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
        LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM Families
    WHERE IsActive = 1
    ORDER BY Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
