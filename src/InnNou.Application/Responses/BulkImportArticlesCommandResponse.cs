namespace InnNou.Application.Responses
{
    public class BulkImportArticlesCommandResponse
    {
        public int TotalRows { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportArticleRowError> Errors { get; set; } = new();
    }

    public class BulkImportArticleRowError
    {
        public int RowNumber { get; set; }
        public string? Identifier { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
