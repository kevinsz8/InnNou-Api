/* =============================================================
   USER - GET BY TOKEN
   Returns full user row joined with role level and impersonation
   flag, looked up by UserToken (JWT sub claim).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_User_GetByToken
(
    @UserToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.*,
        r.RoleLevel,
        r.CanImpersonate
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.UserToken = @UserToken;
END;
GO
