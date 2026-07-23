namespace InnNou.Domain.Dtos
{
    public class PurchaseOrderDto
    {
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
        public string Status { get; set; } = default!;
        public DateTime SentUtc { get; set; }
        public DateTime? CancelledUtc { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? CreatedBy { get; set; }

        // Always accurate regardless of code path: sp_PurchaseOrder_GetPaged computes it via a
        // cheap CROSS APPLY COUNT; GetByToken/Cancel set it from Lines.Count after hydrating
        // Lines. Exists so list/summary views (which never hydrate the full Lines collection,
        // to avoid N+1) can still show an accurate line count.
        public int LineCount { get; set; }

        // Populated by PurchaseOrderService via sp_PurchaseOrderLine_GetByPurchaseOrderId —
        // its own independent snapshot rows, not a filtered view over OrderLine.
        public List<PurchaseOrderLineDto> Lines { get; set; } = [];
    }
}
