namespace InnNou.Domain.Dtos
{
    public class BulkImportFamilyResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportFamilyRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportFamilyRowErrorDto
    {
        public int RowNumber { get; set; }
        // Named FamilyCode (not "Code", the row's own natural-key field) to avoid colliding with
        // the error-code field every bulk-import row-error DTO in this codebase calls "Code".
        public string? FamilyCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
