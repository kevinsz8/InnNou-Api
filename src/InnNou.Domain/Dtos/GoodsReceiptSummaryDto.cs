namespace InnNou.Domain.Dtos
{
    // One row on the standalone "Recepciones" history/search page — every GoodsReceipt ever
    // recorded across an organization's purchase orders, not scoped to one PurchaseOrder and not
    // filtered by invoicing state (contrast GoodsReceiptForInvoicingDto, which is invoicing-
    // eligibility-scoped). TotalTaxableAmount/TotalAmount are THIS RECEIPT's own totals.
    public class GoodsReceiptSummaryDto
    {
        public Guid GoodsReceiptToken { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public DateTime PurchaseOrderSentUtc { get; set; }
        public string PurchaseOrderStatus { get; set; } = default!;
        public string SupplierName { get; set; } = default!;
        public string WarehouseName { get; set; } = default!;
        public string DeliveryNoteNumber { get; set; } = default!;
        public DateTime? DeliveryNoteDate { get; set; }
        public DateTime GoodsReceiptCreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int LineCount { get; set; }
    }
}
