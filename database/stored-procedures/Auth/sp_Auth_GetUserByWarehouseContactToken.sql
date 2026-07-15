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
   its UserToken. No IsActive/IsDeleted filter on purpose —
   eligibility is decided entirely by AuthService.ImpersonateAsync
   downstream (organization-hierarchy + RoleLevel), same as the
   Supplier equivalent.
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
    WHERE wc.WarehouseContactToken = @WarehouseContactToken;
END;
GO
