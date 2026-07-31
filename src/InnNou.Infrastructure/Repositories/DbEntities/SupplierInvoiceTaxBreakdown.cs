namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierInvoiceTaxBreakdown
    {
        public int SupplierInvoiceTaxBreakdownId { get; set; }
        public Guid SupplierInvoiceTaxBreakdownToken { get; set; }
        public int SupplierInvoiceId { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
