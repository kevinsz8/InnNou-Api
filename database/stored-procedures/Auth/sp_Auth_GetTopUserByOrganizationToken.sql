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
   IsActive/IsDeleted filter on the shadow User itself), this
   procedure chooses among MANY candidate users, so IsActive/IsDeleted
   ARE filtered here on the User — an inactive top-ranked user should
   be skipped in favor of the next eligible one, not returned as an
   unusable target.

   The organization itself is also gated: a deleted/inactive
   Organization must never be impersonable, regardless of whether
   its real staff Users are still individually active — Organizations
   only ever reach IsActive = 0 paired with IsDeleted = 1 (via
   sp_Organization_SoftDelete), so filtering both here is equivalent
   to filtering IsDeleted alone today, but keeps the guarantee correct
   even if a future feature ever decouples the two flags.
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
      AND o.IsActive = 1 AND o.IsDeleted = 0
      AND u.IsActive = 1 AND u.IsDeleted = 0
      AND u.SupplierId IS NULL AND u.WarehouseContactId IS NULL
    ORDER BY r.RoleLevel DESC, u.UserId ASC;
END;
GO
