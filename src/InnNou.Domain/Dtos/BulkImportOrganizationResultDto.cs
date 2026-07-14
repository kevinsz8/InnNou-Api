namespace InnNou.Domain.Dtos
{
    public class BulkImportOrganizationResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportOrganizationRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportOrganizationRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? Name { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
