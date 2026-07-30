namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // Projection returned by sp_GoodsReceiptLine_GetEligibleForReturn — a rejected
    // GoodsReceiptLine not yet claimed by any SupplierReturnLine, feeding the "new return" line
    // picker. Distinct shape from GoodsReceiptLine/SupplierReturnLine (carries the delivery
    // note/receipt date context a picker needs, none of the accepted/courtesy quantities it
    // doesn't).
    public class EligibleReturnLine
    {
        public int GoodsReceiptLineId { get; set; }
        public Guid GoodsReceiptLineToken { get; set; }
        public int GoodsReceiptId { get; set; }
        public string? DeliveryNoteNumber { get; set; }
        public DateTime ReceivedUtc { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
    }
}
