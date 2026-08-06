namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierReturnLine
    {
        public int SupplierReturnLineId { get; set; }
        public Guid SupplierReturnLineToken { get; set; }
        public int SupplierReturnId { get; set; }
        public int GoodsReceiptLineId { get; set; }
        public Guid GoodsReceiptLineToken { get; set; }
        public int GoodsReceiptId { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public decimal QuantityRejected { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // The underlying GoodsReceiptLine's own frozen fields (see
        // migrations/20260807_GoodsReceiptLine_AddUnitPrice.sql) — NULL for a line received
        // before that fix. Used to pre-fill/compute a Nota de Crédito's own line; see
        // SupplierCreditNoteService.CreateAsync.
        public decimal? UnitPrice { get; set; }
        public string? CurrencyCode { get; set; }
        public int? TaxCategoryId { get; set; }
        public decimal? TaxRatePercent { get; set; }
    }
}
