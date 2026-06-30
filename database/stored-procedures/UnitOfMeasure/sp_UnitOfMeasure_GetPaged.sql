CREATE OR ALTER PROCEDURE sp_UnitOfMeasure_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @UnitTypeId      INT = NULL,
    @IncludeInactive BIT = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UnitOfMeasureId,
        u.UnitOfMeasureToken,
        u.UnitTypeId,
        u.Code,
        u.Symbol,
        u.Decimals,
        u.IsSystem,
        u.IsActive,
        u.CreatedUtc,
        u.CreatedBy,
        u.LastUpdatedUtc,
        u.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM UnitsOfMeasure u
    WHERE (@IncludeInactive = 1 OR u.IsActive = 1)
      AND (@UnitTypeId IS NULL OR u.UnitTypeId = @UnitTypeId)
    ORDER BY u.Code
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
