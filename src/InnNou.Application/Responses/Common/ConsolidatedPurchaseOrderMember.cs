namespace InnNou.Application.Responses.Common
{
    public class ConsolidatedPurchaseOrderMember
    {
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public Guid OrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime SentUtc { get; set; }
        public int LineCount { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
