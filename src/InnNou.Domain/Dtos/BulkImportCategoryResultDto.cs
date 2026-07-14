namespace InnNou.Domain.Dtos
{
    public class BulkImportCategoryResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportCategoryRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportCategoryRowErrorDto
    {
        public int RowNumber { get; set; }
        // Named CategoryCode (not "Code", the row's own natural-key field) to avoid colliding
        // with the error-code field every other bulk-import row-error DTO in this codebase calls
        // "Code" — keep that name reserved for the error code, not the entity identifier.
        public string? CategoryCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
