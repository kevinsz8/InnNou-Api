namespace InnNou.Application.Responses
{
    public class BulkImportFamiliesCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportFamilyRowError> Errors { get; set; } = new();
    }

    public class BulkImportFamilyRowError
    {
        public int RowNumber { get; set; }
        public string? FamilyCode { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
