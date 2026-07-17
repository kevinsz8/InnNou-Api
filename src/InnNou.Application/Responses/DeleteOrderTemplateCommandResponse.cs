namespace InnNou.Application.Responses
{
    public class DeleteOrderTemplateCommandResponse
    {
        public Guid OrderTemplateToken { get; set; }
        public bool Success { get; set; }
    }
}
