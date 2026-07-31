namespace InnNou.Domain.Dtos
{
    // One eligible GoodsReceipt (delivery/albarán), invoiceable independently of its sibling
    // receipts on the same PurchaseOrder — see sp_GoodsReceipt_GetEligibleForInvoicing's own
    // comment for why invoicing moved to this granularity. TotalTaxableAmount/TotalAmount are
    // THIS RECEIPT's own totals, not the whole PurchaseOrder's.
    public class GoodsReceiptForInvoicingDto
    {
        public Guid GoodsReceiptToken { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public DateTime PurchaseOrderSentUtc { get; set; }
        public string PurchaseOrderStatus { get; set; } = default!;
        public string DeliveryNoteNumber { get; set; } = default!;
        public DateTime GoodsReceiptCreatedUtc { get; set; }
        public string WarehouseName { get; set; } = default!;
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
