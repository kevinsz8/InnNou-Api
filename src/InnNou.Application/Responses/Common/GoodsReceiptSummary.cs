namespace InnNou.Application.Responses.Common
{
    public class GoodsReceiptSummary
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
