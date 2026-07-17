namespace InnNou.Application.Responses.Common
{
    public class ApplyOrderTemplateLineResult
    {
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierType { get; set; }
        public decimal Quantity { get; set; }

        // "SUCCEEDED" | "NEEDS_MANUAL_PRICE" | "FAILED"
        public string Outcome { get; set; } = default!;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
