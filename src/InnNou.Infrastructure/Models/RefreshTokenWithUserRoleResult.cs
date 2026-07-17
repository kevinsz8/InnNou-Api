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
    public int RoleLevel { get; set; }

    // Populated by sp_Auth_GetRefreshTokenData (joins Organizations -> OrganizationTypes off the
    // user's own OrganizationId); null for a Supplier-scoped login with no OrganizationId.
    public string? OrganizationTypeCode { get; set; }
}
