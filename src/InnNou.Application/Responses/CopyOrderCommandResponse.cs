namespace InnNou.Application.Responses
{
    public class CopyOrderCommandResponse
    {
        public Guid NewOrderToken { get; set; }
        public int TotalLines { get; set; }
        public int CopiedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<CopyOrderSkippedLineResponse> SkippedLines { get; set; } = new();
    }

    public class CopyOrderSkippedLineResponse
    {
        public Guid? ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
