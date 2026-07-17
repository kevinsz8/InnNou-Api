namespace InnNou.Application.Responses.Common
{
    public class PurchaseOrder
    {
        public Guid PurchaseOrderToken { get; set; }
        public Guid OrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public Guid OrganizationToken { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime SentUtc { get; set; }
        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int LineCount { get; set; }
        public List<PurchaseOrderLine> Lines { get; set; } = [];
    }
}
