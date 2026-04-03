namespace InnNou.Domain.Models
{
    public class Login
    {
        public int UserId { get; set; }
        public Guid UserToken { get; set; }
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
