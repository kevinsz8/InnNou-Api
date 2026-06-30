CREATE OR ALTER PROCEDURE sp_UnitType_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @IncludeInactive BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UnitTypeId,
        UnitTypeToken,
        Code,
        IsSystem,
        IsActive,
        CreatedUtc,
        CreatedBy,
        LastUpdatedUtc,
        LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM UnitTypes
    WHERE (@IncludeInactive = 1 OR IsActive = 1)
    ORDER BY Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
