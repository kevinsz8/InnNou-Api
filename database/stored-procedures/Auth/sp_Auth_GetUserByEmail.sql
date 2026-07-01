SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   AUTH - GET USER BY EMAIL
   Returns full user + role data for login validation.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserByEmail
(
    @Email VARCHAR(320)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.UserToken,
        u.Email,
        u.PasswordHash,
        u.OrganizationId,
        u.SupplierId,
        u.IsActive,
        u.IsDeleted,
        u.LockedUntilUtc,
        r.RoleLevel,
        r.CanImpersonate
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.NormalizedEmail = @Email;
END;
GO
