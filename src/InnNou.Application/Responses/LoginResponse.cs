namespace InnNou.Application.Responses
{
    public class LoginResponse
    {
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string UserId { get; set; } = default!;
    }
}
