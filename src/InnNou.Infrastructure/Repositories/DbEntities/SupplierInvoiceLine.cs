namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierInvoiceLine
    {
        public int SupplierInvoiceLineId { get; set; }
        public Guid SupplierInvoiceLineToken { get; set; }
        public int SupplierInvoiceId { get; set; }
        public int PurchaseOrderLineId { get; set; }
        public Guid PurchaseOrderLineToken { get; set; }

        // Only populated by sp_SupplierInvoiceLine_GetBySupplierInvoiceId — display convenience.
        public decimal OrderedQuantity { get; set; }
        public string? PurchaseOrderNumber { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        public decimal QuantityInvoiced { get; set; }
        public decimal UnitPriceInvoiced { get; set; }
        public string CurrencyCode { get; set; } = default!;

        public int? TaxCategoryId { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public bool IsWithinTolerance { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
