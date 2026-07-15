SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   WAREHOUSE CONTACT - GET PAGED BY WAREHOUSE ID
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_WarehouseContact_GetPagedByWarehouseId
(
    @WarehouseId     INT,
    @PageNumber      INT,
    @PageSize        INT,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wc.WarehouseContactId,
        wc.WarehouseContactToken,
        wc.WarehouseId,
        wc.ContactName,
        wc.ContactType,
        wc.Department,
        wc.Phone,
        wc.Mobile,
        wc.Fax,
        wc.Email,
        wc.Notes,
        wc.IsPrimary,
        wc.HasAccessToSystem,
        wc.IsActive,
        wc.IsDeleted,
        wc.CreatedUtc,
        wc.CreatedBy,
        wc.LastUpdatedUtc,
        wc.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.WarehouseContacts wc
    WHERE
        wc.WarehouseId = @WarehouseId
        AND wc.IsDeleted = 0
        AND (@IncludeInactive = 1 OR wc.IsActive = 1)
        AND
        (
            @SearchText IS NULL
            OR LOWER(wc.ContactName)              LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(wc.ContactType,  ''))  LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(wc.Department,   ''))  LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(wc.Email,        ''))  LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY wc.IsPrimary DESC, wc.ContactName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
