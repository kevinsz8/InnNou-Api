namespace InnNou.Domain.Dtos
{
    public class BulkAssignArticleClassificationResultDto
    {
        public int TotalCount { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkAssignArticleClassificationItemErrorDto> Errors { get; set; } = new();
    }

    public class BulkAssignArticleClassificationItemErrorDto
    {
        // Internal id, not a token — same convention as every other service-layer DTO in this
        // codebase (tokens are a Request/Response-boundary concern). The handler, which already
        // holds the token->id map it built while resolving the request, translates this back to
        // ArticleToken for the public response.
        public int ArticleId { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
