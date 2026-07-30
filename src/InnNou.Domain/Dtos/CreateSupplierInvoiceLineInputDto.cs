namespace InnNou.Domain.Dtos
{
    // Caller-supplied (possibly buyer-corrected) line values — pre-filled by the frontend from
    // the PurchaseOrderLine's effective quantity/price, editable if the real supplier invoice
    // differs. See SupplierInvoiceService.CreateAsync.
    public class CreateSupplierInvoiceLineInputDto
    {
        public Guid PurchaseOrderLineToken { get; set; }
        public decimal QuantityInvoiced { get; set; }
        public decimal UnitPriceInvoiced { get; set; }
    }
}
