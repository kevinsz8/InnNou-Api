namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class OrderLine
    {
        public int OrderLineId { get; set; }
        public Guid OrderLineToken { get; set; }
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public decimal Quantity { get; set; }

        public int PurchaseUnitId { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public decimal PurchaseQuantity { get; set; }
        public int ContentUnitId { get; set; }
        public string? ContentUnitCode { get; set; }
        public decimal? ContentQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;

        // Frozen classification snapshot, resolved once at line-add time (see
        // OrderService.AddLineAsync) — never re-resolved live, so a later Article
        // reclassification or Category Code rename can't retroactively change a historical
        // report. Null when the article had no classification at add time.
        public int? CategoryId { get; set; }
        public string? CategoryCode { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryCode { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
