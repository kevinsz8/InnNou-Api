namespace InnNou.Application.Responses.Common
{
    public class PurchaseOrder
    {
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public Guid OrderToken { get; set; }
        public int SupplierId { get; set; }
        public Guid SupplierToken { get; set; }
        public string? SupplierName { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime SentUtc { get; set; }
        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? ClosedShortUtc { get; set; }
        public string? ClosedShortBy { get; set; }
        public string? ClosedShortReason { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int LineCount { get; set; }
        public List<PurchaseOrderLine> Lines { get; set; } = [];
    }
}
