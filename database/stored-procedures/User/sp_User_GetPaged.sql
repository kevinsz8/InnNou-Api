/* =============================================================
   USER - GET PAGED
   Returns a paginated list of users. Super admins see all.
   Hotel users see only users in their hotel subtree. Supplier
   users see only users belonging to their supplier.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_GetPaged
(
    @ContextRoleLevel INT,
    @RootHotelId      INT          = NULL,
    @SupplierId       INT          = NULL,
    @SearchField      VARCHAR(50)  = NULL,
    @SearchText       VARCHAR(200) = NULL,
    @PageNumber       INT,
    @PageSize         INT
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH HotelHierarchy AS
    (
        SELECT HotelId
        FROM dbo.Hotels
        WHERE HotelId  = @RootHotelId
          AND IsDeleted = 0
          AND IsActive  = 1

        UNION ALL

        SELECT h.HotelId
        FROM dbo.Hotels h
        INNER JOIN HotelHierarchy hh ON h.ParentHotelId = hh.HotelId
        WHERE h.IsDeleted = 0
          AND h.IsActive  = 1
    )
    SELECT
        u.UserId,
        u.UserToken,
        u.FirstName,
        u.LastName,
        u.Email,
        u.UserName,
        u.RoleId,
        u.HotelId,
        u.SupplierId,
        u.IsActive,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Users u
    WHERE
        u.IsDeleted = 0
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

            -- HOTEL USER: see only users within the hotel subtree
            (
                @RootHotelId IS NOT NULL
                AND EXISTS
                (
                    SELECT 1 FROM HotelHierarchy hh WHERE hh.HotelId = u.HotelId
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
