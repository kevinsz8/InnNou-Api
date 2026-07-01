/* =============================================================
   AUTH - GET USER BY SUPPLIER TOKEN
   Returns full user + role data for the shadow/login User linked
   to a Supplier, looked up by Suppliers.SupplierToken. Used to
   resolve the impersonation target for "impersonate this supplier"
   without requiring the caller to already know its UserToken.
   No IsActive/IsDeleted filter on purpose — eligibility is decided
   entirely by AuthService.ImpersonateAsync downstream, so a
   Supplier with no real system access (HasAccessToSystem = 0) can
   still be impersonated by a superadmin.
   ============================================================= */
CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetUserBySupplierToken
(
    @SupplierToken UNIQUEIDENTIFIER
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
        r.CanImpersonate,
        s.Name AS SupplierName
    FROM dbo.Suppliers s
    INNER JOIN dbo.Users u ON u.SupplierId = s.SupplierId
    INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE s.SupplierToken = @SupplierToken;
END;
GO
