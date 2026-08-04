namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class InternalOrderReceiptLine
    {
        public int InternalOrderReceiptLineId { get; set; }
        public Guid InternalOrderReceiptLineToken { get; set; }
        public int InternalOrderReceiptId { get; set; }

        public int InternalOrderShipmentLineId { get; set; }
        public Guid InternalOrderShipmentLineToken { get; set; }
        public decimal QuantityShipped { get; set; }

        public int InternalOrderLineId { get; set; }
        public Guid InternalOrderLineToken { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal QuantityAccepted { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }

        public int? TaxCategoryId { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
