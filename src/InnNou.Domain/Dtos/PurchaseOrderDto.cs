namespace InnNou.Domain.Dtos
{
    public class PurchaseOrderDto
    {
        public int PurchaseOrderId { get; set; }
        public Guid PurchaseOrderToken { get; set; }
        public int OrderId { get; set; }
        public Guid OrderToken { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int OrganizationId { get; set; }
        public Guid OrganizationToken { get; set; }
        public int WarehouseId { get; set; }
        public Guid WarehouseToken { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = default!;
        public DateTime SentUtc { get; set; }
        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Populated by PurchaseOrderService via sp_PurchaseOrderLine_GetByPurchaseOrderId —
        // its own independent snapshot rows, not a filtered view over OrderLine.
        public List<PurchaseOrderLineDto> Lines { get; set; } = [];
    }
}
