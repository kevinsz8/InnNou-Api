namespace InnNou.Domain.Dtos
{
    // Caller-supplied (possibly buyer-corrected) line values — pre-filled by the frontend from
    // the selected GoodsReceiptLine's QuantityAccepted and the owning PurchaseOrderLine's
    // effective price, editable if the real supplier invoice differs. Keyed by
    // GoodsReceiptLineToken (not PurchaseOrderLineToken) since 2026-08-02 — invoicing moved to
    // per-delivery granularity, see SupplierInvoiceService.CreateAsync.
    public class CreateSupplierInvoiceLineInputDto
    {
        public Guid GoodsReceiptLineToken { get; set; }
        public decimal QuantityInvoiced { get; set; }
        public decimal UnitPriceInvoiced { get; set; }
    }
}
