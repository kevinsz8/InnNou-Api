namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // sp_GoodsReceipt_GetEligibleForInvoicing's row shape.
    public class GoodsReceiptForInvoicing
    {
        public int GoodsReceiptId { get; set; }
        public Guid GoodsReceiptToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public DateTime PurchaseOrderSentUtc { get; set; }
        public string PurchaseOrderStatus { get; set; } = default!;
        public string DeliveryNoteNumber { get; set; } = default!;
        public DateTime GoodsReceiptCreatedUtc { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string WarehouseName { get; set; } = default!;
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
    }
}
