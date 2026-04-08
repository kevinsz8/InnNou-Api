namespace InnNou.Application.Responses
{
    public class ImpersonateResponse
    {
        public string Token { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;

        public string UserToken { get; set; } = default!;
        public string Email { get; set; } = default!;

        public bool IsImpersonating { get; set; }
    }
}
