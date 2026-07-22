namespace InnNou.Domain.Dtos
{
    public class CopyOrderResultDto
    {
        public Guid NewOrderToken { get; set; }
        public int TotalLines { get; set; }
        public int CopiedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<CopyOrderSkippedLineDto> SkippedLines { get; set; } = new();
    }

    public class CopyOrderSkippedLineDto
    {
        public Guid? ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
