namespace InnNou.Application.Responses
{
    public class BulkImportSubCategoriesCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportSubCategoryRowError> Errors { get; set; } = new();
    }

    public class BulkImportSubCategoryRowError
    {
        public int RowNumber { get; set; }
        public string? SubCategoryCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
