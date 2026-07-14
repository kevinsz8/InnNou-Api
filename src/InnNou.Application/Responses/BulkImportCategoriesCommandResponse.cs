namespace InnNou.Application.Responses
{
    public class BulkImportCategoriesCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportCategoryRowError> Errors { get; set; } = new();
    }

    public class BulkImportCategoryRowError
    {
        public int RowNumber { get; set; }
        public string? CategoryCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
