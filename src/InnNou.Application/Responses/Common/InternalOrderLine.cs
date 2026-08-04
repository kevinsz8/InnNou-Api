namespace InnNou.Application.Responses.Common
{
    public class InternalOrderLine
    {
        public Guid InternalOrderLineToken { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }
        public string? SupplierSku { get; set; }

        public decimal Quantity { get; set; }
        public string? PurchaseUnitCode { get; set; }

        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;

        public string? Notes { get; set; }

        public decimal QuantityShipped { get; set; }
        public decimal QuantityAccepted { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
