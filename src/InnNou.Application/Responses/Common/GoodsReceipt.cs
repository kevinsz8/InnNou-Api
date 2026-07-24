namespace InnNou.Application.Responses.Common
{
    public class GoodsReceipt
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
        public List<GoodsReceiptLine> Lines { get; set; } = [];
    }
}
