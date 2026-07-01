namespace InnNou.Application.Responses
{
    public class DeleteArticleCommandResponse
    {
        public Guid ArticleToken { get; set; }
        public bool Success { get; set; }
    }
}
