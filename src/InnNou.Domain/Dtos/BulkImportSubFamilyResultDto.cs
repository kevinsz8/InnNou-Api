namespace InnNou.Domain.Dtos
{
    public class BulkImportSubFamilyResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportSubFamilyRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportSubFamilyRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? SubFamilyCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
