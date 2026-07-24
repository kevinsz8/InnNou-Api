namespace InnNou.Domain.Dtos
{
    public class GoodsReceiptDto
    {
        public Guid GoodsReceiptToken { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        public int LineCount { get; set; }

        // Populated by PurchaseOrderService via sp_GoodsReceiptLine_GetByGoodsReceiptId.
        public List<GoodsReceiptLineDto> Lines { get; set; } = [];
    }
}
