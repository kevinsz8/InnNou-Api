namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class SupplierCreditNoteTaxBreakdown
    {
        public Guid SupplierCreditNoteTaxBreakdownToken { get; set; }
        public decimal TaxRatePercent { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string CurrencyCode { get; set; } = default!;
    }
}
