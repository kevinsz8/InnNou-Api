namespace InnNou.Domain.Dtos
{
    public class BulkImportSupplierResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportSupplierRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportSupplierRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? Name { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
