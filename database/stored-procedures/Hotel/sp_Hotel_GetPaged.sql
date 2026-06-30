/* =============================================================
   HOTEL - GET PAGED
   Returns a paginated list of hotels. Super admins receive all
   hotels (@RootHotelId = NULL). Non-admins receive only hotels
   within their hierarchy subtree.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_GetPaged
(
    @RootHotelId     INT          = NULL,
    @SearchText      VARCHAR(200) = NULL,
    @PageNumber      INT,
    @PageSize        INT,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH HotelHierarchy AS
    (
        SELECT h.HotelId
        FROM dbo.Hotels h
        WHERE h.HotelId = @RootHotelId

        UNION ALL

        SELECT h.HotelId
        FROM dbo.Hotels h
        INNER JOIN HotelHierarchy hh ON h.ParentHotelId = hh.HotelId
    )
    SELECT
        h.HotelId,
        h.HotelToken,
        h.Name,
        h.NormalizedName,
        h.LegalName,
        h.Code,
        h.ParentHotelId,
        h.TimeZone,
        h.CurrencyCode,
        h.LanguageCode,
        h.IsActive,
        h.IsDeleted,
        h.CreatedUtc,
        h.CreatedBy,
        h.LastUpdatedUtc,
        h.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Hotels h
    WHERE
        h.IsDeleted = 0
        AND (@IncludeInactive = 1 OR h.IsActive = 1)
        AND
        (
            @RootHotelId IS NULL
            OR EXISTS
            (
                SELECT 1 FROM HotelHierarchy hh WHERE hh.HotelId = h.HotelId
            )
        )
        AND
        (
            @SearchText IS NULL
            OR LOWER(h.Name)                   LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(h.LegalName, ''))  LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(h.Code, ''))       LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY h.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
