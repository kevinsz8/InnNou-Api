namespace InnNou.Application.Responses
{
    public class DeleteOrderLineCommandResponse
    {
        public Guid OrderLineToken { get; set; }
        public bool Success { get; set; }
    }
}
