SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
/* =============================================================
   AUTH - GET USER BY WAREHOUSE CONTACT TOKEN
   Returns full user + role data for the shadow/login User linked
   to a WarehouseContact, looked up by WarehouseContacts.WarehouseContactToken.
   Used to resolve the impersonation target for "impersonate this
   warehouse contact" without requiring the caller to already know
   its UserToken. No IsActive filter on the shadow User itself, on
   purpose — eligibility is decided entirely by
   AuthService.ImpersonateAsync downstream (organization-hierarchy +
   RoleLevel), same as the Supplier equivalent. wc.IsDeleted IS
   filtered, though: a deleted WarehouseContact no longer exists as
   a manageable entity, so its identity must not remain impersonable
   even if its shadow User row is still IsActive = 1
   (WarehouseContactService.DeleteAsync deactivates it, but this
   filter is the real guarantee — it doesn't depend on that).
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserByWarehouseContactToken
(
    @WarehouseContactToken UNIQUEIDENTIFIER
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
        r.CanImpersonate,
        wc.ContactName AS WarehouseContactName
    FROM dbo.WarehouseContacts wc
    INNER JOIN dbo.Users u ON u.WarehouseContactId = wc.WarehouseContactId
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE wc.WarehouseContactToken = @WarehouseContactToken
      AND wc.IsDeleted = 0;
END;
GO
