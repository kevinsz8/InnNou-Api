SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   AUTH - GET TOP USER BY ORGANIZATION TOKEN
   Resolves the impersonation target for "impersonate this
   organization directly" (no specific user picked by the caller):
   the highest-RoleLevel active, non-deleted, genuine staff member
   of that organization (excludes Supplier/WarehouseContact shadow
   logins via SupplierId/WarehouseContactId IS NULL — those are a
   different entity's identity, not this org's own staff). Ties
   broken by lowest UserId (earliest-created) for determinism.

   Unlike sp_Auth_GetUserBySupplierToken/GetUserByWarehouseContactToken
   (which resolve a FIXED 1:1 shadow user and deliberately skip the
   IsActive/IsDeleted filter), this procedure chooses among MANY
   candidate users, so IsActive/IsDeleted ARE filtered here — an
   inactive top-ranked user should be skipped in favor of the next
   eligible one, not returned as an unusable target.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetTopUserByOrganizationToken
(
    @OrganizationToken UNIQUEIDENTIFIER
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        u.UserId, u.UserToken, u.Email, u.PasswordHash, u.OrganizationId, u.SupplierId,
        u.IsActive, u.IsDeleted, u.LockedUntilUtc,
        r.RoleLevel, r.CanImpersonate,
        o.Name AS OrganizationName
    FROM dbo.Organizations o
    INNER JOIN dbo.Users u ON u.OrganizationId = o.OrganizationId
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE o.OrganizationToken = @OrganizationToken
      AND u.IsActive = 1 AND u.IsDeleted = 0
      AND u.SupplierId IS NULL AND u.WarehouseContactId IS NULL
    ORDER BY r.RoleLevel DESC, u.UserId ASC;
END;
GO
