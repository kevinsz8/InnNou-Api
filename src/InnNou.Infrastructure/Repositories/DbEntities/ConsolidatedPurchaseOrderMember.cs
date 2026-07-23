using InnNou.Application.Common;

namespace InnNou.Infrastructure.Repositories.DbEntities
{
    // One row per PurchaseOrder pulled into a ConsolidatedPurchaseOrder — the PurchaseOrder
    // itself is never mutated, this is a pure read-through join for display.
    public class ConsolidatedPurchaseOrderMember
    {
        public int ConsolidatedPurchaseOrderMemberId { get; set; }
        public int ConsolidatedPurchaseOrderId { get; set; }
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public string PurchaseOrderNumber { get; set; } = default!;
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public string? OrganizationName { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        public DateTime SentUtc { get; set; }
        public int LineCount { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }
    }
}
