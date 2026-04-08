using System.ComponentModel.DataAnnotations;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class RefreshToken
    {
        [Key]
        public long Id { get; set; }
        public int UserId { get; set; }

        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
