SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   DEPARTMENT - GET PAGED BY ORGANIZATION ID
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Department_GetPagedByOrganizationId
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
        d.DepartmentId, d.DepartmentToken, d.OrganizationId, org.OrganizationToken, org.Name AS OrganizationName,
        d.Name, d.NormalizedName, d.Code, d.IsActive,
        d.CreatedUtc, d.CreatedBy, d.LastUpdatedUtc, d.LastUpdatedBy,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Departments d
    JOIN dbo.Organizations org ON org.OrganizationId = d.OrganizationId
    WHERE
        d.OrganizationId = @OrganizationId
        AND (@IncludeInactive = 1 OR d.IsActive = 1)
        AND
        (
            @SearchText IS NULL
            OR LOWER(d.Name)             LIKE '%' + LOWER(@SearchText) + '%'
            OR LOWER(ISNULL(d.Code, '')) LIKE '%' + LOWER(@SearchText) + '%'
        )
    ORDER BY d.Name
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
