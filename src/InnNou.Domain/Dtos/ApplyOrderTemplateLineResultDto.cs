namespace InnNou.Domain.Dtos
{
    public static class ApplyOrderTemplateLineOutcomes
    {
        public const string Succeeded = "SUCCEEDED";
        public const string NeedsManualPrice = "NEEDS_MANUAL_PRICE";
        public const string Failed = "FAILED";
    }

    public class ApplyOrderTemplateLineResultDto
    {
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierType { get; set; }
        public decimal Quantity { get; set; }

        // One of ApplyOrderTemplateLineOutcomes.
        public string Outcome { get; set; } = default!;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
