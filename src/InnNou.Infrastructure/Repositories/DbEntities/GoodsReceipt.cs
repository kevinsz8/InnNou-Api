namespace InnNou.Infrastructure.Repositories.DbEntities
{
    public class GoodsReceipt
    {
        public int GoodsReceiptId { get; set; }
        public Guid GoodsReceiptToken { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public int SupplierId { get; set; }
        public int OrganizationId { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Only populated by sp_GoodsReceipt_GetPaged (a cheap CROSS APPLY COUNT, same convention
        // as PurchaseOrder.LineCount); GetByToken/Create leave this at 0 and GoodsReceiptService
        // overwrites it from the real hydrated Lines.Count instead.
        public int LineCount { get; set; }
    }
}
