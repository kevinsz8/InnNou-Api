namespace InnNou.Domain.Dtos
{
    // Caller-supplied, typed in from the supplier's real paper/PDF invoice — the "Base Fra" per
    // tax rate, an external fact rather than anything derived from our own receipt data. TaxAmount
    // is never accepted from the client; SupplierInvoiceService.CreateAsync computes it server-side.
    public class CreateSupplierInvoiceTaxBreakdownInputDto
    {
        public decimal? TaxRatePercent { get; set; }
        public decimal BaseAmount { get; set; }
    }
}
