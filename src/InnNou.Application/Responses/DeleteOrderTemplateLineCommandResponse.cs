namespace InnNou.Application.Responses
{
    public class DeleteOrderTemplateLineCommandResponse
    {
        public Guid OrderTemplateLineToken { get; set; }
        public bool Success { get; set; }
    }
}
