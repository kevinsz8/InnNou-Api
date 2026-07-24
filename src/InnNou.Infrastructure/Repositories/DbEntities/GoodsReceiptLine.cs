namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class GoodsReceiptLine
    {
        public int GoodsReceiptLineId { get; set; }
        public Guid GoodsReceiptLineToken { get; set; }
        public int GoodsReceiptId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }

        // The line's originally ordered quantity (PurchaseOrderLine.Quantity, not the
        // rectification-effective value) — purely a display convenience denormalization.
        public decimal OrderedQuantity { get; set; }

        public int ArticleId { get; set; }
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
