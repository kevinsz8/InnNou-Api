namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierCreditNoteLine
    {
        public int SupplierCreditNoteLineId { get; set; }
        public Guid SupplierCreditNoteLineToken { get; set; }
        public int SupplierCreditNoteId { get; set; }

        public int SupplierReturnLineId { get; set; }
        public Guid SupplierReturnLineToken { get; set; }

        public int ArticleId { get; set; }
        public Guid ArticleToken { get; set; }
        public string? ArticleName { get; set; }

        public decimal QuantityCredited { get; set; }
        public decimal UnitPrice { get; set; }
        public string CurrencyCode { get; set; } = default!;

        public int? TaxCategoryId { get; set; }
        public string? TaxCategoryCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool WasManuallyEntered { get; set; }

        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
