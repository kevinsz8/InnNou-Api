namespace InnNou.Domain.Dtos
{
    public class SupplierInvoiceTaxBreakdownDto
    {
        public Guid SupplierInvoiceTaxBreakdownToken { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
