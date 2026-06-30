CREATE OR ALTER PROCEDURE sp_UnitConversionRate_GetPaged
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
        r.UnitConversionRateId,
        r.UnitConversionRateToken,
        r.FromUnitOfMeasureId,
        r.ToUnitOfMeasureId,
        f.Code   AS FromUOMCode,
        f.Symbol AS FromUOMSymbol,
        t.Code   AS ToUOMCode,
        t.Symbol AS ToUOMSymbol,
        r.Factor,
        r.IsActive,
        r.CreatedUtc,
        r.CreatedBy,
        r.LastUpdatedUtc,
        r.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM UnitConversionRates r
    JOIN UnitsOfMeasure f ON f.UnitOfMeasureId = r.FromUnitOfMeasureId
    JOIN UnitsOfMeasure t ON t.UnitOfMeasureId = r.ToUnitOfMeasureId
    WHERE (@IncludeInactive = 1 OR r.IsActive = 1)
      AND (@UnitTypeId IS NULL OR f.UnitTypeId = @UnitTypeId)
    ORDER BY r.FromUnitOfMeasureId, r.ToUnitOfMeasureId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
