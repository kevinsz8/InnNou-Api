namespace InnNou.Application.Responses
{
    public class DeleteOrderCommandResponse
    {
        public Guid OrderToken { get; set; }
        public bool Success { get; set; }
    }
}
