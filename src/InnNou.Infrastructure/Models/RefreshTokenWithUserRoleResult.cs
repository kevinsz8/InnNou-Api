namespace InnNou.Infrastructure.Models;

// Flat projection returned by sp_Auth_GetRefreshTokenData.
// SP must alias: u.UserToken, u.Email, u.HotelId, r.Level AS RoleLevel (no name collisions).
internal sealed class RefreshTokenWithUserRoleResult
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public Guid UserToken { get; set; }
    public string Email { get; set; } = default!;
    public int? HotelId { get; set; }
    public int RoleLevel { get; set; }
}
