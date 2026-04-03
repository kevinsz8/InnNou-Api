namespace InnNou.Application.Responses
{
    public class CreateUserCommandResponse
    {
        public Guid UserToken { get; set; }
        public string Email { get; set; } = default!;
    }
}
