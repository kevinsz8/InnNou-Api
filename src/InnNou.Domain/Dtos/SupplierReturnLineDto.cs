namespace InnNou.Domain.Dtos
{
    public class SupplierReturnLineDto
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

        // The underlying GoodsReceiptLine's own frozen fields (see
        // migrations/20260807_GoodsReceiptLine_AddUnitPrice.sql) — null for a line received before
        // that fix. Exposed here (added 2026-08-07, same day as Supplier Credit Notes) purely so
        // the credit-note create page can pre-fill/display what the system already knows instead
        // of asking the buyer to type every price blind.
        public decimal? UnitPrice { get; set; }
        public string? CurrencyCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
    }
}
