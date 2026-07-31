namespace InnNou.Application.Responses.Common
{
    public class GoodsReceiptForInvoicing
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
