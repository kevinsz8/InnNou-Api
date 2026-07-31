namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // sp_GoodsReceipt_GetByToken's row shape — internal to SupplierInvoiceService.CreateAsync's
    // per-selected-receipt validation, never mapped to a public DTO.
    public class GoodsReceiptWithPurchaseOrderContext
    {
        public int GoodsReceiptId { get; set; }
        public Guid GoodsReceiptToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public DateTime PurchaseOrderSentUtc { get; set; }
        public int OrganizationId { get; set; }
        public int SupplierId { get; set; }
        public string PurchaseOrderStatus { get; set; } = default!;
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string WarehouseName { get; set; } = default!;
        public string DeliveryNoteNumber { get; set; } = default!;
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string CreatedBy { get; set; } = default!;
    }
}
