/* =============================================================
   HOTEL - GET PAGED
   Returns a paginated list of hotels, per caller scope (resolved
   in HotelService.ResolveScope):
     - @RootHotelId and @ExactHotelId both NULL: unrestricted (all
       hotels) — SuperAdmin, or Admin with no hotel assigned.
     - @RootHotelId set: that hotel's subtree (itself + all
       descendants) — Admin/Manager with a hotel assigned.
     - @ExactHotelId set: exactly that one hotel, no descendants —
       anyone below Manager.
   Callers pass at most one of @RootHotelId/@ExactHotelId.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Hotel_GetPaged
(
    @RootHotelId     INT          = NULL,
    @ExactHotelId    INT          = NULL,
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
            (@RootHotelId IS NULL AND @ExactHotelId IS NULL)
            OR (@RootHotelId IS NOT NULL AND EXISTS (SELECT 1 FROM HotelHierarchy hh WHERE hh.HotelId = h.HotelId))
            OR (@ExactHotelId IS NOT NULL AND h.HotelId = @ExactHotelId)
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
