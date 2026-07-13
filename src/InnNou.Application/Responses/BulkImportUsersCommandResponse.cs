namespace InnNou.Application.Responses
{
    public class BulkImportUsersCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportUserRowError> Errors { get; set; } = new();
    }

    public class BulkImportUserRowError
    {
        public int RowNumber { get; set; }
        public string? Email { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
