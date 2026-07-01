SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   ORGANIZATION CONTACT - GET PAGED BY ORGANIZATION ID
   Returns a paginated list of contacts for a given organization.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_OrganizationContact_GetPagedByOrganizationId
(
    @OrganizationId  INT,
    @PageNumber      INT,
    @PageSize        INT,
    @SearchText      VARCHAR(200) = NULL,
    @IncludeInactive BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        oc.OrganizationContactId,
        oc.OrganizationContactToken,
        oc.OrganizationId,
        oc.ContactName,
        oc.ContactType,
        oc.Department,
        oc.Phone,
        oc.Mobile,
        oc.Fax,
        oc.Email,
        oc.Notes,
        oc.IsPrimary,
        oc.IsActive,
        oc.IsDeleted,
        oc.CreatedUtc,
        oc.CreatedBy,
        oc.LastUpdatedUtc,
        oc.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.OrganizationContacts oc
    WHERE
        oc.OrganizationId = @OrganizationId
        AND oc.IsDeleted = 0
        AND (@IncludeInactive = 1 OR oc.IsActive = 1)
        AND
        (
            @SearchText IS NULL
            OR LOWER(oc.ContactName)                  LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(oc.ContactType,  ''))     LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(oc.Department,   ''))     LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(oc.Email,        ''))     LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY oc.IsPrimary DESC, oc.ContactName
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
