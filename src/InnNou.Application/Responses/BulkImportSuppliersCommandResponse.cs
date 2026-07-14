namespace InnNou.Application.Responses
{
    public class BulkImportSuppliersCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportSupplierRowError> Errors { get; set; } = new();
    }

    public class BulkImportSupplierRowError
    {
        public int RowNumber { get; set; }
        public string? Name { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
