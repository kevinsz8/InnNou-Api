namespace InnNou.Domain.Dtos
{
    public class BulkImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? Email { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
