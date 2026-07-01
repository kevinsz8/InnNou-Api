SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   USER - GET PAGED
   Returns a paginated list of users. Super admins see all.
   Organization users see only users in their organization subtree.
   Supplier users see only users belonging to their supplier.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_GetPaged
(
    @ContextRoleLevel    INT,
    @RootOrganizationId  INT          = NULL,
    @SupplierId          INT          = NULL,
    @SearchField         VARCHAR(50)  = NULL,
    @SearchText          VARCHAR(200) = NULL,
    @PageNumber          INT,
    @PageSize            INT,
    @IncludeInactive     BIT          = 0
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH OrganizationHierarchy AS
    (
        SELECT OrganizationId
        FROM dbo.Organizations
        WHERE OrganizationId = @RootOrganizationId
          AND IsDeleted = 0
          AND IsActive  = 1

        UNION ALL

        SELECT o.OrganizationId
        FROM dbo.Organizations o
        INNER JOIN OrganizationHierarchy oh ON o.ParentOrganizationId = oh.OrganizationId
        WHERE o.IsDeleted = 0
          AND o.IsActive  = 1
    )
    SELECT
        u.UserId,
        u.UserToken,
        u.FirstName,
        u.LastName,
        u.Email,
        u.UserName,
        u.RoleId,
        u.OrganizationId,
        u.SupplierId,
        u.IsActive,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Users u
    WHERE
        u.IsDeleted = 0
        AND (@IncludeInactive = 1 OR u.IsActive = 1)
        AND
        (
            -- SUPER ADMIN: see everything
            @ContextRoleLevel >= 100

            OR

            -- SUPPLIER USER: see only users of their supplier
            (
                @SupplierId IS NOT NULL
                AND u.SupplierId = @SupplierId
            )

            OR

            -- ORGANIZATION USER: see only users within the organization subtree
            (
                @RootOrganizationId IS NOT NULL
                AND EXISTS
                (
                    SELECT 1 FROM OrganizationHierarchy oh WHERE oh.OrganizationId = u.OrganizationId
                )
            )
        )
        AND
        (
            @SearchText IS NULL
            OR (@SearchField = 'email'     AND LOWER(u.Email)     LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'firstname' AND LOWER(u.FirstName) LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'lastname'  AND LOWER(u.LastName)  LIKE '%' + LOWER(@SearchText) + '%')
            OR (@SearchField = 'username'  AND LOWER(u.UserName)  LIKE '%' + LOWER(@SearchText) + '%')
        )
    ORDER BY u.UserId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
