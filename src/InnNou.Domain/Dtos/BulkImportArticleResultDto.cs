namespace InnNou.Domain.Dtos
{
    public class BulkImportArticleResultDto
    {
        public int TotalRows { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportArticleRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportArticleRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? Identifier { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
