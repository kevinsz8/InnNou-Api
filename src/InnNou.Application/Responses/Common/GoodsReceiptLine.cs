namespace InnNou.Application.Responses.Common
{
    public class GoodsReceiptLine
    {
        public Guid GoodsReceiptLineToken { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }
        public decimal OrderedQuantity { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityAccepted { get; set; }
        public decimal QuantityCourtesy { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? LotNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
