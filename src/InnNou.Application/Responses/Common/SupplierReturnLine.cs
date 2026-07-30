namespace InnNou.Application.Responses.Common
{
    public class SupplierReturnLine
    {
        public Guid SupplierReturnLineToken { get; set; }
        public Guid GoodsReceiptLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
