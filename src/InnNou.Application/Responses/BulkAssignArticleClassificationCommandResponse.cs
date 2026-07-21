namespace InnNou.Application.Responses
{
    public class BulkAssignArticleClassificationCommandResponse
    {
        public int TotalCount { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkAssignArticleClassificationItemError> Errors { get; set; } = [];
    }

    public class BulkAssignArticleClassificationItemError
    {
        public Guid ArticleToken { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
