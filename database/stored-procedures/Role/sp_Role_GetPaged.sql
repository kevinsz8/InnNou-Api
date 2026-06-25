/* =============================================================
   ROLE - GET PAGED
   Returns paginated roles visible to the caller. Only roles
   with RoleLevel <= @MaxLevel are returned, so users cannot
   see or assign roles above their own level.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Role_GetPaged
(
    @MaxLevel   INT,
    @PageNumber INT,
    @PageSize   INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.RoleId,
        r.RoleToken,
        r.Name,
        r.NormalizedName,
        r.Description,
        r.RoleLevel,
        r.CanImpersonate,
        r.IsActive,
        COUNT(*) OVER() AS TotalCount
    FROM dbo.Roles r
    WHERE
        r.IsActive   = 1
        AND r.RoleLevel <= @MaxLevel
    ORDER BY r.RoleLevel DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO
