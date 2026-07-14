namespace InnNou.Domain.Dtos
{
    public class BulkImportArticlePriceResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportArticlePriceRowErrorDto> Errors { get; set; } = new();
    }

    public class BulkImportArticlePriceRowErrorDto
    {
        public int RowNumber { get; set; }
        public string? SupplierSku { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
