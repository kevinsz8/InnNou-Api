namespace InnNou.Application.Responses.Common
{
    public class PurchaseOrderLine
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public Guid OrderLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal Quantity { get; set; }
        public string? PurchaseUnitCode { get; set; }
        public decimal PurchaseQuantity { get; set; }
        public string? ContentUnitCode { get; set; }
        public decimal? ContentQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
