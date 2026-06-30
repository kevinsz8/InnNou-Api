/* =============================================================
   HOTEL CONTACT - GET PAGED BY HOTEL ID
   Returns a paginated list of contacts for a given hotel.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_HotelContact_GetPagedByHotelId
(
    @HotelId         INT,
    @PageNumber      INT,
    @PageSize        INT,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        hc.HotelContactId,
        hc.HotelContactToken,
        hc.HotelId,
        hc.ContactName,
        hc.ContactType,
        hc.Department,
        hc.Phone,
        hc.Mobile,
        hc.Fax,
        hc.Email,
        hc.Notes,
        hc.IsPrimary,
        hc.IsActive,
        hc.IsDeleted,
        hc.CreatedUtc,
        hc.CreatedBy,
        hc.LastUpdatedUtc,
        hc.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.HotelContacts hc
    WHERE
        hc.HotelId    = @HotelId
        AND hc.IsDeleted = 0
        AND (@IncludeInactive = 1 OR hc.IsActive = 1)
        AND
        (
            @SearchText IS NULL
            OR LOWER(hc.ContactName)                  LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(hc.ContactType,  ''))     LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(hc.Department,   ''))     LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(hc.Email,        ''))     LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY hc.IsPrimary DESC, hc.ContactName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
