CREATE OR ALTER PROCEDURE sp_Zone_GetPaged
(
    @PageNumber      INT,
    @PageSize        INT,
    @CountryCode     VARCHAR(2)   = NULL,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        z.ZoneId, z.ZoneToken, z.CountryId, c.Code AS CountryCode, c.Name AS CountryName,
        z.Code, z.Name, z.IsActive, z.CreatedUtc, z.CreatedBy, z.LastUpdatedUtc, z.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM Zones z
    JOIN Countries c ON c.CountryId = z.CountryId
    WHERE (@IncludeInactive = 1 OR z.IsActive = 1)
      AND (@CountryCode IS NULL OR c.Code = @CountryCode)
      AND (@SearchText IS NULL OR LOWER(z.Code) LIKE '%' + LOWER(@SearchText) + '%' OR LOWER(z.Name) LIKE '%' + LOWER(@SearchText) + '%')
    ORDER BY c.Name, z.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
