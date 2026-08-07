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

        // Frozen discount snapshot, resolved once at line-add time from
        // sp_ArticleDiscount_GetEffective (see OrderService.AddLineAsync). NULL BaseUnitPrice
        // means no discount applied — UnitPrice above already IS the base price in that case.
        // When set, UnitPrice is the already-discounted price and these three fields are the
        // frozen "why", for transparency on a historical line — see .claude/ArticleDiscountModule.md.
        public decimal? BaseUnitPrice { get; set; }
        public int? DiscountTypeId { get; set; }
        public string? DiscountTypeCode { get; set; }
        public decimal? DiscountValue { get; set; }

        // Frozen classification snapshot, resolved once at line-add time (see
        // OrderService.AddLineAsync) — never re-resolved live, so a later Article
        // reclassification or Category Code rename can't retroactively change a historical
        // report. Null when the article had no classification at add time.
        public int? CategoryId { get; set; }
        public string? CategoryCode { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryCode { get; set; }

        // Unlike Category/SubCategory above, NOT frozen — resolved live from the Article's
        // current Family/SubFamily on every read (see sp_OrderLine_GetByOrderId). Family/
        // SubFamily is stable catalog structure, not the ownership-scoped BI classification
        // model Category/SubCategory has, and this only backs in-page search/filter, not
        // historical reporting, so a live join is simpler and more useful here.
        public string? FamilyCode { get; set; }
        public string? SubFamilyCode { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
