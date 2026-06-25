/* =============================================================
   AUTH - GET USER BY TOKEN
   Returns full user + role data by UserToken (JWT sub claim).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserByToken
(
    @UserToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.UserToken,
        u.Email,
        u.PasswordHash,
        u.HotelId,
        u.SupplierId,
        u.IsActive,
        u.IsDeleted,
        u.LockedUntilUtc,
        r.RoleLevel,
        r.CanImpersonate
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.UserToken = @UserToken;
END;
GO
