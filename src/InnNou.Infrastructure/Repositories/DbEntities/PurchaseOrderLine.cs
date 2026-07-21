namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class PurchaseOrderLine
    {
        public int PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public int OrderLineId { get; set; }
        public Guid OrderLineToken { get; set; }
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

        // Copied verbatim from the source OrderLine at Submit split time — see OrderLine.cs.
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
