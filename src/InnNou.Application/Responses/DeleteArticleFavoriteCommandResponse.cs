namespace InnNou.Application.Responses
{
    public class DeleteArticleFavoriteCommandResponse
    {
        public Guid ArticleToken { get; set; }
        public bool Success { get; set; }
    }
}
