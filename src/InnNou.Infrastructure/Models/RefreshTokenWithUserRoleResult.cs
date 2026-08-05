namespace InnNou.Infrastructure.Models;

// Flat projection returned by sp_Auth_GetRefreshTokenData.
// SP must alias: u.UserToken, u.Email, u.OrganizationId, u.SupplierId, r.RoleLevel AS RoleLevel.
internal sealed class RefreshTokenWithUserRoleResult
{
    public int RefreshTokenId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public Guid? ReplacedByToken { get; set; }
    public Guid UserToken { get; set; }
    public string Email { get; set; } = default!;
    public int? OrganizationId { get; set; }
    public int? SupplierId { get; set; }
    public int? WarehouseId { get; set; }
    public int RoleLevel { get; set; }

    // Populated by sp_Auth_GetRefreshTokenData (joins Organizations -> OrganizationTypes off the
    // user's own OrganizationId); null for a Supplier-scoped login with no OrganizationId.
    public string? OrganizationTypeCode { get; set; }

    // Set only when this refresh token was minted while impersonating (see AuthService.ImpersonateAsync) —
    // the remaining Impersonated* fields are the joined target user's data, so RefreshTokenAsync can
    // re-mint the JWT for the same impersonated target instead of reverting to this row's own actor identity.
    public int? ImpersonatedUserId { get; set; }
    public Guid? ImpersonatedUserToken { get; set; }
    public string? ImpersonatedEmail { get; set; }
    public int? ImpersonatedOrganizationId { get; set; }
    public int? ImpersonatedSupplierId { get; set; }
    public int? ImpersonatedWarehouseId { get; set; }
    public int? ImpersonatedRoleLevel { get; set; }
    public string? ImpersonatedOrganizationTypeCode { get; set; }
}
