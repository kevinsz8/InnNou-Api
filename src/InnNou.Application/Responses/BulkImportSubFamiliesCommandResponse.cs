namespace InnNou.Application.Responses
{
    public class BulkImportSubFamiliesCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportSubFamilyRowError> Errors { get; set; } = new();
    }

    public class BulkImportSubFamilyRowError
    {
        public int RowNumber { get; set; }
        public string? SubFamilyCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
