namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class RefreshToken
    {
        public int RefreshTokenId { get; set; }
        public Guid RefreshTokenToken { get; set; }
        public int UserId { get; set; }
        public string TokenHash { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedUtc { get; set; }
        public string? CreatedByIp { get; set; }
        public string? RevokedByIp { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceName { get; set; }
        public Guid? ReplacedByToken { get; set; }
        public Guid? SessionToken { get; set; }
    }
}
