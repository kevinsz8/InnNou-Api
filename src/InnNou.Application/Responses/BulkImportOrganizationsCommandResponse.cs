namespace InnNou.Application.Responses
{
    public class BulkImportOrganizationsCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportOrganizationRowError> Errors { get; set; } = new();
    }

    public class BulkImportOrganizationRowError
    {
        public int RowNumber { get; set; }
        public string? Name { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
