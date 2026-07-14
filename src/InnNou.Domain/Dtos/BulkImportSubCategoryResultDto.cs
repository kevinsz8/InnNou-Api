namespace InnNou.Domain.Dtos
{
    public class BulkImportSubCategoryResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportSubCategoryRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportSubCategoryRowErrorDto
    {
        public int RowNumber { get; set; }
        // Same naming rationale as BulkImportCategoryRowErrorDto.CategoryCode — reserves "Code"
        // for the error code field.
        public string? SubCategoryCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
