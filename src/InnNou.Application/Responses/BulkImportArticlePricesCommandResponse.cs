namespace InnNou.Application.Responses
{
    public class BulkImportArticlePricesCommandResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<BulkImportArticlePriceRowError> Errors { get; set; } = new();
    }

    public class BulkImportArticlePriceRowError
    {
        public int RowNumber { get; set; }
        public string? SupplierSku { get; set; }
        public string Code { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
