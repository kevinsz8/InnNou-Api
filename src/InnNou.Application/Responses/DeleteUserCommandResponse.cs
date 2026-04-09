namespace InnNou.Application.Responses
{
    public class DeleteUserCommandResponse
    {
        public Guid UserToken { get; set; }
        public bool Success { get; set; }
    }
}
